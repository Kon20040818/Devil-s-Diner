// ============================================================
// StaffRaceData.cs
// 悪魔種族のマスターデータ。固定効果とランダムバフプールを定義。
// ============================================================
using UnityEngine;

/// <summary>
/// 悪魔種族のマスターデータ。
/// スカウトされた悪魔がスタッフになる際、種族に基づく固定効果と
/// ランダムバフの候補プールが参照される。
/// </summary>
[CreateAssetMenu(fileName = "RACE_New", menuName = "DevilsDiner/Staff/StaffRaceData")]
public sealed class StaffRaceData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string _raceID;
    [SerializeField] private string _raceName;
    [SerializeField] private Sprite _portrait;

    [Header("固定種族効果")]
    [SerializeField] private StaffFixedEffect _fixedEffect = StaffFixedEffect.SatisfactionUp;
    [SerializeField, Tooltip("固定効果の数値（倍率 or 加算値）")]
    private float _fixedEffectValue = 0.1f;

    [Header("給料")]
    [SerializeField, Tooltip("常勤時の基本日給"), Min(0)]
    private int _baseSalary = 50;

    [Header("ランダムバフ")]
    [SerializeField, Tooltip("この種族で出現しうるバフの候補")]
    private StaffBuffData[] _possibleBuffs;

    [SerializeField, Tooltip("スカウト時に付与されるバフの最小数"), Range(1, 3)]
    private int _minBuffCount = 1;

    [SerializeField, Tooltip("スカウト時に付与されるバフの最大数"), Range(1, 3)]
    private int _maxBuffCount = 2;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    public string RaceID => _raceID;
    public string RaceName => _raceName;
    public Sprite Portrait => _portrait;
    public StaffFixedEffect FixedEffect => _fixedEffect;
    public float FixedEffectValue => _fixedEffectValue;
    public int BaseSalary => _baseSalary;
    public StaffBuffData[] PossibleBuffs => _possibleBuffs;
    public int MinBuffCount => _minBuffCount;
    public int MaxBuffCount => _maxBuffCount;
}
