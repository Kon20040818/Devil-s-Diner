// ============================================================
// CalendarEventData.cs
// 特定日のカレンダーイベント定義。
// 「肉の日」「豊漁祭」等のボーナスデイを ScriptableObject で定義する。
// ============================================================
using UnityEngine;

/// <summary>
/// カレンダーイベントのマスターデータ。
/// 特定の日に料理カテゴリの売上や満足度にボーナスが付く。
/// </summary>
[CreateAssetMenu(fileName = "CAL_New", menuName = "DevilsDiner/CalendarEventData")]
public sealed class CalendarEventData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string _eventID;
    [SerializeField] private string _eventName;
    [SerializeField, TextArea(1, 3)] private string _description;
    [SerializeField] private Sprite _icon;

    [Header("発動条件")]
    [SerializeField, Tooltip("発動日リスト（ゲーム内日数）")]
    private int[] _triggerDays;

    [Header("ボーナス対象")]
    [SerializeField, Tooltip("有効にすると特定カテゴリのみボーナス適用")]
    private bool _bonusCategoryEnabled;
    [SerializeField, Tooltip("ボーナス対象カテゴリ（上記が有効時のみ）")]
    private DishCategory _bonusCategory;

    [Header("ボーナス倍率")]
    [SerializeField, Tooltip("満足度への倍率"), Min(0.1f)]
    private float _satisfactionMultiplier = 1.5f;
    [SerializeField, Tooltip("品質（鮮度）への倍率"), Min(0.1f)]
    private float _freshnessMultiplier = 1.2f;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    public string EventID => _eventID;
    public string EventName => _eventName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int[] TriggerDays => _triggerDays;
    public bool BonusCategoryEnabled => _bonusCategoryEnabled;
    public DishCategory BonusCategory => _bonusCategory;
    public float SatisfactionMultiplier => _satisfactionMultiplier;
    public float FreshnessMultiplier => _freshnessMultiplier;

    /// <summary>指定日がイベント発動日かを判定する。</summary>
    public bool IsActiveOnDay(int day)
    {
        if (_triggerDays == null) return false;
        foreach (int d in _triggerDays)
        {
            if (d == day) return true;
        }
        return false;
    }
}
