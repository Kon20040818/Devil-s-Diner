# Devil's Diner — API リファレンス & DB スキーマ一覧

**出力日**: 2026-03-08

---

## 目次

1. [Battle モジュール API](#1-battle-モジュール-api)
2. [Core モジュール API](#2-core-モジュール-api)
3. [Management モジュール API](#3-management-モジュール-api)
4. [Field モジュール API](#4-field-モジュール-api)
5. [UI モジュール API](#5-ui-モジュール-api)
6. [DB スキーマ（ScriptableObject フィールド定義）](#6-db-スキーマ)
7. [Enum 定義一覧](#7-enum-定義一覧)
8. [クイックリファレンス（主要操作→API対応表）](#8-クイックリファレンス)

---

## 1. Battle モジュール API

### BattleManager

```csharp
// ── Properties ──
static BattleManager Instance { get; }
BattlePhase CurrentPhase { get; }
CharacterBattleController ActiveCharacter { get; }
ActionQueueSystem Queue { get; }
IReadOnlyList<CharacterBattleController> PlayerParty { get; }
IReadOnlyList<CharacterBattleController> EnemyParty { get; }
int CurrentSP { get; }
int MaxSP { get; }
bool IsAutoBattle { get; }
bool IsDoubleSpeed { get; }
float JustRate { get; }
int TotalHitCount { get; }
int JustHitCount { get; }
IReadOnlyList<ScoutedEnemyRecord> ScoutedEnemies { get; }
BuffDurationTracker BuffDurationTracker { get; }

// ── Events ──
event Action<BattlePhase> OnPhaseChanged;
event Action<CharacterBattleController> OnActiveCharacterChanged;
event Action<bool> OnBattleEnd;                           // true = 勝利
event Action<int, int> OnSPChanged;                       // current, max
event Action<DamageResult> OnDamageDealt;
event Action<CharacterBattleController> OnUltimateActivated;
event Action<CharacterBattleController, string> OnSkillExecuted;
event Action<bool> OnAutoBattleChanged;
event Action<bool> OnSpeedChanged;

// ── Methods ──
void SetCameraManager(BattleCameraManager manager)
void SetAttackAction(AttackAction action)
void SetEnemyAttackAction(EnemyAttackAction action)
void SetMealAction(MealAction action)
void SetScoutAction(ScoutAction action)
void SetBuffDurationTracker(BuffDurationTracker tracker)
void SetEnemyAIController(EnemyAIController controller)
void SetSelectedDish(DishInstance dish)
void ClearSelectedDish()
void StartBattle(CharacterBattleController[] players, CharacterBattleController[] enemies)
void ExecutePlayerAction(ActionType action, CharacterBattleController target)
bool CanUseSkill()
void ExecuteUltimate(CharacterBattleController character, CharacterBattleController target)
void ToggleAutoBattle()
void ToggleSpeed()
```

### ActionQueueSystem

```csharp
event Action OnQueueUpdated;

void Register(CharacterBattleController character)
void Unregister(CharacterBattleController character)
CharacterBattleController AdvanceAndGetNext()
List<CharacterBattleController> GetOrderPreview(int count = 10)
void Clear()
```

### AttackAction

```csharp
Action OnActionEnd;
bool IsRunning { get; }
float JustMultiplier { get; }
int HitCount { get; }

void ExecuteAttack()
IEnumerator ExecuteAttackCoroutine(Action<int, bool> onHit)  // (damage, isJust)
```

### EnemyAttackAction

```csharp
enum GuardResult { JustGuard, NormalGuard, Failed }

Action OnActionEnd;
bool IsRunning { get; }
int HitCount { get; }
float JustGuardMultiplier { get; }
float NormalGuardMultiplier { get; }
bool IsAcceptingGuard { get; }

void ExecuteEnemyAttack()
IEnumerator ExecuteAttackCoroutine(Action<int, GuardResult> onHit)
```

### CharacterBattleController

```csharp
// ── Properties ──
CharacterStats Stats { get; }
Faction CharacterFaction { get; }
BattleState CurrentState { get; }
int CurrentHP { get; }
int MaxHP { get; }
bool IsAlive { get; }
int CurrentEP { get; }
int MaxEP { get; }
bool IsUltimateReady { get; }
string DisplayName { get; }
int CurrentToughness { get; }
int MaxToughness { get; }
bool IsBroken { get; }
bool HasToughness { get; }
bool IsScouted { get; }
bool IsGuarding { get; }
EnemyData EnemyData { get; }

// ── Events ──
event Action<int, int> OnHPChanged;
event Action<CharacterBattleController> OnDeath;
event Action<int, int> OnEPChanged;
event Action<BattleState> OnStateChanged;
event Action<int, int> OnToughnessChanged;
event Action<CharacterBattleController> OnToughnessBreak;
event Action<DamageResult> OnDamageReceived;

// ── Methods ──
void Initialize(CharacterStats stats, Faction faction)
void SetGuarding(bool value)
void SetEnemyData(EnemyData data)
int TakeDamage(int rawDamage, ElementType element = Physical, CharacterBattleController attacker = null)
void Heal(int amount)
void ScoutRemove()
void AddEP(int amount)
void ConsumeAllEP()
void SetState(BattleState newState)
int CalculateBasicAttackDamage()
int CalculateSkillDamage()
int CalculateUltimateDamage()
```

### BattleCameraManager

```csharp
// ── 静的シェイクプリセット (intensity, frequency, duration) ──
static readonly Vector3 SHAKE_BASIC_HIT       = (0.15, 25, 0.12)
static readonly Vector3 SHAKE_SKILL_HIT       = (0.35, 20, 0.20)
static readonly Vector3 SHAKE_ULTIMATE_IMPACT = (0.70, 15, 0.25)
static readonly Vector3 SHAKE_ENEMY_HIT       = (0.08, 25, 0.10)
static readonly Vector3 SHAKE_BREAK           = (0.50, 14, 0.25)

CameraMode CurrentMode { get; }

void SetFieldCenter(Vector3 center)
void SwitchToOverview()
void FocusOnCharacter(Transform target)
void SwitchToActionCamera(Transform attacker, Transform target)
void SwitchToSkillCamera(Transform attacker, Transform target)
void SwitchToEnemyCamera(Transform attacker, Transform target)
void SwitchToUltimateCamera(Transform attacker, Transform target)
void SwitchToUltimateActionCamera(Transform attacker, Transform target)
void SwitchToVictoryCamera()
void SwitchToDefeatCamera()
void ShakeCamera(float intensity, float frequency, float duration)
void ShakeCamera(Vector3 profile)
void SlowMotion(float timeScale, float duration)
```

### その他バトルコンポーネント

```csharp
// MealAction
void SetBuffApplier(MealBuffApplier applier)
IEnumerator ExecuteActionCoroutine(CharacterBattleController healer, Action<int> onHeal)
IEnumerator ExecuteActionCoroutine(CharacterBattleController healer, DishInstance dish, Action<int> onHeal)

// MealBuffApplier
void ApplyBuff(DishInstance dish)

// BuffDurationTracker
event Action<DishCategory, int> OnBuffApplied;
event Action<DishCategory> OnBuffExpired;
void RegisterBuff(DishInstance dish)
void ProcessTurnEnd(CharacterBattleController character)
IReadOnlyDictionary<DishCategory, ActiveBuff> GetActiveBuffs()
void ClearAll()

// ScoutAction
IEnumerator ExecuteActionCoroutine(CharacterBattleController target, Action<bool> onComplete)

// EnemyAIController
AIDecision Decide(CharacterBattleController actor, CharacterBattleController[] opponents)

// BattleResultController
void Initialize(BattleManager battleManager)
void SetReturnSceneName(string sceneName)
```

---

## 2. Core モジュール API

### GameManager

```csharp
// ── Properties ──
static GameManager Instance { get; }
int CurrentDay { get; }
GamePhase CurrentPhase { get; }
int Gold { get; }
string CurrentSceneName { get; }
BattleTransitionData PendingBattleData { get; set; }
InventoryManager Inventory { get; }
SaveDataManager SaveData { get; }
StaffManager Staff { get; }
HousingManager Housing { get; }
int CookingXP { get; }
int ChefLevel { get; }
int Reputation { get; }
float DailyFreshnessBuff { get; set; }
string EquippedWeaponID { get; }

// ── Events ──
event Action<GamePhase> OnPhaseChanged;
event Action<int> OnDayAdvanced;
event Action<int> OnGoldChanged;
event Action<string> OnSceneLoaded;
event Action<int> OnChefLevelUp;
event Action<int> OnReputationChanged;

// ── Methods ──
void AddGold(int amount)
bool CanAfford(int cost)
bool TrySpendGold(int cost)
void AdvancePhase()
void TransitionToScene(string sceneName)
void AddCookingXP(int xp)
void AddReputation(int amount)
void EquipWeapon(string weaponID)
WeaponData GetEquippedWeapon()
void ResetProgress()
void ClearBattleTransitionData()
static void ForceRestoreTimeScale()
```

### InventoryManager

```csharp
event Action OnInventoryChanged;

// アイテム操作
void Add(ItemData item, int count = 1)
bool Remove(ItemData item, int count = 1)
int GetCount(ItemData item)
bool Has(ItemData item, int count = 1)
IReadOnlyDictionary<ItemData, int> GetAllItems()
List<KeyValuePair<T, int>> GetItemsOfType<T>() where T : ItemData

// 料理操作
void AddDish(DishInstance dish, int count = 1)
bool RemoveDish(DishInstance dish, int count = 1)
int GetDishCount(DishInstance dish)
bool HasDish(DishInstance dish, int count = 1)
IReadOnlyDictionary<DishInstance, int> GetAllDishes()
int GetTotalDishCount()

void ClearAll()
```

### SaveDataManager

```csharp
void Save()
void Load()
bool HasSaveData()
void DeleteSaveData()
```

### AudioManager

```csharp
static AudioManager Instance { get; }

void PlaySE(string key)
void PlaySE(AudioClip clip, float volume = 1f)
void PlayBGM(AudioClip clip)
void PlayDefaultBGM()
void StopBGM()
void SetBGMVolume(float volume)
void SetSEVolume(float volume)
```

### AudioEventConnector

```csharp
// SE キー定数
const string SE_ATTACK_HIT, SE_JUST_HIT, SE_SKILL, SE_ULTIMATE,
             SE_GUARD, SE_DAMAGE, SE_DEFEAT, SE_VICTORY,
             SE_MENU_SELECT, SE_MENU_CONFIRM, SE_COOKING, SE_SAVE

static void WireBattle(BattleManager battleManager)
static void WireSceneBGM()
```

### SkillEffectApplier

```csharp
static float AttackMultiplier { get; }     // default 1.0
static float DefenseMultiplier { get; }    // default 1.0
static float SpeedMultiplier { get; }      // default 1.0
static int RegenPerTurn { get; }           // default 0
static float DropRateBonus { get; }        // default 0.0
static float ScoutChanceBonus { get; }     // default 0.0

static void ResetAll()
```

### DropResolver

```csharp
static DropResult ResolveDropWithResult(EnemyData enemy, bool isJustKill)
static void ResolveDrop(EnemyData enemy, bool isJustKill)
```

---

## 3. Management モジュール API

### CookingManager

```csharp
event Action<CookResult> OnDishCooked;
int ChefLevel { get; set; }

List<RecipeData> GetAvailableRecipes()
bool CanCook(RecipeData recipe)
CookResult Cook(RecipeData recipe, float playerSkillScore, CalendarEventData calendarEvent)
```

### DinerService

```csharp
event Action<DinerResult> OnServiceEnd;

DinerResult RunService(DishInstance[] availableDishes, CalendarEventData calendarEvent)
```

### StaffManager

```csharp
event Action OnStaffChanged;
IReadOnlyList<StaffInstance> PermanentStaff { get; }
IReadOnlyList<StaffInstance> TemporaryStaff { get; }
int PermanentSlotsAvailable { get; }
int TemporarySlotsAvailable { get; }

bool TryHire(StaffInstance staff, StaffSlotType slotType)
void Fire(StaffInstance staff)
bool TryPromote(StaffInstance staff)
void ProcessMorningPayroll()
void ClearTemporaryStaff()
StaffBuffSummary GetActiveBonuses()
void ReceiveRecruits(List<RecruitedDemonData> recruits)
List<StaffInstance> GetAllStaff()
int GetTotalDailySalary()
void ClearAll()
```

### HousingManager

```csharp
event Action OnFurnitureChanged;
IReadOnlyList<FurnitureData> OwnedFurniture { get; }

bool TryBuyFurniture(FurnitureData furniture)
bool Owns(FurnitureData furniture)
float GetTotalSatisfactionBonus()
int GetTotalCustomerBonus()
List<string> GetOwnedIDs()
void RestoreOwned(List<string> ids)
void ClearAll()
```

### StaffBuffRoller

```csharp
static List<RecruitedDemonData> RollAll(List<ScoutedEnemyRecord> scouted)
```

---

## 4. Field モジュール API

### FieldPlayerController

```csharp
void SetCameraTransform(Transform cam)
void SetInputActions(InputActionAsset asset)
```

### FieldCameraController

```csharp
void SetTarget(Transform player)
void SetInputActions(InputActionAsset asset)
```

### EnemySymbol

```csharp
event Action<EnemySymbol> OnEncounter;
EnemyData EnemyData { get; }
CharacterStats EnemyStats { get; }
SymbolState CurrentState { get; }

void SetPlayer(Transform player)
```

### FieldEncounterHandler

```csharp
void RegisterSymbol(EnemySymbol symbol)
```

---

## 5. UI モジュール API

### BattleUIManager

```csharp
void SetSkillCommandUI(SkillCommandUI ui)
void SetUltimatePortraitUI(UltimatePortraitUI ui)
void Initialize(BattleManager battleManager)
```

### SkillCommandUI

```csharp
event Action<ActionType> OnCommandSelected;
event Action<ActionType, CharacterBattleController> OnTargetConfirmed;

void Initialize(BattleManager battleManager)
void Show(CharacterBattleController character, bool canUseSkill)
void Hide()
void UpdateSP(int current, int max)
void EnterTargetSelection(ActionType action, IReadOnlyList<CharacterBattleController> enemies)
```

### UltimatePortraitUI

```csharp
event Action<CharacterBattleController> OnUltimateRequested;

void Initialize(BattleManager bm, IReadOnlyList<CharacterBattleController> playerParty)
void Hide()
```

### BattleResultUI

```csharp
event Action OnReturnToBase;
event Action OnRetry;

void ShowVictory(int gold, List<DropResult> drops)
void ShowDefeat()
void Hide()
```

### BattleEffectsUI

```csharp
void Initialize()
IEnumerator PlayBattleStartEffect()
IEnumerator PlayVictoryEffect()
IEnumerator PlayDefeatEffect()
void PlayUltimateFlash()
void PlayBreakFlash()
void PlayTurnStartFlash()
void PlayUltimateCutIn(string characterName)
void PlaySkillNameDisplay(string skillName)
```

### TitleScreenUI

```csharp
event Action OnStartGame;
event Action OnContinueGame;

void Show(bool hasSaveData)
```

### BaseSceneUI / ManagementSceneUI

```csharp
// BaseSceneUI
void Initialize(CookingManager cookingMgr)

// ManagementSceneUI
void Initialize(CookingManager cookingMgr, DinerService dinerService)
```

---

## 6. DB スキーマ

### 6.1 CharacterStats

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _id | string | 一意識別子 |
| _displayName | string | 表示名 |
| _portrait | Sprite | ポートレート画像 |
| _element | ElementType | 属性 |
| _maxHP | int | 最大HP |
| _attack | int | 攻撃力 |
| _defense | int | 防御力 |
| _speed | int | 速度（行動値計算に使用） |
| _physicalRes ~ _darkRes | float | 各属性耐性 (0.0~1.0, 負値=弱点) |
| _maxEP | int | 最大EP |
| _epGainOnAttack | int | 攻撃時EP獲得量 |
| _epGainOnHit | int | 被弾時EP獲得量 |
| _epGainOnSkill | int | スキル使用時EP獲得量 |
| _skillMultiplier | float | スキル倍率 |
| _ultimateMultiplier | float | 必殺技倍率 |
| _skillTargetMode | TargetingMode | Single / AllEnemies |
| _maxToughness | int | 最大タフネス |
| _weakElements | ElementType[] | 弱点属性配列 |
| _baseActionValue | float | 基本行動値 |

### 6.2 ItemData（基底）

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _itemID | string | 一意識別子 |
| _displayName | string | 表示名 |
| _description | string | 説明文 |
| _icon | Sprite | アイコン |
| _sellPrice | int | 売却価格 |

### 6.3 IngredientData（ItemData 継承）

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _rarity | int (1-5) | レア度 |
| _dropRate | float (0-1) | 基本ドロップ率 |
| _gaugeSpeedMultiplier | float | 調理ゲージ速度倍率 |

### 6.4 DishData（ItemData 継承）

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _category | DishCategory | 料理カテゴリ |
| _hpRecoveryAmount | int | 基本HP回復量 |
| _baseBuff | float | 基本バフ値 |
| _buffDurationTurns | int | バフ持続ターン |
| _scoutBonus | float | スカウト確率ボーナス |
| _shopPrice | int | 店舗販売価格 |
| _baseSatisfaction | int | 基本満足度 |
| _servingTime | float | 提供時間 |
| _qualityTable | QualityScaleTable | 品質倍率テーブル参照 |

### 6.5 WeaponData（ItemData 継承）

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _baseDamage | int | 基本ダメージ |
| _basePartBreakValue | int | 部位破壊値 |
| _justInputFrameBonus | int | ジャスト入力ボーナス |
| _animatorOverride | AnimatorOverrideController | アニメーション上書き |

### 6.6 RecipeData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _recipeID | string | 一意識別子 |
| _displayName | string | 表示名 |
| _description | string | 説明文 |
| _outputDish | DishData | 完成品参照 |
| _ingredients | IngredientSlot[] | 必要素材配列 |
| _requiredChefLevel | int | 必要シェフレベル |

**IngredientSlot 構造体:**

| フィールド | 型 | 説明 |
|-----------|-----|------|
| Ingredient | IngredientData | 素材参照 |
| Amount | int | 必要数量 |

### 6.7 EnemyData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _id | string | 一意識別子 |
| _enemyName | string | 敵名 |
| _maxHP | int | 最大HP |
| _baseAttack | int | 基本攻撃力 |
| _dropItemNormal | ItemData | 通常ドロップアイテム |
| _dropItemJust | ItemData | ジャストドロップアイテム |
| _dropRateNormal | float (0-1) | 通常ドロップ率 |
| _dropRateJust | float (0-1) | ジャストドロップ率 |
| _goldReward | int | ゴールド報酬 |
| _staffRace | StaffRaceData | スタッフ種族参照 |

### 6.8 MapData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _id | string | 一意識別子 |
| _mapName | string | マップ名 |
| _description | string | 説明文 |
| _environment | EnvironmentType | 環境タイプ |
| _requiredShopLevel | int | 必要ショップレベル |
| _recommendedLevel | int | 推奨レベル |
| _sceneName | string | 遷移先シーン名 |

### 6.9 StaffRaceData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _raceID | string | 一意識別子 |
| _raceName | string | 種族名 |
| _portrait | Sprite | ポートレート |
| _fixedEffect | StaffFixedEffect | 固定効果タイプ |
| _fixedEffectValue | float | 固定効果値 |
| _baseSalary | int | 基本給料 |
| _possibleBuffs | StaffBuffData[] | バフ候補プール |
| _minBuffCount | int | 最小バフ数 |
| _maxBuffCount | int | 最大バフ数 |

### 6.10 StaffBuffData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _buffID | string | 一意識別子 |
| _displayName | string | 表示名 |
| _description | string | 説明文 |
| _icon | Sprite | アイコン |
| _type | StaffBuffType | バフ種別 |
| _value | float | バフ値 |
| _targetCategory | DishCategory | 対象カテゴリ（CategorySpecialty時） |
| _rarity | int | レア度（選出重み = 6 - rarity） |

### 6.11 CalendarEventData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _eventID | string | 一意識別子 |
| _eventName | string | イベント名 |
| _description | string | 説明文 |
| _icon | Sprite | アイコン |
| _triggerDays | int[] | 発動日配列 |
| _bonusCategoryEnabled | bool | カテゴリ限定ボーナスの有無 |
| _bonusCategory | DishCategory | 対象カテゴリ |
| _satisfactionMultiplier | float | 満足度倍率 |
| _freshnessMultiplier | float | 鮮度倍率 |

### 6.12 FurnitureData

| フィールド | 型 | 説明 |
|-----------|-----|------|
| _id | string | 一意識別子 |
| _furnitureName | string | 家具名 |
| _description | string | 説明文 |
| _type | FurnitureType | 種別 |
| _price | int | 購入価格 |
| _satisfactionBonus | float | 満足度ボーナス |
| _customerBonus | int | 来客数ボーナス |
| _prefab | GameObject | 配置プレハブ |

### 6.13 QualityScaleTable

| 品質 | Heal | Buff | Scout | Price | Satisfaction |
|------|------|------|-------|-------|-------------|
| Poor | 0.6x | 0.5x | 0.5x | 0.7x | 0.6x |
| Normal | 1.0x | 1.0x | 1.0x | 1.0x | 1.0x |
| Fine | 1.3x | 1.4x | 1.3x | 1.5x | 1.4x |
| Exquisite | 1.6x | 1.8x | 1.5x | 2.0x | 1.8x |

---

## 7. Enum 定義一覧

```csharp
// BattleManager
enum BattlePhase { None, BattleStart, AdvancingQueue, PlayerCommand,
                   EnemyAction, Executing, TurnEnd, Victory, Defeat }

// CharacterBattleController
enum Faction { Player, Enemy }
enum BattleState { WaitingTurn, SelectingAction, Executing, Down }
enum ActionType { BasicAttack, Skill, Ultimate, Meal, Scout, Guard }

// CharacterStats
enum TargetingMode { Single, AllEnemies }
enum ElementType { Physical, Fire, Ice, Lightning, Wind, Dark }

// BattleCameraManager
enum CameraMode { Overview, TurnStart, BasicAttack, SkillExecution,
                  UltimateCinematic, EnemyAction, Victory, Defeat }

// EnemyAttackAction
enum GuardResult { JustGuard, NormalGuard, Failed }

// GameManager
enum GamePhase { Morning, Noon, Evening }

// EnemySymbol
enum SymbolState { Patrol, Chase, Returning }

// Data
enum DishCategory { Meat, Fish, Salad, Dessert }
enum DishQuality { Poor, Normal, Fine, Exquisite }
enum StaffFixedEffect { CookSpeedUp, SatisfactionUp, SalaryDiscount, QualityUp, DropRateUp }
enum StaffBuffType { CookSpeed, QualityBonus, SatisfactionBonus,
                     SalaryReduction, CategorySpecialty, FreshnessBonus }
enum StaffSlotType { Permanent, Temporary }
enum FurnitureType { Table, Chair, Decoration, Lighting, Kitchen }
enum EnvironmentType { Desert, Forest, Swamp, Volcano, Castle }
```

---

## 8. クイックリファレンス

| やりたいこと | API |
|-------------|-----|
| バトル開始 | `GameManager.PendingBattleData = data` → `TransitionToScene("BattleScene")` |
| コマンド実行 | `BattleManager.ExecutePlayerAction(ActionType, target)` |
| 必殺技発動 | `BattleManager.ExecuteUltimate(character, target)` |
| ダメージ適用 | `CharacterBattleController.TakeDamage(rawDmg, element, attacker)` |
| HP変化監視 | `CharacterBattleController.OnHPChanged += (cur, max) => {}` |
| 食事バフ適用 | `MealBuffApplier.ApplyBuff(dishInstance)` |
| 料理する | `CookingManager.Cook(recipe, skillScore, calendarEvent)` |
| 営業する | `DinerService.RunService(dishes, calendarEvent)` |
| スタッフボーナス取得 | `StaffManager.GetActiveBonuses()` → `StaffBuffSummary` |
| ゴールド加算 | `GameManager.AddGold(amount)` |
| セーブ | `GameManager.SaveData.Save()` |
| SE再生 | `AudioManager.Instance.PlaySE("AttackHit")` |
| フェーズ進行 | `GameManager.AdvancePhase()` |
