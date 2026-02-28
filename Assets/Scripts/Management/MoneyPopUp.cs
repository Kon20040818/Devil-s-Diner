// ============================================================
// MoneyPopUp.cs
// 客の支払い時に頭上に「+150G」テキストがフワッと浮かび上がって消える演出。
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 客の支払い時に金額テキストを頭上にポップアップ表示するコンポーネント。
/// DinerManager から支払い通知を受けてワールド空間UIテキストを生成する。
/// </summary>
public sealed class MoneyPopUp : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float POPUP_DURATION = 1.5f;
    private const float FLOAT_HEIGHT = 1.5f;
    private const float POPUP_START_Y_OFFSET = 2f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("参照")]
    [SerializeField] private Canvas _worldCanvas;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 指定ワールド座標に金額ポップアップを表示する。
    /// </summary>
    /// <param name="worldPosition">ポップアップの開始ワールド座標。</param>
    /// <param name="amount">表示する金額。</param>
    public void ShowPopUp(Vector3 worldPosition, int amount)
    {
        if (_worldCanvas == null) return;

        StartCoroutine(PopUpCoroutine(worldPosition, amount));
    }

    // ──────────────────────────────────────────────
    // コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator PopUpCoroutine(Vector3 worldPosition, int amount)
    {
        // テキストオブジェクト生成
        GameObject popObj = new GameObject("MoneyPopUp");
        popObj.transform.SetParent(_worldCanvas.transform, false);

        Text popText = popObj.AddComponent<Text>();
        popText.text = $"+{amount}G";
        popText.fontSize = 28;
        popText.color = new Color(1f, 0.9f, 0.2f, 1f); // ゴールド色
        popText.alignment = TextAnchor.MiddleCenter;
        popText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (popText.font == null)
        {
            popText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Shadow コンポーネントで視認性向上
        Shadow shadow = popObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(1f, -1f);

        RectTransform popRect = popObj.GetComponent<RectTransform>();
        popRect.sizeDelta = new Vector2(150f, 40f);

        // 開始位置
        Vector3 startPos = worldPosition + Vector3.up * POPUP_START_Y_OFFSET;
        Vector3 endPos = startPos + Vector3.up * FLOAT_HEIGHT;

        float elapsed = 0f;
        Camera cam = Camera.main;

        while (elapsed < POPUP_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / POPUP_DURATION);

            // ワールド → スクリーン座標変換
            Vector3 currentWorldPos = Vector3.Lerp(startPos, endPos, t);

            if (cam != null)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(currentWorldPos);

                // カメラの後ろにある場合は非表示
                if (screenPos.z < 0)
                {
                    popObj.SetActive(false);
                }
                else
                {
                    popObj.SetActive(true);
                    popRect.position = screenPos;
                }
            }

            // フェードアウト (後半50%でフェード)
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
            popText.color = new Color(popText.color.r, popText.color.g, popText.color.b, alpha);

            yield return null;
        }

        Destroy(popObj);
    }
}
