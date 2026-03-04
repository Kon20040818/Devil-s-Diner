// ============================================================
// MapData.cs
// 1つのマップ（狩り場）を定義する ScriptableObject。
// レベルに応じて解放される。
// ============================================================
using UnityEngine;

/// <summary>
/// 狩りに出撃可能なマップの1エリアを表すデータアセット。
/// 店舗レベルに応じて段階的に解放される。
/// </summary>
[CreateAssetMenu(fileName = "MAP_New", menuName = "DevilsDiner/MapData")]
public sealed class MapData : ScriptableObject
{
    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>マップの環境タイプ。</summary>
    public enum EnvironmentType
    {
        Desert,
        Forest,
        Swamp,
        Volcano,
        Castle
    }

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    /// <summary>マップの一意な識別子。</summary>
    [SerializeField] private string _id;

    /// <summary>マップの表示名。</summary>
    [SerializeField] private string _mapName;

    /// <summary>マップの説明文。</summary>
    [SerializeField] private string _description;

    /// <summary>環境タイプ。</summary>
    [SerializeField] private EnvironmentType _environment;

    /// <summary>解放に必要な店舗レベル。</summary>
    [SerializeField] private int _requiredShopLevel = 1;

    /// <summary>推奨レベル（表示用）。</summary>
    [SerializeField] private int _recommendedLevel = 1;

    /// <summary>ロード対象のシーン名。</summary>
    [SerializeField] private string _sceneName = "ActionScene";

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>マップの一意な識別子。</summary>
    public string Id => _id;

    /// <summary>マップの表示名。</summary>
    public string MapName => _mapName;

    /// <summary>マップの説明文。</summary>
    public string Description => _description;

    /// <summary>環境タイプ。</summary>
    public EnvironmentType Environment => _environment;

    /// <summary>解放に必要な店舗レベル。</summary>
    public int RequiredShopLevel => _requiredShopLevel;

    /// <summary>推奨レベル（表示用）。</summary>
    public int RecommendedLevel => _recommendedLevel;

    /// <summary>ロード対象のシーン名。</summary>
    public string SceneName => _sceneName;
}
