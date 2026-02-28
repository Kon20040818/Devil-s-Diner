using UnityEngine;

/// <summary>
/// アクションシーンからマネジメントシーンへ遷移するためのトリガーゾーン。
/// Noon フェーズ中にプレイヤーが触れると Evening へ進行し、ManagementScene がロードされる。
/// </summary>
public sealed class ReturnPortal : MonoBehaviour
{
    private bool _hasTriggered;

    private void Awake()
    {
        // 視覚的ヒント: 半透明の青色に設定
        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            var color = new Color(0f, 0.4f, 1f, 0.4f);
            // インスタンス化されたマテリアルを使用（共有マテリアルを汚さない）
            meshRenderer.material.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Noon) return;

        _hasTriggered = true;
        Debug.Log("[ReturnPortal] プレイヤーがポータルに接触 — Evening フェーズへ進行します");
        GameManager.Instance.AdvancePhase();
    }
}
