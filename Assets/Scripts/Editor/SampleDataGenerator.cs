// ============================================================
// SampleDataGenerator.cs
// メニュー「DevilsDiner > Generate All Master Data」で
// MasterDataImporter を呼び出し、全マスターデータを一括生成する。
//
// 全データは Assets/MasterData/ の CSV / JSON で管理。
// データの追加・修正はそれらのファイルを編集するだけで済む。
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 全マスターデータを一括生成するエディタツール。
/// MasterDataImporter に全処理を委譲する。
/// </summary>
public static class SampleDataGenerator
{
    [MenuItem("DevilsDiner/Generate All Master Data")]
    public static void Generate()
    {
        MasterDataImporter.ImportAll();
        Debug.Log("[SampleDataGenerator] 全マスターデータ生成完了！");
    }
}
#endif
