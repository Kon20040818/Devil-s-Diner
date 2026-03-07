// ============================================================
// BattleSceneBootstrap.cs
// BattleScene のブートストラップ。シーンロード時に各コンポーネント間の
// 参照を自動結線し、バトルを開始する。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BattleScene 起動時にバトルシステムとUIを初期化・結線するブートストラップ。
/// シーン内の「BattleSystem」GameObject にアタッチして使用する。
/// </summary>
public sealed class BattleSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        WireAndStartBattle();
    }

    private void WireAndStartBattle()
    {
        // ── コンポーネント検索 ──
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        BattleCameraManager cameraManager = FindFirstObjectByType<BattleCameraManager>();
        BattleUIManager uiManager = FindFirstObjectByType<BattleUIManager>();

        if (battleManager == null)
        {
            Debug.LogError("[BattleSceneBootstrap] BattleManager が見つかりません。");
            return;
        }

        // ── カメラマネージャー結線 ──
        if (cameraManager != null)
        {
            battleManager.SetCameraManager(cameraManager);
        }
        else
        {
            Debug.LogWarning("[BattleSceneBootstrap] BattleCameraManager が見つかりません。");
        }

        // ── SkillEffectApplier を追加 ──
        if (FindFirstObjectByType<SkillEffectApplier>() == null)
        {
            gameObject.AddComponent<SkillEffectApplier>();
        }

        // ── パーティ検索 ──
        CharacterBattleController[] allCharacters = FindObjectsByType<CharacterBattleController>(FindObjectsSortMode.None);

        var playerList = new System.Collections.Generic.List<CharacterBattleController>();
        var enemyList = new System.Collections.Generic.List<CharacterBattleController>();

        foreach (var c in allCharacters)
        {
            if (c.CharacterFaction == CharacterBattleController.Faction.Player)
                playerList.Add(c);
            else
                enemyList.Add(c);
        }

        if (playerList.Count == 0)
        {
            Debug.LogError("[BattleSceneBootstrap] 味方キャラクターが見つかりません。");
            return;
        }
        if (enemyList.Count == 0)
        {
            Debug.LogError("[BattleSceneBootstrap] 敵キャラクターが見つかりません。");
            return;
        }

        Debug.Log($"[BattleSceneBootstrap] 味方 {playerList.Count}体, 敵 {enemyList.Count}体 を検出。");

        // ── SkillCommandUI 検索・結線 ──
        SkillCommandUI skillCommandUI = FindFirstObjectByType<SkillCommandUI>();
        if (skillCommandUI != null && uiManager != null)
        {
            uiManager.SetSkillCommandUI(skillCommandUI);
        }

        // ── UltimatePortraitUI 検索・結線 ──
        UltimatePortraitUI ultimatePortraitUI = FindFirstObjectByType<UltimatePortraitUI>();
        if (ultimatePortraitUI != null && uiManager != null)
        {
            uiManager.SetUltimatePortraitUI(ultimatePortraitUI);
        }

        // ── AttackAction（ジャストアタック）検索・自動生成・結線 ──
        AttackAction attackAction = FindFirstObjectByType<AttackAction>();
        if (attackAction == null)
        {
            attackAction = gameObject.AddComponent<AttackAction>();
            Debug.Log("[BattleSceneBootstrap] AttackAction を自動生成しました。");
        }
        battleManager.SetAttackAction(attackAction);

        // ── EnemyAttackAction（ジャストガード）検索・自動生成・結線 ──
        EnemyAttackAction enemyAttackAction = FindFirstObjectByType<EnemyAttackAction>();
        if (enemyAttackAction == null)
        {
            enemyAttackAction = gameObject.AddComponent<EnemyAttackAction>();
            Debug.Log("[BattleSceneBootstrap] EnemyAttackAction を自動生成しました。");
        }
        battleManager.SetEnemyAttackAction(enemyAttackAction);

        // ── MealAction（食事）検索・自動生成・結線 ──
        MealAction mealAction = FindFirstObjectByType<MealAction>();
        if (mealAction == null)
        {
            mealAction = gameObject.AddComponent<MealAction>();
            Debug.Log("[BattleSceneBootstrap] MealAction を自動生成しました。");
        }
        battleManager.SetMealAction(mealAction);

        // ── MealBuffApplier（食事バフ）検索・自動生成・結線 ──
        MealBuffApplier mealBuffApplier = FindFirstObjectByType<MealBuffApplier>();
        if (mealBuffApplier == null)
        {
            mealBuffApplier = gameObject.AddComponent<MealBuffApplier>();
            Debug.Log("[BattleSceneBootstrap] MealBuffApplier を自動生成しました。");
        }
        mealAction.SetBuffApplier(mealBuffApplier);

        // ── BuffDurationTracker（バフ持続管理）自動生成・結線 ──
        BuffDurationTracker buffTracker = gameObject.AddComponent<BuffDurationTracker>();
        battleManager.SetBuffDurationTracker(buffTracker);
        Debug.Log("[BattleSceneBootstrap] BuffDurationTracker を自動生成しました。");

        // ── EnemyAIController（敵AI）自動生成・結線 ──
        EnemyAIController enemyAI = gameObject.AddComponent<EnemyAIController>();
        battleManager.SetEnemyAIController(enemyAI);
        Debug.Log("[BattleSceneBootstrap] EnemyAIController を自動生成しました。");

        // ── 敵キャラに EnemyData を結線（ドロップ・ゴールド報酬用） ──
        EnemyData[] allEnemyData = Resources.LoadAll<EnemyData>("");
        var enemyDataLookup = new Dictionary<string, EnemyData>();
        foreach (var ed in allEnemyData)
        {
            if (ed != null && !string.IsNullOrEmpty(ed.Id))
                enemyDataLookup[ed.Id] = ed;
        }

        foreach (var enemy in enemyList)
        {
            if (enemy.Stats != null && enemyDataLookup.TryGetValue(enemy.Stats.Id, out var eData))
            {
                enemy.SetEnemyData(eData);
                Debug.Log($"[BattleSceneBootstrap] {enemy.DisplayName} に EnemyData '{eData.Id}' を結線。");
            }
        }

        // ── バトル開始（パーティ配列をセットし、キューを構築する） ──
        battleManager.StartBattle(playerList.ToArray(), enemyList.ToArray());

        // ── UI初期化（PlayerParty/EnemyParty が確定した後に呼ぶ） ──
        if (uiManager != null)
        {
            uiManager.Initialize(battleManager);
            Debug.Log("[BattleSceneBootstrap] BattleUIManager を初期化しました。");
        }

        // ── DynamicBattleUIController（Metaphor UI + ダイナミック追従）検索・初期化 ──
        DynamicBattleUIController dynamicUI = FindFirstObjectByType<DynamicBattleUIController>();
        if (dynamicUI != null)
        {
            dynamicUI.Initialize(battleManager);
            Debug.Log("[BattleSceneBootstrap] DynamicBattleUIController を初期化しました。");
        }

        // ── BattleResultController（リザルト画面＋シーン遷移）自動生成・結線 ──
        BattleResultController resultController = gameObject.AddComponent<BattleResultController>();
        resultController.Initialize(battleManager);
        Debug.Log("[BattleSceneBootstrap] BattleResultController を自動生成しました。");
    }
}
