// ============================================================
// StaffInstance.cs
// スカウトされた悪魔スタッフの個体データ（ランタイム）。
// 種族固定効果 + ランダムバフを保持する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// スカウトされた悪魔スタッフ1体分の個体データ。
/// <see cref="StaffRaceData"/> のマスターを参照しつつ、
/// ランダムバフの確定結果を個体固有に保持する。
/// </summary>
[Serializable]
public sealed class StaffInstance
{
    // ──────────────────────────────────────────────
    // フィールド
    // ──────────────────────────────────────────────

    [SerializeField] private string _id;
    [SerializeField] private string _sourceEnemyName;
    [SerializeField] private StaffRaceData _race;
    [SerializeField] private StaffBuffData[] _randomBuffs;
    [SerializeField] private StaffSlotType _slotType;
    [SerializeField] private int _moralePenalty;

    // ──────────────────────────────────────────────
    // コンストラクタ
    // ──────────────────────────────────────────────

    /// <summary>新規スタッフを生成する。</summary>
    public StaffInstance(string sourceEnemyName, StaffRaceData race, StaffBuffData[] randomBuffs, StaffSlotType slotType)
    {
        _id = Guid.NewGuid().ToString("N");
        _sourceEnemyName = sourceEnemyName;
        _race = race;
        _randomBuffs = randomBuffs ?? Array.Empty<StaffBuffData>();
        _slotType = slotType;
        _moralePenalty = 0;
    }

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>個体固有 ID。</summary>
    public string ID => _id;

    /// <summary>スカウト元の敵名。</summary>
    public string SourceEnemyName => _sourceEnemyName;

    /// <summary>種族マスター参照。</summary>
    public StaffRaceData Race => _race;

    /// <summary>確定済みランダムバフ配列。</summary>
    public StaffBuffData[] RandomBuffs => _randomBuffs;

    /// <summary>スロット種別（常勤 / 臨時）。</summary>
    public StaffSlotType SlotType => _slotType;

    /// <summary>不満蓄積値。3 に達すると退職。</summary>
    public int MoralePenalty => _moralePenalty;

    /// <summary>表示名（元の敵名 + 種族名）。</summary>
    public string DisplayName => _race != null
        ? $"{_sourceEnemyName}（{_race.RaceName}）"
        : _sourceEnemyName;

    // ──────────────────────────────────────────────
    // 給料計算
    // ──────────────────────────────────────────────

    /// <summary>日給を計算する。SalaryReduction バフを考慮。</summary>
    public int CalculateSalary()
    {
        if (_race == null) return 0;
        if (_slotType == StaffSlotType.Temporary) return 0;

        float salary = _race.BaseSalary;

        // 固定効果: SalaryDiscount
        if (_race.FixedEffect == StaffFixedEffect.SalaryDiscount)
        {
            salary *= (1f - _race.FixedEffectValue);
        }

        // ランダムバフ: SalaryReduction
        foreach (var buff in _randomBuffs)
        {
            if (buff != null && buff.Type == StaffBuffType.SalaryReduction)
            {
                salary *= (1f - buff.Value);
            }
        }

        return Mathf.Max(1, Mathf.RoundToInt(salary));
    }

    // ──────────────────────────────────────────────
    // スロット変更
    // ──────────────────────────────────────────────

    /// <summary>臨時 → 常勤に昇格する。</summary>
    public void Promote()
    {
        _slotType = StaffSlotType.Permanent;
    }

    // ──────────────────────────────────────────────
    // モラルペナルティ
    // ──────────────────────────────────────────────

    /// <summary>不満を1加算する。戻り値は退職ラインに達したか。</summary>
    public bool AddMoralePenalty()
    {
        _moralePenalty++;
        return _moralePenalty >= 3;
    }

    /// <summary>給料支払い成功時に不満をリセットする。</summary>
    public void ResetMoralePenalty()
    {
        _moralePenalty = 0;
    }
}
