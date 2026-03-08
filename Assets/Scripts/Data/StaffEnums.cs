// ============================================================
// StaffEnums.cs
// スタッフシステムで使用する列挙型をまとめて定義する。
// ============================================================

/// <summary>スタッフの固定種族効果。</summary>
public enum StaffFixedEffect
{
    /// <summary>調理速度アップ（将来用）。</summary>
    CookSpeedUp,
    /// <summary>顧客満足度アップ。</summary>
    SatisfactionUp,
    /// <summary>給料が安い。</summary>
    SalaryDiscount,
    /// <summary>品質アップ。</summary>
    QualityUp,
    /// <summary>ドロップ率アップ（バトルバフ）。</summary>
    DropRateUp
}

/// <summary>ランダムバフの種別。</summary>
public enum StaffBuffType
{
    /// <summary>調理速度アップ。</summary>
    CookSpeed,
    /// <summary>品質スコアへの加算。</summary>
    QualityBonus,
    /// <summary>満足度加算。</summary>
    SatisfactionBonus,
    /// <summary>給料減額（割合）。</summary>
    SalaryReduction,
    /// <summary>特定カテゴリの品質ボーナス。</summary>
    CategorySpecialty,
    /// <summary>鮮度バフ倍率ボーナス。</summary>
    FreshnessBonus
}

/// <summary>スタッフのスロット種別。</summary>
public enum StaffSlotType
{
    /// <summary>常勤（日給あり、永続）。</summary>
    Permanent,
    /// <summary>臨時（無給、翌朝消滅）。</summary>
    Temporary
}
