// ============================================================
// BaseSceneBootstrap.cs
// BaseScene（拠点：車の運転席）のブートストラップ。
// シーンロード時に UI の初期化とカーソル解放を行う。
// ============================================================
using UnityEngine;

/// <summary>
/// BaseScene 起動時にシステムを初期化するブートストラップ。
/// BaseSystem GameObject にアタッチして使用する。
/// </summary>
public sealed class BaseSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        // カーソル解放（フィールドでロックされていた場合）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // CookingManager を配置
        CookingManager cookingMgr = FindFirstObjectByType<CookingManager>();
        if (cookingMgr == null)
        {
            var go = new GameObject("CookingManager");
            cookingMgr = go.AddComponent<CookingManager>();
        }

        // BaseSceneUI 検索 & 初期化
        BaseSceneUI baseUI = FindFirstObjectByType<BaseSceneUI>();
        if (baseUI == null)
        {
            Debug.LogWarning("[BaseSceneBootstrap] BaseSceneUI が見つかりません。");
        }
        else
        {
            baseUI.Initialize(cookingMgr);
        }

        Debug.Log("[BaseSceneBootstrap] 拠点シーン結線完了。");
    }
}
