# Devil's Diner — 基本設計書

**バージョン**: 1.0
**作成日**: 2026-03-08
**エンジン**: Unity 6 (6000.3.10f1)

---

## 第1章 ゲーム概要

### 1.1 コンセプト
プレイヤーは魔界でダイナーを経営する悪魔シェフ。
昼は魔物と戦って食材を調達し、夜は料理して客に振る舞い、店の評判を上げていく。
倒した敵をスカウトしてスタッフに雇うことで経営効率が向上する、
バトルと経営が有機的に連携するゲームループ。

### 1.2 ゲームループ
```
     ┌──────────────────────────────────────────────────┐
     │                  1日のサイクル                     │
     │                                                    │
     │  ┌─────────┐    ┌─────────┐    ┌──────────────┐  │
     │  │  朝      │ →  │  昼      │ →  │  夜           │  │
     │  │ BaseScene│    │Field    │    │Management   │  │
     │  │ 準備     │    │Battle   │    │Scene        │  │
     │  └─────────┘    └─────────┘    └──────────────┘  │
     │    料理            探索              店舗営業       │
     │    装備            戦闘              売上集計       │
     │    出撃先選択      素材獲得          評判変動       │
     │    スタッフ管理    スカウト          → 翌日の朝へ   │
     └──────────────────────────────────────────────────┘
```

### 1.3 コアメカニクス
1. **ターン制バトル** — Star Rail 式スピード基準行動値キュー + ジャストアタック/ガード QTE
2. **料理システム** — 素材 × レシピ → 品質計算 → DishInstance（バトル回復 + 店舗販売）
3. **店舗経営** — 料理メニュー設定 → 客満足度シミュレーション → 売上 + 評判
4. **スカウト & スタッフ** — 敵をスカウト → バフ抽選 → スタッフ配置 → 経営ボーナス

---

## 第2章 システムアーキテクチャ

### 2.1 シーン構成

| シーン | 役割 | Bootstrap |
|--------|------|-----------|
| BootScene | GameManager 生成 + タイトル画面 | BootLoader |
| BaseScene | 朝の拠点ハブ（料理/装備/出撃/スタッフ/セーブ） | BaseSceneBootstrap |
| FieldScene | フィールド探索（TPS 移動 + 敵シンボルエンカウント） | FieldSceneBootstrap |
| BattleScene | ターン制バトル | BattleSceneBootstrap |
| ManagementScene | 夜の店舗経営（営業/メニュー/改装/スタッフ） | ManagementSceneBootstrap |

### 2.2 永続データ管理

```
GameManager (DontDestroyOnLoad)
├── Gold: int                    通貨
├── CurrentDay: int              経過日数
├── Reputation: float            店舗評判 (0.0~)
├── ChefLevel: int               シェフレベル
├── CookingXP: int               調理経験値
├── DailyFreshnessBuff: float    バトル成績由来の鮮度倍率
├── EquippedWeaponID: string     装備中の武器
├── PendingBattleData             フィールド→バトル遷移データ
│
├── InventoryManager
│   ├── Dictionary<ItemData, int>      アイテム在庫
│   └── Dictionary<DishInstance, int>  料理在庫（品質付き）
│
├── StaffManager
│   ├── StaffInstance[3]  永続スタッフスロット
│   └── StaffInstance[2]  臨時スタッフスロット
│
├── HousingManager
│   └── List<FurnitureData>  所持家具
│
├── SaveDataManager
│   └── JSON ファイル永続化
│
└── AudioManager (DontDestroyOnLoad)
    ├── BGM AudioSource
    └── SE AudioSource
```

### 2.3 フェーズ遷移

```csharp
enum GamePhase { Morning, Noon, Evening }

// GameManager.AdvancePhase()
Morning → Noon     : TransitionToScene("FieldScene")
Noon    → Evening  : TransitionToScene("ManagementScene")
Evening → Morning  : CurrentDay++, TransitionToScene("BaseScene")
```

---

## 第3章 バトルシステム設計

### 3.1 行動値キュー (ActionQueueSystem)

Star Rail 方式のスピード基準ターン制：
- 各キャラクターに `ActionValue` を持たせる
- 毎ターン全員の AV を減少させ、AV ≤ 0 になったキャラが行動
- `ActionValue = BASE_AV / (Speed × SpeedMultiplier)`

### 3.2 アクションタイプ

| ActionType | 説明 | SP消費 | ターゲット |
|-----------|------|--------|----------|
| BasicAttack | 通常攻撃（ジャスト QTE 付き） | +1 | 単体 |
| Skill | スキル攻撃（AoE 対応） | -1 | 単体/全体 |
| Ultimate | 必殺技（EP 100 で割り込み発動） | 0 | 単体/全体 |
| Meal | 食事（HP回復 + カテゴリバフ） | 0 | 自身 |
| Scout | スカウト（敵を仲間に勧誘） | 0 | 単体 |
| Guard | 防御（被ダメ50%減 + SP+1） | +1 | 自身 |

### 3.3 ジャストアタック

```
攻撃モーション開始
    │
    ├── _justWindowStart ～ _justWindowEnd (フレーム数)
    │     ↓ ボタン入力判定
    │     ├── ジャスト成功: ダメージ × _justMultiplier + ヒットストップ
    │     └── 通常ヒット:   ダメージ × 1.0
    │
    └── ウィンドウ外: ミス → ダメージ × 0.5
```

### 3.4 ジャストガード

```
敵攻撃モーション開始
    │
    ├── _justGuardStart ～ _justGuardEnd (フレーム数)
    │     ↓ ボタン入力判定
    │     ├── ジャストガード: ダメージ × 0.1 + タフネス回復
    │     ├── 通常ガード:     ダメージ × 0.5
    │     └── 失敗:           ダメージ × 1.0
    │
    └── 早押しペナルティ: _prematureInputPenalty 適用
```

### 3.5 ダメージ計算式

```
baseDamage = ATK × skillMultiplier × weaponDamage × attackMultiplier
resistance = 属性耐性 (0.0 ~ 1.0)
breakBonus = タフネス0 なら 1.5x
guardReduction = ガード中なら 0.5x

finalDamage = max(baseDamage × (1 - resistance) × breakBonus × guardReduction, 0)
```

### 3.6 EP システム（必殺技）

- 被ダメージ時: `EP += (damage / maxHP) × epChargeRatio × 100`
- 通常攻撃時: `EP += epChargeOnAttack`
- EP ≥ 100 で Ultimate 発動可能（数字キー 1-4 で割り込み）

### 3.7 タフネス（部位破壊）

- 弱点属性でヒット → タフネスゲージ減少
- タフネス 0 → **BREAK** 状態（被ダメ 1.5x）
- 自動回復: ターン経過で `_toughnessRegenRate` ずつ回復

### 3.8 バトル終了処理

```
勝利:
  DropResolver → アイテムドロップ判定
  CalculateTotalGold → ゴールド加算
  ProcessScoutedDemons → スカウト結果処理
  ComputeAndApplyFreshnessBuff → 鮮度バフ計算
  VictoryEffect → ResultUI → 自動遷移 (FieldScene)

敗北:
  DefeatEffect → ResultUI → 選択待ち
    ├── 拠点に帰還 → BaseScene
    └── リトライ → BattleScene 再ロード
```

---

## 第4章 経営システム設計

### 4.1 料理システム (CookingManager)

#### レシピ構造
```
RecipeData
├── RecipeID: string
├── DisplayName: string
├── OutputDish: DishData       完成品
├── Ingredients: IngredientSlot[]
│   ├── Ingredient: IngredientData
│   └── Amount: int
└── RequiredChefLevel: int     解放条件
```

#### 品質計算
```csharp
float qualityScore = baseScore
    × AverageIngredientRarity()     // 素材レア度平均
    × GameManager.DailyFreshnessBuff // バトル成績
    × calendarMultiplier             // カレンダーイベント
    × (1 + staffQualityBonus)        // スタッフバフ

// 品質判定
if (score < 0.4f) quality = Poor;
else if (score < 0.7f) quality = Normal;
else if (score < 0.9f) quality = Fine;
else quality = Exquisite;
```

#### 品質倍率テーブル (QualityScaleTable)

| パラメータ | Poor | Normal | Fine | Exquisite |
|-----------|------|--------|------|-----------|
| 回復量 | 0.6x | 1.0x | 1.3x | 1.6x |
| バフ値 | 0.5x | 1.0x | 1.4x | 1.8x |
| スカウトボーナス | 0.5x | 1.0x | 1.3x | 1.5x |
| 店舗価格 | 0.7x | 1.0x | 1.5x | 2.0x |
| 満足度 | 0.6x | 1.0x | 1.4x | 1.8x |

### 4.2 店舗営業 (DinerService)

```
RunService(menu: DishInstance[], customerCount)
  │
  ├── 各客ごと:
  │     baseSatisfaction = dish.GetSatisfaction(quality)
  │     × (1 + staffSatisfactionBonus)
  │     × (1 + furnitureSatisfactionBonus)
  │     × calendarSatisfactionMultiplier
  │     = finalSatisfaction
  │
  │     revenue = dish.GetShopPrice(quality)
  │     tip = revenue × max(0, (satisfaction - 50) / 100) × tipRate
  │
  └── 集計:
        totalRevenue, totalTips, avgSatisfaction
        reputationChange = (avgSatisfaction - 50) × reputationCoeff
```

### 4.3 スタッフシステム (StaffManager)

#### スロット構成
| スロット種別 | 数 | 特徴 |
|-------------|---|------|
| Permanent | 3 | 解雇まで永続。昇格可能 |
| Temporary | 2 | 一定期間で契約終了 |

#### バフ抽選 (StaffBuffRoller)
```
1. スカウト成功した敵 → EnemyData.StaffRace 取得
2. StaffRaceData.PossibleBuffs[] から重み付き抽選
   - 重み = 6 - Rarity（低レア → 高出現率）
   - MinBuffCount ～ MaxBuffCount 個を重複なし抽選
3. RecruitedDemonData に固定効果 + 抽選バフを格納
4. StaffManager.ReceiveRecruits() で空きスロットに配置
```

#### バフ種別
| StaffBuffType | 効果 |
|--------------|------|
| QualityBonus | 料理品質ボーナス |
| SatisfactionBonus | 客満足度ボーナス |
| FreshnessBonus | 鮮度ボーナス |
| CookSpeed | 調理速度ボーナス |
| SalaryReduction | 給料割引 |
| CategorySpecialty | 特定カテゴリ強化 |

### 4.4 家具システム (HousingManager)

```
FurnitureData
├── Type: Table / Decoration / Kitchen
├── Price: int
├── SatisfactionBonus: float    全体満足度加算
└── CustomerBonus: int          来客数加算
```

---

## 第5章 フィールドシステム設計

### 5.1 移動
- TPS カメラ相対移動（WASD + マウスルック）
- Shift でスプリント
- Cinemachine 3.x による TPS フォローカメラ

### 5.2 敵シンボル (EnemySymbol)

3状態 FSM:
```
Patrol (巡回)
  │ プレイヤー検知 (距離 + 視野角)
  ▼
Chase (追跡)
  │ プレイヤーに接触
  ▼
Encounter → BattleScene 遷移

Chase → 見失い → Returning (帰還)
  │ 初期位置に到達
  ▼
Patrol
```

### 5.3 エンカウントフロー
```
EnemySymbol.OnEncounter
    │
    ▼
FieldEncounterHandler
    │ BattleTransitionData 構築
    │   - EnemyDataList: EnemyData[]
    │   - EnemyStatsList: CharacterStats[]
    │   - ReturnSceneName: "FieldScene"
    ▼
GameManager.PendingBattleData = data
GameManager.TransitionToScene("BattleScene")
    │
    ▼
BattleSceneBootstrap
    │ PendingBattleData から敵を動的生成
    ▼
BattleManager.StartBattle()
```

---

## 第6章 データ設計

### 6.1 マスターデータ（ScriptableObject）

```
Data/
├── Characters/     STAT_*.asset   (CharacterStats)
├── Enemies/        ENM_*.asset    (EnemyData)
├── Ingredients/    ING_*.asset    (IngredientData)
├── Dishes/         DISH_*.asset   (DishData)
├── Recipes/        RCP_*.asset    (RecipeData)
├── Weapons/        WPN_*.asset    (WeaponData)
├── Maps/           MAP_*.asset    (MapData)
├── StaffBuffs/     SBUF_*.asset   (StaffBuffData)
├── StaffRaces/     RACE_*.asset   (StaffRaceData)
├── CalendarEvents/ CAL_*.asset    (CalendarEventData)
├── Furniture/      FRN_*.asset    (FurnitureData)
├── Skills/         SKL_*.asset    (SkillData)
├── Config/         CookingConfig / JustInputConfig
└── QualityScaleTable.asset
```

### 6.2 素材-レシピ-料理 リレーション

```
ING_Meat ──┐
ING_Scale ─┤── RCP_DragonSteak ──▶ DISH_DragonSteak
           │   (meat×3 + scale×1)    (Meat / HP120 / buff0.25)
           │   (ChefLevel 3)
           │
ING_Meat ──┤── RCP_Steak ──▶ DISH_Steak
           │   (meat×2)       (Meat / HP80 / buff0.15)
           │   (ChefLevel 1)
           │
ING_Vegetable ─┤── RCP_Salad ──▶ DISH_Salad
               │   (veg×2)       (Salad / HP40 / buff0.10)
               │   (ChefLevel 1)
               │
ING_Meat ──────┤── RCP_Stew ──▶ DISH_Stew
ING_Vegetable ─┘   (meat×1 + veg×1)  (Meat / HP60 / buff0.12)
                    (ChefLevel 1)

ING_Bone ──┤── RCP_PoisonStew ──▶ DISH_PoisonStew
ING_Poison ┘   (bone×2 + poison×1)   (Fish / HP70 / buff0.18)
               (ChefLevel 2)

ING_Vegetable ─┤── RCP_Pudding ──▶ DISH_Pudding
ING_Herb ──────┘   (veg×1 + herb×1)  (Dessert / HP30 / buff0.08)
                    (ChefLevel 1)
```

### 6.3 敵-ドロップ-種族 リレーション

| 敵 | 通常ドロップ | ジャストドロップ | 種族 | ゴールド |
|----|------------|--------------|------|---------|
| ENM_Cactus | ING_Vegetable | ING_Herb | RACE_Cactus | 100G |
| ENM_Boss | ING_Meat | ING_Bone | RACE_Boss | 100G |
| ENM_DragonLord | ING_Meat | ING_Scale | RACE_DragonLord | 100G |
| ENM_PoisonHydra | ING_Bone | ING_Poison | RACE_PoisonHydra | 100G |
| ENM_Dummy | ING_Vegetable | ING_Meat | RACE_Dummy | 100G |

### 6.4 マップデータ

| マップ | 環境 | 推奨Lv | シーン |
|--------|------|--------|--------|
| MAP_Forest | Forest | 1 | FieldScene |
| MAP_Desert | Desert | 3 | FieldScene |
| MAP_Swamp | Swamp | 5 | FieldScene |
| MAP_Castle | Castle | 7 | FieldScene |
| MAP_Volcano | Volcano | 10 | FieldScene |

### 6.5 セーブデータ構造 (JSON)

```json
{
  "Gold": 1500,
  "CurrentDay": 7,
  "Reputation": 65.5,
  "ChefLevel": 3,
  "CookingXP": 450,
  "EquippedWeaponID": "WPN_Standard",
  "Items": [
    { "ItemID": "ING_Meat", "Count": 5 },
    { "ItemID": "WPN_Heavy", "Count": 1 }
  ],
  "CookedDishes": [
    { "DishID": "DISH_Steak", "Quality": 2, "Count": 3 }
  ],
  "Staff": [
    {
      "Id": "guid-...",
      "SourceEnemyName": "ボス",
      "RaceId": "RACE_Boss",
      "BuffIds": ["SBUF_QualityUp", "SBUF_MeatSpecialty"],
      "SlotType": 0,
      "MoralePenalty": 0
    }
  ],
  "OwnedFurnitureIds": ["FRN_Table", "FRN_Decoration"],
  "HasSaveData": true
}
```

---

## 第7章 UI 設計

### 7.1 バトル UI レイヤー構成

```
[Sort Order]
  150  BattleEffectsUI     全画面演出（ワイプ、カットイン、フラッシュ）
  100  DamageNumberUI      フローティングダメージ数値
   50  EnemyStatusUI       敵頭上ビルボード（HP/タフネス/弱点）
   10  SkillCommandUI      Star Rail 風コマンドメニュー（uGUI）
   10  UltimatePortraitUI  EP ポートレート（uGUI）
    5  ActionTimelineUI    行動順タイムライン（UI Toolkit）
    5  CharacterStatusUI   味方 HP カードバー（uGUI）
    1  RevolverMenuController  Metaphor 風リボルバーUI（UI Toolkit）
    0  DynamicBattleUIController  Metaphor 風追従UI（UI Toolkit）
```

### 7.2 経営 UI パネル構成

**BaseSceneUI（朝の拠点）:**
| パネル | 内容 |
|--------|------|
| メインメニュー | 出撃/料理/装備/スカウト/セーブ ボタン |
| マップ選択 | MapData 一覧 → 出撃先選択 |
| 料理パネル | レシピ一覧 + 素材在庫 + 調理ボタン |
| 装備パネル | 所持武器一覧 + 装備切替 |
| スカウトパネル | スタッフ一覧 + 雇用/解雇/昇格 |
| セーブパネル | セーブ/ロード ボタン |

**ManagementSceneUI（夜の経営）:**
| パネル | 内容 |
|--------|------|
| 営業パネル | メニュー選択 → 営業開始 → 結果表示 |
| メニュー編成 | 在庫料理の営業メニュー設定 |
| 改装パネル | 家具購入 → 満足度/来客数ボーナス |
| スタッフ配置 | スタッフスロット管理 |

---

## 第8章 音声設計

### 8.1 SE キー一覧

| キー | トリガー |
|------|---------|
| AttackHit | 通常攻撃ヒット |
| JustHit | ジャストアタック成功 |
| Skill | スキル発動 |
| Ultimate | 必殺技発動 |
| Guard | ガード実行 |
| Damage | 被ダメージ |
| Victory | バトル勝利 |
| Defeat | バトル敗北 |
| MenuSelect | メニュー選択 |
| MenuConfirm | メニュー決定 |
| Cooking | 調理実行 |
| Save | セーブ完了 |

### 8.2 BGM 切替

| シーン | BGM |
|--------|-----|
| BaseScene | デフォルト BGM |
| ManagementScene | デフォルト BGM |
| FieldScene | （フィールド用 — 未設定） |
| BattleScene | （バトル用 — 未設定） |

---

## 第9章 カレンダーイベント

| イベント | 発動日 | 対象カテゴリ | 満足度倍率 | 鮮度倍率 |
|---------|--------|------------|----------|---------|
| 肉の日 | 3, 10, 17日 | Meat | 1.5x | 1.3x |
| 収穫祭 | 7, 14日 | Salad | 1.8x | 1.2x |
| グルメウィーク | 5, 12, 19日 | 全カテゴリ | 1.3x | 1.5x |

---

## 第10章 鮮度バフシステム

バトル成績が翌日の料理品質に影響する、バトルと経営の橋渡しメカニクス。

```
鮮度バフ = 1.0
  + (ジャスト成功率 × 0.5)    // ジャスト率が高い → 品質UP
  + (スカウト成功数 × 0.1)    // スカウトが多い → 品質UP

clamp(1.0, 2.0)

→ CookingManager の品質計算に乗算
→ 翌日の GameManager.DailyFreshnessBuff に格納
```

---

## 付録 A: クラス依存関係図

```
GameManager ◄─── BootLoader
    │
    ├── InventoryManager
    ├── SaveDataManager
    ├── StaffManager ◄─── StaffBuffRoller ◄─── BattleResultController
    ├── HousingManager
    │
    ├──▶ BaseSceneBootstrap ──▶ BaseSceneUI
    │                              └── CookingManager
    │
    ├──▶ FieldSceneBootstrap ──▶ FieldPlayerController
    │                          ├── FieldCameraController
    │                          ├── EnemySymbol
    │                          └── FieldEncounterHandler
    │
    ├──▶ BattleSceneBootstrap ──▶ BattleManager
    │                              ├── AttackAction
    │                              ├── EnemyAttackAction
    │                              ├── MealAction ──▶ MealBuffApplier
    │                              ├── ScoutAction
    │                              ├── ActionQueueSystem
    │                              ├── EnemyAIController
    │                              ├── BuffDurationTracker
    │                              ├── BattleCameraManager
    │                              ├── BattleResultController
    │                              └── BattleUIManager
    │                                    ├── SkillCommandUI
    │                                    ├── UltimatePortraitUI
    │                                    ├── ActionTimelineUI
    │                                    ├── CharacterStatusUI
    │                                    ├── EnemyStatusUI
    │                                    ├── DamageNumberUI
    │                                    ├── BattleEffectsUI
    │                                    └── BattleResultUI
    │
    └──▶ ManagementSceneBootstrap ──▶ ManagementSceneUI
                                      ├── CookingManager
                                      └── DinerService
```

---

## 付録 B: ScriptableObject 継承ツリー

```
ScriptableObject
├── ItemData
│   ├── IngredientData    素材アイテム
│   ├── DishData          料理アイテム
│   └── WeaponData        武器アイテム
├── CharacterStats        キャラクターステータス
├── EnemyData             敵データ
├── RecipeData            レシピ定義
├── MapData               マップ定義
├── FurnitureData         家具定義
├── StaffRaceData         スタッフ種族
├── StaffBuffData         スタッフバフ
├── CalendarEventData     カレンダーイベント
└── QualityScaleTable     品質倍率テーブル
```
