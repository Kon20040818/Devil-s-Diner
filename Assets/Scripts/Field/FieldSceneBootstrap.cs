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
    private InputAction _returnAction;

    private void Start()
    {
        EnsureGameManagerExists();
        WireFieldScene();
    }

    private void Update()
    {
        // ESC キーで経営パートへ帰還
        if (_returnAction != null && _returnAction.WasPressedThisFrame())
        {
            Debug.Log("[FieldSceneBootstrap] 帰還！経営パートへ遷移します。");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvancePhase();
            }
        }
    }

    private void OnDisable()
    {
        _returnAction?.Disable();
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

        // ── 帰還アクション（ESC キー）──
        _returnAction = new InputAction("Return", InputActionType.Button, "<Keyboard>/escape");
        _returnAction.Enable();

        Debug.Log($"[FieldSceneBootstrap] フィールドシーン結線完了。プレイヤー: 1, 敵シンボル: {symbols.Length}体");
    }

    /// <summary>
    /// 単独シーン再生時の開発用フォールバック。
    /// BootScene を経由せずに直接 Play した場合に GameManager を自動生成する。
    /// </summary>
    private static void EnsureGameManagerExists()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager [Fallback]");
        go.AddComponent<GameManager>();
        Debug.LogWarning("[FieldSceneBootstrap] GameManager フォールバック生成。通常は BootScene から起動してください。");
    }
}
