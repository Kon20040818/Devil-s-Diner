// ============================================================
// DevilsDinerSetupTool.cs
// Phase 1 プロトタイプ用の自動セットアップエディタ拡張。
// Assets/Data/ 以下にフォルダ階層とダミー ScriptableObject を生成する。
// ============================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 1 プロトタイプ用オートセットアップツール。
/// メニュー <c>DevilsDiner &gt; Auto Setup Phase 1</c> からフォルダ構造と
/// ダミー ScriptableObject アセットを一括生成する。
/// 既に存在するフォルダ・アセットはスキップする（冪等）。
/// </summary>
public static class DevilsDinerSetupTool
{
    private const string MENU_PATH = "DevilsDiner/Auto Setup Phase 1";
    private const string DATA_ROOT = "Assets/Data";

    // ----------------------------------------------------------
    // Folder definitions
    // ----------------------------------------------------------
    private static readonly string[] _folders = new string[]
    {
        "Assets/Data/Materials",
        "Assets/Data/Recipes",
        "Assets/Data/Weapons",
        "Assets/Data/Furniture",
        "Assets/Data/Enemies",
        "Assets/Data/Config",
    };

    // ----------------------------------------------------------
    // Asset definitions — (folder, file name, ScriptableObject type)
    // ----------------------------------------------------------
    private static readonly List<(string folder, string fileName, System.Type type)> _assets =
        new List<(string, string, System.Type)>
    {
        ("Assets/Data/Materials", "MAT_Dummy",        typeof(MaterialData)),
        ("Assets/Data/Recipes",   "RCP_Dummy",        typeof(RecipeData)),
        ("Assets/Data/Weapons",   "WPN_Dummy",        typeof(WeaponData)),
        ("Assets/Data/Furniture", "FRN_Dummy",        typeof(FurnitureData)),
        ("Assets/Data/Enemies",   "ENM_Dummy",        typeof(EnemyData)),
        ("Assets/Data/Config",    "JustInputConfig",  typeof(JustInputConfig)),
        ("Assets/Data/Config",    "CookingConfig",    typeof(CookingConfig)),
    };

    // ----------------------------------------------------------
    // Menu entry
    // ----------------------------------------------------------
    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        int foldersCreated = 0;
        int assetsCreated  = 0;

        int totalSteps = _folders.Length + _assets.Count;
        int currentStep = 0;

        try
        {
            // ---- Create folders ----
            foreach (string folder in _folders)
            {
                currentStep++;
                EditorUtility.DisplayProgressBar(
                    "Auto Setup Phase 1",
                    $"Creating folder: {folder}",
                    (float)currentStep / totalSteps);

                if (!AssetDatabase.IsValidFolder(folder))
                {
                    CreateFolderRecursive(folder);
                    foldersCreated++;
                }
            }

            // ---- Create dummy ScriptableObject assets ----
            foreach (var (folder, fileName, type) in _assets)
            {
                currentStep++;
                string assetPath = $"{folder}/{fileName}.asset";

                EditorUtility.DisplayProgressBar(
                    "Auto Setup Phase 1",
                    $"Creating asset: {assetPath}",
                    (float)currentStep / totalSteps);

                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
                {
                    continue;
                }

                ScriptableObject instance = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(instance, assetPath);
                assetsCreated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // ---- Summary log ----
        Debug.Log(
            $"[DevilsDiner] Auto Setup Phase 1 complete — " +
            $"Folders created: {foldersCreated}, Assets created: {assetsCreated}");
    }

    // ----------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------

    /// <summary>
    /// 再帰的にフォルダを作成する。親フォルダが存在しない場合は先に作成する。
    /// </summary>
    private static void CreateFolderRecursive(string folderPath)
    {
        // Split into parent and leaf
        int lastSlash = folderPath.LastIndexOf('/');
        if (lastSlash < 0) return;

        string parent = folderPath.Substring(0, lastSlash);
        string leaf   = folderPath.Substring(lastSlash + 1);

        // Ensure the parent exists first
        if (!AssetDatabase.IsValidFolder(parent))
        {
            CreateFolderRecursive(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
