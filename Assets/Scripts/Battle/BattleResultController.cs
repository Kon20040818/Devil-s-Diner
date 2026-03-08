// ============================================================
// BattleResultController.cs
// バトル終了後の報酬集計・演出・シーン遷移を担当する独立コンポーネント。
// BattleManager.OnBattleEnd を購読し、ポストバトルフローを制御する。
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// バトル終了後の報酬処理とリザルト表示を管理するコントローラー。
/// BattleSceneBootstrap で自動生成され、BattleManager.OnBattleEnd イベントで起動する。
/// </summary>
public sealed class BattleResultController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("タイミング設定")]
    [Tooltip("バトル終了からリザルト表示開始までの遅延（秒）")]
    [SerializeField] private float _delayBeforeResult = 1.5f;

    [Tooltip("リザルト画面の表示時間（秒）")]
    [SerializeField] private float _resultDisplayDuration = 4.0f;

    [Header("シーン遷移")]
    [Tooltip("バトル後に遷移するシーン名")]
    [SerializeField] private string _returnSceneName = "FieldScene";

    // ──────────────────────────────────────────────
    // 参照
    // ──────────────────────────────────────────────

    private BattleManager _battleManager;
    private BattleResultUI _resultUI;
    private BattleEffectsUI _effectsUI;

    // ──────────────────────────────────────────────
    // 初期化
    // ──────────────────────────────────────────────

    /// <summary>BattleSceneBootstrap から呼ばれる初期化。</summary>
    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;
        _battleManager.OnBattleEnd += HandleBattleEnd;
        _resultUI = FindFirstObjectByType<BattleResultUI>();
        _effectsUI = FindFirstObjectByType<BattleEffectsUI>();
    }

    /// <summary>バトル後の遷移先シーン名を設定する。</summary>
    public void SetReturnSceneName(string sceneName) => _returnSceneName = sceneName;

    private void OnDestroy()
    {
        if (_battleManager != null)
            _battleManager.OnBattleEnd -= HandleBattleEnd;
    }

    // ──────────────────────────────────────────────
    // バトル終了ハンドラ
    // ──────────────────────────────────────────────

    private void HandleBattleEnd(bool isVictory)
    {
        StartCoroutine(ResultSequence(isVictory));
    }

    // ──────────────────────────────────────────────
    // リザルトシーケンス
    // ──────────────────────────────────────────────

    private IEnumerator ResultSequence(bool isVictory)
    {
        yield return new WaitForSecondsRealtime(_delayBeforeResult);

        if (isVictory)
        {
            yield return StartCoroutine(VictorySequence());
        }
        else
        {
            yield return StartCoroutine(DefeatSequence());
        }

        // リザルト表示時間
        yield return new WaitForSecondsRealtime(_resultDisplayDuration);

        // シーン遷移
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TransitionToScene(_returnSceneName);
        }
    }

    // ──────────────────────────────────────────────
    // 勝利処理
    // ──────────────────────────────────────────────

    private IEnumerator VictorySequence()
    {
        // ドロップ解決
        var drops = ResolveAllDrops();
        int totalGold = CalculateTotalGold();

        // ゴールド加算
        if (GameManager.Instance != null && totalGold > 0)
        {
            GameManager.Instance.AddGold(totalGold);
        }

        // ドロップアイテムをインベントリに追加
        foreach (var drop in drops)
        {
            if (drop.Success && drop.DroppedItem != null && GameManager.Instance != null)
            {
                GameManager.Instance.Inventory.Add(drop.DroppedItem);
            }
        }

        Debug.Log($"[BattleResult] 勝利！ ゴールド: +{totalGold}G, ドロップ: {drops.Count}件判定");

        // スカウト結果のバフ抽選 → StaffManager へ引き渡し
        ProcessScoutedDemons();

        // 勝利演出
        if (_effectsUI != null)
            yield return _effectsUI.PlayVictoryEffect();

        // リザルトUI表示
        if (_resultUI != null)
            _resultUI.ShowVictory(totalGold, drops);
    }

    // ──────────────────────────────────────────────
    // スカウト結果処理
    // ──────────────────────────────────────────────

    /// <summary>スカウト済み悪魔のバフを一括抽選し、StaffManager に渡す。</summary>
    private void ProcessScoutedDemons()
    {
        if (_battleManager == null || _battleManager.ScoutedEnemies.Count == 0) return;

        var recruits = StaffBuffRoller.RollAll(_battleManager.ScoutedEnemies);

        if (GameManager.Instance != null && GameManager.Instance.Staff != null)
        {
            GameManager.Instance.Staff.ReceiveRecruits(recruits);
        }
        else
        {
            Debug.LogWarning("[BattleResult] StaffManager が未初期化のためスカウト結果を保存できません。");
        }
    }

    // ──────────────────────────────────────────────
    // 敗北処理
    // ──────────────────────────────────────────────

    private IEnumerator DefeatSequence()
    {
        Debug.Log("[BattleResult] 敗北...");

        // 敗北演出
        if (_effectsUI != null)
            yield return _effectsUI.PlayDefeatEffect();

        // リザルトUI表示
        if (_resultUI != null)
            _resultUI.ShowDefeat();
    }

    // ──────────────────────────────────────────────
    // 報酬計算
    // ──────────────────────────────────────────────

    /// <summary>全敵のドロップを判定して結果リストを返す。</summary>
    private List<DropResolver.DropResult> ResolveAllDrops()
    {
        var results = new List<DropResolver.DropResult>();

        if (_battleManager == null || _battleManager.EnemyParty == null) return results;

        foreach (var enemy in _battleManager.EnemyParty)
        {
            if (enemy == null) continue;

            // スカウト済みの敵はドロップなし
            if (enemy.IsScouted) continue;

            EnemyData enemyData = enemy.EnemyData;
            if (enemyData == null) continue;

            var result = DropResolver.ResolveDropWithResult(enemyData, false);
            results.Add(result);

            if (result.Success)
            {
                Debug.Log($"[BattleResult] ドロップ: {result.DroppedItem.DisplayName} ({enemy.DisplayName})");
            }
        }

        return results;
    }

    /// <summary>全敵のゴールド報酬合計を返す。</summary>
    private int CalculateTotalGold()
    {
        int total = 0;

        if (_battleManager == null || _battleManager.EnemyParty == null) return total;

        foreach (var enemy in _battleManager.EnemyParty)
        {
            if (enemy == null) continue;
            if (enemy.IsScouted) continue;

            EnemyData enemyData = enemy.EnemyData;
            if (enemyData != null)
            {
                total += enemyData.GoldReward;
            }
        }

        return total;
    }
}
