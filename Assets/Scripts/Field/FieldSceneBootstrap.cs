// ============================================================
// FieldSceneBootstrap.cs
// FieldScene のブートストラップ。シーンロード時に
// プレイヤー・カメラ・敵シンボルの参照を自動結線する。
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FieldScene 起動時にフィールドシステムを初期化・結線するブートストラップ。
/// FieldSystem GameObject にアタッチして使用する。
/// </summary>
public sealed class FieldSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        WireFieldScene();
    }

    private void WireFieldScene()
    {
        // ── InputActionAsset 検索 ──
        InputActionAsset inputActions = null;
        var existingActions = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        if (existingActions.Length > 0)
        {
            inputActions = existingActions[0];
        }

        // ── FieldPlayerController 検索・結線 ──
        FieldPlayerController playerController = FindFirstObjectByType<FieldPlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[FieldSceneBootstrap] FieldPlayerController が見つかりません。");
            return;
        }

        if (inputActions != null)
        {
            playerController.SetInputActions(inputActions);
        }

        Transform playerTransform = playerController.transform;

        // ── FieldCameraController 検索・結線 ──
        FieldCameraController cameraController = FindFirstObjectByType<FieldCameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(playerTransform);
            if (inputActions != null)
            {
                cameraController.SetInputActions(inputActions);
            }

            // カメラ Transform をプレイヤーに設定
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerController.SetCameraTransform(mainCam.transform);
            }
        }
        else
        {
            Debug.LogWarning("[FieldSceneBootstrap] FieldCameraController が見つかりません。");
        }

        // ── FieldEncounterHandler 検索 or 自動生成 ──
        FieldEncounterHandler encounterHandler = FindFirstObjectByType<FieldEncounterHandler>();
        if (encounterHandler == null)
        {
            encounterHandler = gameObject.AddComponent<FieldEncounterHandler>();
            Debug.Log("[FieldSceneBootstrap] FieldEncounterHandler を自動生成しました。");
        }

        // ── 全 EnemySymbol を検索・結線 ──
        EnemySymbol[] symbols = FindObjectsByType<EnemySymbol>(FindObjectsSortMode.None);
        foreach (var symbol in symbols)
        {
            symbol.SetPlayer(playerTransform);
            encounterHandler.RegisterSymbol(symbol);
        }

        Debug.Log($"[FieldSceneBootstrap] フィールドシーン結線完了。プレイヤー: 1, 敵シンボル: {symbols.Length}体");
    }
}
