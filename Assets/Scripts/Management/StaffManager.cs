// ============================================================
// StaffManager.cs
// スタッフの永続管理。常勤3 + 臨時2 スロット。
// GameManager と同一 GameObject にアタッチされ DontDestroyOnLoad。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スタッフの雇用・解雇・昇格・給料処理を管理する。
/// <see cref="GameManager.Staff"/> で参照する。
/// </summary>
public sealed class StaffManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const int MAX_PERMANENT = 3;
    private const int MAX_TEMPORARY = 2;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>スタッフ構成が変化したとき。</summary>
    public event Action OnStaffChanged;

    // ──────────────────────────────────────────────
    // データ
    // ──────────────────────────────────────────────

    private readonly List<StaffInstance> _permanentStaff = new List<StaffInstance>();
    private readonly List<StaffInstance> _temporaryStaff = new List<StaffInstance>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>常勤スタッフ一覧（読み取り専用）。</summary>
    public IReadOnlyList<StaffInstance> PermanentStaff => _permanentStaff;

    /// <summary>臨時スタッフ一覧（読み取り専用）。</summary>
    public IReadOnlyList<StaffInstance> TemporaryStaff => _temporaryStaff;

    /// <summary>常勤枠の空き数。</summary>
    public int PermanentSlotsAvailable => MAX_PERMANENT - _permanentStaff.Count;

    /// <summary>臨時枠の空き数。</summary>
    public int TemporarySlotsAvailable => MAX_TEMPORARY - _temporaryStaff.Count;

    // ──────────────────────────────────────────────
    // 雇用
    // ──────────────────────────────────────────────

    /// <summary>
    /// スタッフを指定スロットに配置する。
    /// 枠が満杯なら false を返し何もしない。
    /// </summary>
    public bool TryHire(StaffInstance staff, StaffSlotType slot)
    {
        if (staff == null) return false;

        switch (slot)
        {
            case StaffSlotType.Permanent:
                if (_permanentStaff.Count >= MAX_PERMANENT) return false;
                _permanentStaff.Add(staff);
                break;
            case StaffSlotType.Temporary:
                if (_temporaryStaff.Count >= MAX_TEMPORARY) return false;
                _temporaryStaff.Add(staff);
                break;
            default:
                return false;
        }

        OnStaffChanged?.Invoke();
        Debug.Log($"[StaffManager] {staff.DisplayName} を{slot}スロットに配置しました。");
        return true;
    }

    /// <summary>スタッフを解雇（リストから除去）する。</summary>
    public void Fire(StaffInstance staff)
    {
        if (staff == null) return;
        bool removed = _permanentStaff.Remove(staff) || _temporaryStaff.Remove(staff);
        if (removed)
        {
            OnStaffChanged?.Invoke();
            Debug.Log($"[StaffManager] {staff.DisplayName} を解雇しました。");
        }
    }

    /// <summary>臨時スタッフを常勤に昇格させる。常勤枠が満杯なら false。</summary>
    public bool TryPromote(StaffInstance staff)
    {
        if (staff == null) return false;
        if (staff.SlotType != StaffSlotType.Temporary) return false;
        if (_permanentStaff.Count >= MAX_PERMANENT) return false;

        if (!_temporaryStaff.Remove(staff)) return false;

        staff.Promote();
        _permanentStaff.Add(staff);
        OnStaffChanged?.Invoke();
        Debug.Log($"[StaffManager] {staff.DisplayName} を常勤に昇格しました。");
        return true;
    }

    // ──────────────────────────────────────────────
    // 朝の処理
    // ──────────────────────────────────────────────

    /// <summary>
    /// 朝の給料支払い処理。
    /// 残高不足の場合は不満加算。不満3で自動退職。
    /// </summary>
    public void ProcessMorningPayroll()
    {
        if (GameManager.Instance == null) return;

        var toRemove = new List<StaffInstance>();

        foreach (var staff in _permanentStaff)
        {
            int salary = staff.CalculateSalary();

            if (GameManager.Instance.TrySpendGold(salary))
            {
                staff.ResetMoralePenalty();
                Debug.Log($"[StaffManager] {staff.DisplayName} 給料 {salary}G 支払い完了。");
            }
            else
            {
                bool shouldQuit = staff.AddMoralePenalty();
                Debug.LogWarning($"[StaffManager] {staff.DisplayName} 給料未払い！ 不満: {staff.MoralePenalty}/3");

                if (shouldQuit)
                {
                    Debug.LogWarning($"[StaffManager] {staff.DisplayName} が退職しました！（不満上限）");
                    toRemove.Add(staff);
                }
            }
        }

        foreach (var staff in toRemove)
        {
            _permanentStaff.Remove(staff);
        }

        if (toRemove.Count > 0)
        {
            OnStaffChanged?.Invoke();
        }
    }

    /// <summary>臨時スタッフを全員消去する。翌朝に呼ぶ。</summary>
    public void ClearTemporaryStaff()
    {
        if (_temporaryStaff.Count == 0) return;

        Debug.Log($"[StaffManager] 臨時スタッフ {_temporaryStaff.Count}名 が去りました。");
        _temporaryStaff.Clear();
        OnStaffChanged?.Invoke();
    }

    // ──────────────────────────────────────────────
    // バフ集計
    // ──────────────────────────────────────────────

    /// <summary>全スタッフのバフを集計して返す。</summary>
    public StaffBuffSummary GetActiveBonuses()
    {
        var summary = new StaffBuffSummary();

        foreach (var staff in _permanentStaff)
        {
            AccumulateBuffs(staff, ref summary);
        }
        foreach (var staff in _temporaryStaff)
        {
            AccumulateBuffs(staff, ref summary);
        }

        return summary;
    }

    private void AccumulateBuffs(StaffInstance staff, ref StaffBuffSummary summary)
    {
        if (staff.Race == null) return;

        // 固定種族効果
        switch (staff.Race.FixedEffect)
        {
            case StaffFixedEffect.SatisfactionUp:
                summary.SatisfactionBonus += staff.Race.FixedEffectValue;
                break;
            case StaffFixedEffect.QualityUp:
                summary.QualityBonus += staff.Race.FixedEffectValue;
                break;
            case StaffFixedEffect.DropRateUp:
                summary.DropRateBonus += staff.Race.FixedEffectValue;
                break;
            case StaffFixedEffect.CookSpeedUp:
                summary.CookSpeedBonus += staff.Race.FixedEffectValue;
                break;
            // SalaryDiscount は CalculateSalary 内で処理済み
        }

        // ランダムバフ
        foreach (var buff in staff.RandomBuffs)
        {
            if (buff == null) continue;
            switch (buff.Type)
            {
                case StaffBuffType.CookSpeed:
                    summary.CookSpeedBonus += buff.Value;
                    break;
                case StaffBuffType.QualityBonus:
                    summary.QualityBonus += buff.Value;
                    break;
                case StaffBuffType.SatisfactionBonus:
                    summary.SatisfactionBonus += buff.Value;
                    break;
                case StaffBuffType.FreshnessBonus:
                    summary.FreshnessBonus += buff.Value;
                    break;
                case StaffBuffType.CategorySpecialty:
                    summary.AddCategoryBonus(buff.TargetCategory, buff.Value);
                    break;
                // SalaryReduction は CalculateSalary 内で処理済み
            }
        }
    }

    // ──────────────────────────────────────────────
    // スカウト結果の受け取り
    // ──────────────────────────────────────────────

    /// <summary>
    /// バトル終了後のスカウト結果を受け取り、臨時スロットへ自動配置する。
    /// 枠が足りない分は破棄される。
    /// </summary>
    public void ReceiveRecruits(List<RecruitedDemonData> recruits)
    {
        if (recruits == null) return;

        foreach (var recruit in recruits)
        {
            if (_temporaryStaff.Count >= MAX_TEMPORARY)
            {
                Debug.LogWarning($"[StaffManager] 臨時枠が満杯のため {recruit.EnemyName} を配置できません。");
                continue;
            }

            var staff = new StaffInstance(
                recruit.EnemyName,
                recruit.Race,
                recruit.RolledBuffs,
                StaffSlotType.Temporary
            );

            _temporaryStaff.Add(staff);
            Debug.Log($"[StaffManager] {staff.DisplayName} を臨時スタッフとして受け入れました。");
        }

        if (recruits.Count > 0)
        {
            OnStaffChanged?.Invoke();
        }
    }

    // ──────────────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────────────

    /// <summary>全スタッフ（常勤+臨時）のリストを返す。</summary>
    public List<StaffInstance> GetAllStaff()
    {
        var all = new List<StaffInstance>(_permanentStaff.Count + _temporaryStaff.Count);
        all.AddRange(_permanentStaff);
        all.AddRange(_temporaryStaff);
        return all;
    }

    /// <summary>全スタッフをクリアする（デバッグ / ニューゲーム用）。</summary>
    public void ClearAll()
    {
        _permanentStaff.Clear();
        _temporaryStaff.Clear();
        OnStaffChanged?.Invoke();
    }

    /// <summary>日給の合計を返す。</summary>
    public int GetTotalDailySalary()
    {
        int total = 0;
        foreach (var staff in _permanentStaff)
        {
            total += staff.CalculateSalary();
        }
        return total;
    }
}
