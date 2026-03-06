// ============================================================
// MetaphorUISetup.cs
// メタファー風バトルUIの自動セットアップEditor拡張。
// 旧uGUIコンポーネントを非破壊的に無効化し、
// UIDocument + DynamicBattleUIController を持つGameObjectを構築する。
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class MetaphorUISetup
{
    private const string MENU_PATH = "Tools/Setup Metaphor Battle UI";
    private const string GO_NAME = "MetaphorBattleUI";
    private const string UXML_PATH = "Assets/UI/BattleUI.uxml";
    private const string PANEL_SETTINGS_PATH = "Assets/UI/BattlePanelSettings.asset";

    [MenuItem(MENU_PATH)]
    private static void Execute()
    {
        // ── 1. 旧uGUI無効化（非破壊） ──
        DisableLegacyUI<SkillCommandUI>();
        DisableLegacyUI<ActionTimelineUI>();
        DisableLegacyUI<CharacterStatusUI>();
        DisableLegacyUI<UltimatePortraitUI>();
        DisableLegacyByName("ToggleButtons");

        // ── 2. 既存MetaphorBattleUIを削除して再作成 ──
        var existing = GameObject.Find(GO_NAME);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        var go = new GameObject(GO_NAME);
        Undo.RegisterCreatedObjectUndo(go, "Create MetaphorBattleUI");

        // ── UIDocument ──
        var uiDocument = go.AddComponent<UIDocument>();

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        if (uxml != null)
        {
            uiDocument.visualTreeAsset = uxml;
        }
        else
        {
            Debug.LogWarning($"[MetaphorUISetup] {UXML_PATH} が見つかりません。手動で設定してください。");
        }

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS_PATH);
        if (panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings;
        }
        else
        {
            Debug.LogWarning($"[MetaphorUISetup] {PANEL_SETTINGS_PATH} が見つかりません。手動で設定してください。");
        }

        // ── DynamicBattleUIController ──
        go.AddComponent<DynamicBattleUIController>();

        // ── 3. シーンをダーティにしてログ出力 ──
        EditorSceneManager.MarkSceneDirty(go.scene);

        Debug.Log("[MetaphorUISetup] メタファー風バトルUIの自動セットアップが完了しました。");
    }

    private static void DisableLegacyByName(string goName)
    {
        var go = GameObject.Find(goName);
        if (go != null && go.activeSelf)
        {
            Undo.RecordObject(go, $"Disable {goName}");
            go.SetActive(false);
            Debug.Log($"[MetaphorUISetup] {goName} を非アクティブ化しました。");
        }
    }

    private static void DisableLegacyUI<T>() where T : MonoBehaviour
    {
        var instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            if (instance.gameObject.activeSelf)
            {
                Undo.RecordObject(instance.gameObject, $"Disable {typeof(T).Name}");
                instance.gameObject.SetActive(false);
                Debug.Log($"[MetaphorUISetup] {instance.gameObject.name} ({typeof(T).Name}) を非アクティブ化しました。");
            }
        }
    }
}
#endif
