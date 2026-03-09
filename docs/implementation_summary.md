# Devil's Diner — 実装サマリーレポート

**出力日**: 2026-03-08
**エンジン**: Unity 6 (6000.3.10f1)
**言語**: C# (.NET Standard 2.1)
**総スクリプト数**: 65ファイル / 約23,900行

---

## 1. プロジェクト概要

「Devil's Diner」は悪魔シェフシミュレーター。3つのゲームフェーズで構成される：

| フェーズ | シーン | 内容 |
|----------|--------|------|
| 朝 (Morning) | BaseScene | 料理・装備・出撃準備・スタッフ管理・セーブ |
| 昼 (Noon) | FieldScene → BattleScene | フィールド探索 → エンカウント → ターン制バトル |
| 夜 (Evening) | ManagementScene | 店舗営業シミュレーション（接客・売上・評判） |

---

## 2. アーキテクチャ総覧

```
                    ┌─────────────┐
                    │ BootScene   │  GameManager (DontDestroyOnLoad)
                    │ BootLoader  │  AudioManager (DontDestroyOnLoad)
                    └──────┬──────┘
                           │ TransitionToScene
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
   ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
   │  BaseScene   │ │  FieldScene  │ │ ManagementScene  │
   │ BaseSceneUI  │ │ FieldPlayer  │ │ ManagementSceneUI│
   │ CookingMgr   │ │ EnemySymbol  │ │ DinerService     │
   └──────┬───────┘ └──────┬───────┘ └──────────────────┘
          │                │ Encounter
          │                ▼
          │         ┌──────────────┐
          │         │ BattleScene  │
          │         │ BattleManager│
          │         │ 12+ 戦闘系   │
          │         └──────────────┘
          │
          ▼
   ┌──────────────────────────────────────────┐
   │            GameManager (Singleton)        │
   │  Gold, Reputation, ChefLevel, Day        │
   │  InventoryManager, SaveDataManager       │
   │  StaffManager, HousingManager            │
   │  PendingBattleData, DailyFreshnessBuff   │
   └──────────────────────────────────────────┘
```

### 設計原則
- **Bootstrap パターン**: 各シーンに専用 Bootstrap がコンポーネント間参照を自動結線
- **ScriptableObject データ駆動**: 全マスターデータは SO で定義（Inspector チューニング可能）
- **イベント駆動**: `System.Action` デリゲートでシステム間を疎結合
- **UI Toolkit**: バトル UI・メニュー UI を全てプログラマティックに構築（UXML 最小使用）

---

## 3. モジュール別実装状況

### 3.1 バトルシステム (Assets/Scripts/Battle/) — 13ファイル / 3,564行

| コンポーネント | 行数 | 状態 | 概要 |
|--------------|------|------|------|
| BattleManager | 1,140 | 完成 | ターン制ステートマシン。SP/EP管理、全アクション統括 |
| CharacterBattleController | 407 | 完成 | キャラ個体管理。HP/EP/タフネス、ダメージ計算、ガード |
| AttackAction | 233 | 完成 | ジャストアタック QTE（ヒットストップ付き） |
| EnemyAttackAction | 302 | 完成 | ジャストガード QTE（3段階判定 + 早押しペナルティ） |
| ActionQueueSystem | 148 | 完成 | Star Rail 式スピード基準行動値キュー |
| BattleCameraManager | 190 | 完成 | Cinemachine 3.x カメラワーク（180度ルール厳守） |
| ScoutAction | 87 | 完成 | スカウトコマンド（HP比率基準 + 加速度曲線） |
| MealAction | 99 | 完成 | 食事コマンド（回復 + カテゴリバフ） |
| MealBuffApplier | 54 | 完成 | 食事バフ適用（ATK/SPD/DEF/リジェネ） |
| BuffDurationTracker | 181 | 完成 | バフ持続管理（ターン経過で自動解除） |
| EnemyAIController | 164 | 完成 | 敵AI（重み付きランダム行動選択） |
| BattleResultController | 300 | 完成 | 勝利/敗北処理、報酬集計、シーン遷移 |
| BattleSceneBootstrap | 259 | 完成 | バトルシーン自動結線 |

**サポートするアクション**: BasicAttack, Skill, Ultimate, Meal, Scout, Guard

### 3.2 コアシステム (Assets/Scripts/Core/) — 9ファイル / 1,893行

| コンポーネント | 行数 | 状態 | 概要 |
|--------------|------|------|------|
| GameManager | 482 | 完成 | シングルトン。フェーズ管理、通貨、レベル、シーン遷移 |
| InventoryManager | 256 | 完成 | アイテム + 料理（DishInstance）在庫管理 |
| SaveDataManager | 346 | 完成 | JSON セーブ/ロード（全データ永続化） |
| DropResolver | 93 | 完成 | 敵ドロップ判定（通常/ジャスト） |
| SkillEffectApplier | 59 | 完成 | グローバル戦闘補正値ホルダー |
| DebugController | 319 | 完成 | F1-F9 デバッグコマンド |
| AudioManager | 180 | 完成 | BGM/SE シングルトン |
| AudioEventConnector | 101 | 完成 | イベント→音声ブリッジ |
| BootLoader | 57 | 完成 | 起動 + タイトル画面表示 |

### 3.3 データ定義 (Assets/Scripts/Data/) — 23ファイル / 1,971行

| カテゴリ | ファイル | 概要 |
|----------|---------|------|
| アイテム基底 | ItemData | ID, 表示名, 説明, アイコン, 売却価格 |
| 素材 | IngredientData | レア度, ドロップ率, ゲージ速度倍率 |
| 武器 | WeaponData | ダメージ, 部位破壊値, ジャスト補正 |
| 料理 | DishData, DishInstance, DishCategory, DishQuality | 回復量, バフ, 店舗価格（品質スケール対応） |
| レシピ | RecipeData | 素材スロット[], 完成品, 必要シェフレベル |
| 品質 | QualityScaleTable | Poor/Normal/Fine/Exquisite 倍率テーブル |
| 敵 | EnemyData | HP, 攻撃力, ドロップ, ゴールド報酬, 種族紐付け |
| マップ | MapData | 環境タイプ, 推奨レベル, シーン名 |
| スタッフ | StaffRaceData, StaffBuffData, StaffInstance, StaffBuffSummary, RecruitedDemonData | 種族, バフ, 個体データ, 集計 |
| その他 | CharacterStats, BattleTransitionData, CalendarEventData, FurnitureData, DinerResult | キャラステ, バトル遷移, カレンダー, 家具, 営業結果 |

### 3.4 エディタツール (Assets/Scripts/Editor/) — 9ファイル / 2,786行

| ツール | メニュー | 概要 |
|--------|---------|------|
| BootSceneAutoBuilder | DevilsDiner > Auto Setup Boot Scene | BootScene 生成（Build Settings index 0） |
| BaseSceneAutoBuilder | DevilsDiner > Auto Setup Base Scene | BaseScene 生成 |
| FieldSceneAutoBuilder | DevilsDiner > Auto Setup Field Scene | FieldScene 生成（NavMesh ベイク必要） |
| ManagementSceneAutoBuilder | DevilsDiner > Auto Setup Management Scene | ManagementScene 生成 |
| DevilsDinerSetupTool | DevilsDiner > Auto Setup Battle Scene | BattleScene 生成 |
| SampleDataGenerator | DevilsDiner > Generate Sample Staff & Calendar Data | 全マスターデータ一括生成 |
| BattleCameraSetupWindow | DevilsDiner > Setup Battle Camera | Cinemachine ブレンド設定 |
| MetaphorUISetup | Tools > Setup Metaphor Battle UI | Metaphor 風 UI 切替 |
| InventoryTestTool | DevilsDiner > Inventory Test Tool | デバッグ用インベントリ操作 |

### 3.5 フィールドシステム (Assets/Scripts/Field/) — 7ファイル / 914行

| コンポーネント | 行数 | 概要 |
|--------------|------|------|
| FieldPlayerController | 165 | TPS 移動（カメラ相対 + スプリント） |
| FieldCameraController | 145 | Cinemachine 3.x TPS カメラ |
| EnemySymbol | 282 | NavMesh 3状態 FSM（Patrol/Chase/Return） |
| FieldEncounterHandler | 84 | エンカウント → BattleTransitionData 構築 |
| BaseSceneBootstrap | 55 | BaseScene 初期化 |
| FieldSceneBootstrap | 122 | FieldScene 結線 |
| ManagementSceneBootstrap | 61 | ManagementScene 結線 |

### 3.6 経営システム (Assets/Scripts/Management/) — 5ファイル / 940行

| コンポーネント | 行数 | 概要 |
|--------------|------|------|
| CookingManager | 207 | レシピ判定、品質計算、素材消費、XP付与 |
| DinerService | 153 | 営業シミュレーション（満足度・売上・評判） |
| StaffManager | 306 | スタッフ管理（雇用/解雇/昇格/給料） |
| StaffBuffRoller | 145 | スカウト結果→バフ抽選 |
| HousingManager | 129 | 家具購入・ボーナス集計 |

### 3.7 UI (Assets/Scripts/UI/) — 14ファイル / 11,861行

| コンポーネント | 行数 | 技術 | 概要 |
|--------------|------|------|------|
| SkillCommandUI | 1,976 | uGUI Canvas | Star Rail 風コマンドメニュー |
| UltimatePortraitUI | 1,936 | uGUI Canvas | EP ポートレート + 放射リング |
| BattleEffectsUI | 2,002 | uGUI Canvas | 全画面演出（ワイプ, カットイン, フラッシュ） |
| DynamicBattleUIController | 1,417 | UI Toolkit | Metaphor 風リボルバー UI |
| ManagementSceneUI | 1,162 | UI Toolkit | 経営パネル4面 |
| EnemyStatusUI | 1,142 | uGUI Canvas | 敵頭上ビルボード |
| BaseSceneUI | 1,118 | UI Toolkit | 拠点ハブメニュー |
| CharacterStatusUI | 792 | uGUI Canvas | 味方 HP カードバー |
| ActionTimelineUI | 651 | UI Toolkit | 行動順タイムライン |
| RevolverMenuController | 628 | UI Toolkit | リボルバーコマンド選択 |
| DamageNumberUI | 453 | uGUI Canvas | フローティングダメージ数値 |
| BattleUIManager | 339 | ハブ | UI イベントルーター |
| BattleResultUI | 238 | UI Toolkit | 勝利/敗北リザルト |
| TitleScreenUI | 203 | UI Toolkit | タイトル画面 |

---

## 4. データパイプライン

### 4.1 素材→料理→販売フロー
```
敵ドロップ (EnemyData._dropItemNormal/Just)
    │  IngredientData
    ▼
InventoryManager.Add(IngredientData)
    │
    ▼
CookingManager.Cook(RecipeData)
    │  素材消費 + 品質計算
    ▼
DishInstance (DishData + DishQuality)
    │
    ├──▶ InventoryManager.AddDish()  → 店舗メニュー在庫
    │        │
    │        ▼
    │    DinerService.RunService()  → 売上・評判
    │
    └──▶ MealAction (バトル中)  → HP回復 + カテゴリバフ
```

### 4.2 スカウト→スタッフフロー
```
BattleManager.Scout(enemy)
    │  成功判定
    ▼
BattleResultController.ProcessScoutedDemons()
    │
    ▼
StaffBuffRoller.RollAll()
    │  StaffRaceData → 重み付きバフ抽選
    ▼
RecruitedDemonData[]
    │
    ▼
StaffManager.ReceiveRecruits()
    │  空きスロットに配置
    ▼
StaffInstance (種族固定効果 + ランダムバフ)
    │
    ▼
StaffManager.GetActiveBonuses() → StaffBuffSummary
    │
    ├──▶ CookingManager (品質ボーナス)
    └──▶ DinerService (満足度ボーナス)
```

### 4.3 品質計算式
```
qualityScore = baseScore
    × ingredientRarityAvg  (レシピ素材の平均レア度)
    × freshnessBuff        (バトル成績由来)
    × calendarMultiplier   (カレンダーイベント)
    × staffBonus           (スタッフバフ合算)

DishQuality:
  qualityScore < 0.4  → Poor
  qualityScore < 0.7  → Normal
  qualityScore < 0.9  → Fine
  qualityScore >= 0.9 → Exquisite

各パラメータ = base × QualityScaleTable[quality]
```

---

## 5. ScriptableObject アセット一覧

| ディレクトリ | プレフィックス | アセット数 | 生成方法 |
|------------|-------------|----------|---------|
| Data/Characters/ | STAT_ | 2 | 手動 |
| Data/Enemies/ | ENM_ | 5 | DevilsDinerSetupTool |
| Data/Maps/ | MAP_ | 5 | 手動 |
| Data/Weapons/ | WPN_ | 6 | DevilsDinerSetupTool |
| Data/Materials/ | MAT_ | 5 | DevilsDinerSetupTool (Legacy) |
| Data/Ingredients/ | ING_ | 6 | SampleDataGenerator |
| Data/Dishes/ | DISH_ | 6 | SampleDataGenerator |
| Data/Recipes/ | RCP_ | 6 | SampleDataGenerator |
| Data/StaffBuffs/ | SBUF_ | 8 | SampleDataGenerator |
| Data/StaffRaces/ | RACE_ | 5 | SampleDataGenerator |
| Data/CalendarEvents/ | CAL_ | 3 | SampleDataGenerator |
| Data/Furniture/ | FRN_ | 6 | SampleDataGenerator |
| Data/Skills/ | SKL_ | 5 | DevilsDinerSetupTool |
| Data/ | QualityScaleTable | 1 | SampleDataGenerator |
| Data/Config/ | CookingConfig, JustInputConfig | 2 | DevilsDinerSetupTool |

---

## 6. シーン構成

| シーン | Build Index | 生成ツール | Bootstrap | 状態 |
|--------|------------|-----------|-----------|------|
| BootScene | 0 | BootSceneAutoBuilder | — (BootLoader) | 要生成 |
| BaseScene | 1 | BaseSceneAutoBuilder | BaseSceneBootstrap | 要生成 |
| FieldScene | 2 | FieldSceneAutoBuilder | FieldSceneBootstrap | 要生成 |
| BattleScene | 3 | DevilsDinerSetupTool | BattleSceneBootstrap | 生成済み |
| ManagementScene | 4 | ManagementSceneAutoBuilder | ManagementSceneBootstrap | 要生成 |

---

## 7. 未実装・未完了項目

| 項目 | 種別 | 優先度 | 備考 |
|------|------|--------|------|
| シーン生成（Boot/Base/Field/Management） | エディタ操作 | 最高 | AutoBuilder を実行するだけ |
| NavMesh ベイク（FieldScene） | エディタ操作 | 最高 | FieldScene 生成後に必須 |
| SampleDataGenerator 実行 | エディタ操作 | 最高 | 全マスターデータ生成 |
| AudioManager SE 登録 | エディタ操作 | 高 | Inspector で AudioClip を設定 |
| BGM AudioClip 割り当て | エディタ操作 | 中 | シーン別 BGM クリップ |
| Cinemachine カメラリグ確認 | エディタ操作 | 中 | BattleScene 内の VCam 構成確認 |
| ShopLevel プロパティ追加 | コード | 低 | MapData.RequiredShopLevel と連動（現在は全マップ表示） |
| チュートリアル | コード + データ | 低 | 初回プレイヤー向けガイド |

---

## 8. Unity エディタでの初回セットアップ手順

```
1. DevilsDiner > Generate Sample Staff & Calendar Data
   → IngredientData / DishData / RecipeData / StaffBuff / StaffRace /
     CalendarEvent / Furniture / QualityScaleTable 生成
   → EnemyData にドロップ枠 + StaffRace 自動結線

2. DevilsDiner > Auto Setup Boot Scene
   → BootScene.unity 生成（Build Settings index 0）

3. DevilsDiner > Auto Setup Base Scene
   → BaseScene.unity 生成

4. DevilsDiner > Auto Setup Field Scene
   → FieldScene.unity 生成
   → ★ Window > AI > Navigation で NavMesh をベイク

5. DevilsDiner > Auto Setup Battle Scene
   → BattleScene.unity 再生成（最新コンポーネント反映）

6. DevilsDiner > Auto Setup Management Scene
   → ManagementScene.unity 生成

7. Play → BootScene から開始
```
