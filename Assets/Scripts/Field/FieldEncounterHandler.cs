// ============================================================
// FieldEncounterHandler.cs
// フィールド上のシンボルエンカウントからバトルシーンへの遷移を仲介する。
// ============================================================
using System.Collections;
using UnityEngine;

/// <summary>
/// EnemySymbol の OnEncounter イベントを受けて
/// BattleTransitionData を構築し、バトルシーンへ遷移する。
/// </summary>
public sealed class FieldEncounterHandler : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("シーン遷移")]
    [Tooltip("バトルシーン名")]
    [SerializeField] private string _battleSceneName = "BattleScene";

    [Tooltip("遷移前の演出待ち時間（秒）")]
    [SerializeField] private float _transitionDelay = 0.5f;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private bool _isTransitioning;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 敵シンボルの OnEncounter イベントを購読する。
    /// </summary>
    public void RegisterSymbol(EnemySymbol symbol)
    {
        if (symbol == null) return;
        symbol.OnEncounter += HandleEncounter;
    }

    // ──────────────────────────────────────────────
    // エンカウント処理
    // ──────────────────────────────────────────────

    private void HandleEncounter(EnemySymbol symbol)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        Debug.Log($"[FieldEncounter] {symbol.EnemyData?.EnemyName ?? "Unknown"} とエンカウント！");

        // BattleTransitionData を構築して GameManager に設定
        var transitionData = new BattleTransitionData
        {
            EnemyDataList = symbol.EnemyData != null
                ? new EnemyData[] { symbol.EnemyData }
                : new EnemyData[0],
            EnemyStatsList = symbol.EnemyStats != null
                ? new CharacterStats[] { symbol.EnemyStats }
                : new CharacterStats[0],
            ReturnSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PendingBattleData = transitionData;
        }

        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        yield return new WaitForSeconds(_transitionDelay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TransitionToScene(_battleSceneName);
        }
    }
}
