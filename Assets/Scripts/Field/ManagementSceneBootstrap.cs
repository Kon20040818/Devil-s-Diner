// ============================================================
// ManagementSceneBootstrap.cs
// ManagementScene（経営パート）のブートストラップ。
// ============================================================
using UnityEngine;

/// <summary>
/// ManagementScene 起動時にシステムを初期化するブートストラップ。
/// </summary>
public sealed class ManagementSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        // カーソル解放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ManagementSceneUI 検索
        ManagementSceneUI managementUI = FindFirstObjectByType<ManagementSceneUI>();
        if (managementUI == null)
        {
            Debug.LogWarning("[ManagementSceneBootstrap] ManagementSceneUI が見つかりません。");
        }

        Debug.Log("[ManagementSceneBootstrap] 経営シーン結線完了。");
    }
}
