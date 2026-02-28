// ============================================================
// SkillManager.cs
// スキルツリーの解放状態を管理し、効果量を集計する。
// GameManager オブジェクトにアタッチする。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 解放済みスキルの管理と効果量の集計を行うコンポーネント。
/// GameManager と同じ GameObject に配置する。
/// </summary>
public sealed class SkillManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    /// <summary>スキルツリーに含まれる全スキル。</summary>
    [SerializeField] private SkillData[] _availableSkills;

    /// <summary>全利用可能スキルへの読み取り専用アクセス。</summary>
    public System.Collections.Generic.IReadOnlyList<SkillData> AvailableSkills => _availableSkills;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private readonly HashSet<string> _unlockedSkillIds = new HashSet<string>();

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>スキルが解放されたとき（解放されたスキルを引数で通知）。</summary>
    public event Action<SkillData> OnSkillUnlocked;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (_availableSkills == null || _availableSkills.Length == 0)
        {
            _availableSkills = Resources.LoadAll<SkillData>("");
            if (_availableSkills.Length > 0)
            {
                Debug.Log($"[SkillManager] Resources から {_availableSkills.Length} 件の SkillData をロードしました。");
            }
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — スキル解放
    // ──────────────────────────────────────────────

    /// <summary>
    /// スキルの解放を試みる。
    /// 前提スキル未解放・コスト不足・既に解放済みの場合は false を返す。
    /// </summary>
    /// <param name="skill">解放対象のスキル。</param>
    /// <returns>解放に成功した場合 true。</returns>
    public bool TryUnlockSkill(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("[SkillManager] null のスキルは解放できません。");
            return false;
        }

        // 既に解放済み
        if (_unlockedSkillIds.Contains(skill.Id))
        {
            Debug.Log($"[SkillManager] スキル '{skill.SkillName}' は既に解放済みです。");
            return false;
        }

        // 前提スキル未解放
        if (skill.Prerequisite != null && !_unlockedSkillIds.Contains(skill.Prerequisite.Id))
        {
            Debug.Log($"[SkillManager] 前提スキル '{skill.Prerequisite.SkillName}' が未解放のため、'{skill.SkillName}' を解放できません。");
            return false;
        }

        // コスト支払い
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SkillManager] GameManager.Instance が null のためスキル解放を中断しました。");
            return false;
        }

        if (!gm.TrySpendGold(skill.Cost))
        {
            Debug.Log($"[SkillManager] ゴールド不足のため '{skill.SkillName}' を解放できません（必要: {skill.Cost}, 所持: {gm.Gold}）。");
            return false;
        }

        _unlockedSkillIds.Add(skill.Id);
        Debug.Log($"[SkillManager] スキル '{skill.SkillName}' を解放しました。");
        OnSkillUnlocked?.Invoke(skill);
        return true;
    }

    // ──────────────────────────────────────────────
    // 公開 API — 解放状態の問い合わせ
    // ──────────────────────────────────────────────

    /// <summary>指定スキルが解放済みかを返す。</summary>
    public bool IsUnlocked(SkillData skill)
    {
        if (skill == null) return false;
        return _unlockedSkillIds.Contains(skill.Id);
    }

    /// <summary>指定スキルが解放済みかを返す（エイリアス）。</summary>
    public bool IsSkillUnlocked(SkillData skill) => IsUnlocked(skill);

    /// <summary>指定 ID のスキルが解放済みかを返す（セーブ/ロード用）。</summary>
    public bool IsUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return false;
        return _unlockedSkillIds.Contains(skillId);
    }

    // ──────────────────────────────────────────────
    // 公開 API — シリアライズ / デシリアライズ
    // ──────────────────────────────────────────────

    /// <summary>解放済みスキル ID のリストを返す（セーブ用）。</summary>
    public List<string> GetUnlockedSkillIds()
    {
        return new List<string>(_unlockedSkillIds);
    }

    /// <summary>セーブデータから解放済みスキルを復元する。</summary>
    /// <param name="skillIds">復元するスキル ID のリスト。</param>
    public void RestoreUnlockedSkills(List<string> skillIds)
    {
        _unlockedSkillIds.Clear();

        if (skillIds == null) return;

        foreach (string id in skillIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _unlockedSkillIds.Add(id);
            }
        }

        Debug.Log($"[SkillManager] {_unlockedSkillIds.Count} 件のスキルを復元しました。");
    }

    // ──────────────────────────────────────────────
    // 公開 API — 効果量集計
    // ──────────────────────────────────────────────

    /// <summary>
    /// 指定タイプの解放済みスキルの効果量を合計して返す。
    /// </summary>
    /// <param name="type">集計対象のスキルタイプ。</param>
    /// <returns>合計効果量。</returns>
    public float GetTotalBonus(SkillData.SkillType type)
    {
        float total = 0f;

        if (_availableSkills == null) return total;

        foreach (SkillData skill in _availableSkills)
        {
            if (skill != null
                && skill.Type == type
                && _unlockedSkillIds.Contains(skill.Id))
            {
                total += skill.Value;
            }
        }

        return total;
    }
}
