# Devil's Diner — 実装技術詳細マニュアル

**出力日**: 2026-03-09

---

## 目次

1. [バトルシステム](#1-バトルシステム)
   - 1.1 BattleManager ステートマシン
   - 1.2 ActionQueueSystem（速度ベースターン制）
   - 1.3 AttackAction（ジャストアタック QTE）
   - 1.4 EnemyAttackAction（ジャストガード QTE）
   - 1.5 ダメージ計算式
   - 1.6 ScoutAction（スカウト確率曲線）
   - 1.7 EnemyAIController（重み付きランダム）
   - 1.8 必殺技割り込みシステム
   - 1.9 食事バフパイプライン
2. [カメラシステム](#2-カメラシステム)
3. [経営システム](#3-経営システム)
   - 3.1 CookingManager（調理アルゴリズム）
   - 3.2 DinerService（営業シミュレーション）
   - 3.3 StaffManager（スタッフバフ集計）
   - 3.4 CalendarEventData（カレンダーイベント）
4. [品質スケールシステム](#4-品質スケールシステム)
5. [ドロップ計算](#5-ドロップ計算)
6. [バフ管理](#6-バフ管理)
7. [UI 実装技術](#7-ui-実装技術)
   - 7.1 UI Toolkit vs uGUI Canvas 使い分け
   - 7.2 プロシージャルスプライト生成
   - 7.3 ActionTimelineUI（タイムライン表示）
   - 7.4 CharacterStatusUI（HPカード）
   - 7.5 DamageNumberUI（ダメージポップアップ）
   - 7.6 BattleEffectsUI（演出エフェクト）
   - 7.7 UltimatePortraitUI（EPリングポートレート）
   - 7.8 SkillCommandUI（コマンド選択）
   - 7.9 RevolverMenuController（リボルバーメニュー）
8. [コアシステム](#8-コアシステム)
   - 8.1 GameManager（シングルトン・永続状態）
   - 8.2 InventoryManager（インベントリ管理）
   - 8.3 SaveDataManager（セーブ/ロード）
   - 8.4 AudioManager / AudioEventConnector
9. [データアーキテクチャ](#9-データアーキテクチャ)
   - 9.1 ScriptableObject 継承ツリー
   - 9.2 CharacterStats（キャラクターデータ）
   - 9.3 EnemyData（敵固有データ）
10. [エディタツール](#10-エディタツール)
    - 10.1 SampleDataGenerator
    - 10.2 DevilsDinerSetupTool
    - 10.3 Bootstrap パターン
11. [設計パターン総覧](#11-設計パターン総覧)

---

## 1. バトルシステム

### 1.1 BattleManager ステートマシン

**ファイル**: `Assets/Scripts/Battle/BattleManager.cs`

BattleManager はバトル全体の進行を管理するコルーチン駆動のステートマシンである。
`Update()` にはターン進行ロジックを一切含まず、全てを `IEnumerator` で制御する。

#### BattlePhase 状態遷移図

```
None → BattleStart → AdvancingQueue → PlayerCommand → Executing → TurnEnd ─┐
                          ↑               ↓                                 │
                          │          EnemyAction → Executing → TurnEnd ─────┤
                          │                                                 │
                          └─────────────────────────────────────────────────┘
                                         ↓ (全滅判定)
                                    Victory / Defeat
```

#### 状態遷移の仕組み

```csharp
private void SetPhase(BattlePhase newPhase)
{
    CurrentPhase = newPhase;
    OnPhaseChanged?.Invoke(newPhase);  // UI 側に通知
}
```

全ての状態遷移は `SetPhase()` を経由し、`OnPhaseChanged` イベントで UI に通知される。
これにより BattleManager は UI の存在を一切知らずに動作する。

#### メインループ（コルーチンチェーン）

```
StartBattle()
  └→ BattleStartSequence()    // Phase = BattleStart, 演出待ち
       └→ NextTurn()           // Phase = AdvancingQueue
            ├→ [プレイヤー] Phase = PlayerCommand → ExecuteAction()
            └→ [敵]         Phase = EnemyAction  → EnemyTurn() → ExecuteAction()
                                    └→ TurnEnd()  // Phase = TurnEnd → CheckBattleEnd()
                                         └→ NextTurn() [ループ]
```

#### SP（スキルポイント）経済

| アクション | SP 変動 |
|-----------|---------|
| 通常攻撃   | +1      |
| ガード     | +1      |
| スキル     | -1      |
| 必殺技     | 変動なし（EP を消費）|

初期値 3、最大値 5。`OnSPChanged(current, max)` イベントで UI に反映。

#### EP（必殺技ポイント）経済

| アクション | EP 獲得量 |
|-----------|-----------|
| 通常攻撃   | `CharacterStats.EPGainOnAttack` (デフォルト 20) |
| スキル     | `CharacterStats.EPGainOnSkill` (デフォルト 30) |
| 被ダメージ | `CharacterStats.EPGainOnHit` (デフォルト 10) |

EP が `MaxEP`（デフォルト 120）に達すると `IsUltimateReady = true`。

#### 依存注入パターン

BattleManager は全サブシステムを `Set*()` メソッドで受け取る:

```csharp
public void SetCameraManager(BattleCameraManager cam)
public void SetAttackAction(AttackAction attackAction)
public void SetEnemyAttackAction(EnemyAttackAction enemyAttackAction)
public void SetMealAction(MealAction mealAction)
public void SetScoutAction(ScoutAction scoutAction)
```

これらは全て `BattleSceneBootstrap.Start()` から呼ばれる。

---

### 1.2 ActionQueueSystem（速度ベースターン制）

**ファイル**: `Assets/Scripts/Battle/ActionQueueSystem.cs`

Star Rail 方式の **ActionValue (AV)** システム。純粋な C# クラス（MonoBehaviour ではない）。

#### AV 算出式

```
AV(character) = BaseActionValue / Speed
```

- `BaseActionValue` = 10000（全キャラ共通デフォルト）
- `Speed` = `CharacterStats._speed`（キャラ固有）

**AV が小さい = 行動が早い**。

#### AdvanceAndGetNext() アルゴリズム

```
1. 生存キャラの中から最小 AV を持つキャラを選択
2. 全キャラの AV から最小値を減算（時間経過のシミュレーション）
3. 行動キャラの AV を CalculateActionValue() でリセット（次のターンの初期値）
4. 行動キャラを返却
```

#### タイムラインプレビュー

```csharp
public List<CharacterBattleController> GetOrderPreview(int count)
```

内部で辞書のコピーを作り、`AdvanceAndGetNext` と同じロジックを `count` 回シミュレーション。
副作用なしで未来の行動順を返す。`ActionTimelineUI` がこれを使って表示を更新する。

---

### 1.3 AttackAction（ジャストアタック QTE）

**ファイル**: `Assets/Scripts/Battle/AttackAction.cs`

ガンブレードスタイルのタイミング入力システム。

#### Inspector パラメータ

| フィールド | デフォルト値 | 説明 |
|-----------|-------------|------|
| `_hitTimings` | `{0.5, 1.2, 1.8}` | 攻撃開始からの各ヒットタイミング（秒） |
| `_inputWindow` | `0.2` | ヒット前の入力受付開始オフセット（秒） |
| `_justMultiplier` | `1.5` | ジャスト成功時のダメージ倍率 |
| `_hitStopDuration` | `0.05` | ヒットストップ時間（実時間秒） |

#### QTE フロー（各ヒット `i` ごと）

```
時間経過 → hitTimings[i] - inputWindow に到達
  → _isAcceptingInput = true（入力受付開始）
  → プレイヤーがボタン押下 → _justTriggered = true
  → hitTimings[i] に到達（入力窓終了）
  → onHit(i, _justTriggered) コールバック発火
  → ジャスト成功なら HitStop() 実行
```

#### ヒットストップ実装

```csharp
private IEnumerator HitStop()
{
    float savedTimeScale = Time.timeScale;
    Time.timeScale = 0.1f;                    // ほぼ停止
    float elapsed = 0f;
    while (elapsed < _hitStopDuration)
    {
        elapsed += Time.unscaledDeltaTime;    // 実時間で計測
        yield return null;
    }
    Time.timeScale = savedTimeScale;          // 復帰
}
```

**重要**: `Time.unscaledDeltaTime` を使用することで、`timeScale` の変更に影響されない。

#### 入力ポーリング

`Update()` で毎フレーム入力を監視:

```csharp
void Update()
{
    if (!_isRunning || !_isAcceptingInput) return;
    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z) ||
        Input.GetKeyDown(KeyCode.Return) || /* Gamepad South */)
    {
        _justTriggered = true;
    }
}
```

コルーチンと `Update` の分離により、タイミング判定とフレーム精度の入力検知を両立。

---

### 1.4 EnemyAttackAction（ジャストガード QTE）

**ファイル**: `Assets/Scripts/Battle/EnemyAttackAction.cs`

敵の攻撃に対するプレイヤーの防御 QTE。3段階の結果判定を持つ。

#### Inspector パラメータ

| フィールド | デフォルト値 | 説明 |
|-----------|-------------|------|
| `_hitTimings` | (攻撃タイミング配列) | 敵攻撃のヒットタイミング |
| `_justGuardWindow` | `0.15` | ジャストガード受付幅（秒） |
| `_normalGuardWindow` | `0.35` | 通常ガード受付幅（秒） |
| `_justGuardMultiplier` | `0.0` | ジャストガード時のダメージ倍率（0%=完全無効） |
| `_normalGuardMultiplier` | `0.5` | 通常ガード時のダメージ倍率（50%軽減） |

#### 3段階判定アルゴリズム

```
EvaluateGuard(hitTime):

  timeToHitAtPress = hitTime - _guardPressTime

  ① 早押しペナルティ:
     ボタンが normalGuardWindow より前に押された
     → _hasAttemptedGuard = true → 強制 Failed

  ② JustGuard:
     0 ≤ timeToHitAtPress ≤ _justGuardWindow
     → ダメージ × 0.0（完全無効）+ HitStop 発生

  ③ NormalGuard:
     _justGuardWindow < timeToHitAtPress ≤ _normalGuardWindow
     → ダメージ × 0.5（半減）

  ④ Failed:
     上記以外 → ダメージ × 1.0（フルダメージ）
```

#### UI フィードバック連携

```csharp
public bool IsAcceptingGuard { get; private set; }
```

コルーチン内で毎フレーム更新され、UI 側がこのプロパティを監視してガードタイミング表示を切り替える。

---

### 1.5 ダメージ計算式

**ファイル**: `Assets/Scripts/Battle/CharacterBattleController.cs` 内に inline 実装

#### 基本ダメージ算出

```
通常攻撃: damage = Attack × AttackMultiplier + WeaponBonus
スキル:   damage = Attack × SkillMultiplier × AttackMultiplier + WeaponBonus
必殺技:   damage = Attack × UltimateMultiplier × AttackMultiplier + WeaponBonus
```

- `AttackMultiplier` = `SkillEffectApplier.AttackMultiplier`（食事バフで変動、デフォルト 1.0）
- `SkillMultiplier` = `CharacterStats._skillMultiplier`（デフォルト 1.5）
- `UltimateMultiplier` = `CharacterStats._ultimateMultiplier`（デフォルト 3.0）
- `WeaponBonus` = `WeaponData.BaseDamage`（装備武器の基礎ダメージ）

#### ダメージ軽減（TakeDamage 内）

```
reducedDamage = Max(rawDamage - defense, 1)

finalDamage = Max(Round(
    reducedDamage
    × (1 - resistance)       // 属性耐性（-0.5=弱点で1.5倍, 0.5=耐性で0.5倍）
    × breakBonus              // 1.25 if IsBroken, else 1.0
    × guardReduction          // 0.5 if IsGuarding, else 1.0
), 0)
```

#### タフネスシステム

- 弱点属性ヒット時: タフネス -30（ハードコード）
- タフネス 0 到達: `IsBroken = true` → `OnToughnessBreak` イベント発火
- ブレイク中は全ダメージ +25%

---

### 1.6 ScoutAction（スカウト確率曲線）

**ファイル**: `Assets/Scripts/Battle/ScoutAction.cs`

敵をスタッフとして勧誘するアクション。**区分線形確率曲線**を使用。

#### Inspector パラメータ

| フィールド | デフォルト値 | 説明 |
|-----------|-------------|------|
| `_minChance` | `0.10` | HP 100% 時の成功率 |
| `_maxChance` | `0.90` | HP 0% 時の成功率 |
| `_criticalHPRatio` | `0.30` | 確率曲線の折れ点 |

#### 確率曲線アルゴリズム

```
hpRatio = currentHP / maxHP
midChance = Lerp(_maxChance, _minChance, _criticalHPRatio)

if hpRatio ≤ _criticalHPRatio:   // 瀕死ゾーン（急上昇）
    t = hpRatio / _criticalHPRatio
    chance = Lerp(_maxChance, midChance, t)

else:                             // 通常ゾーン（緩やかな上昇）
    t = (hpRatio - _criticalHPRatio) / (1 - _criticalHPRatio)
    chance = Lerp(midChance, _minChance, t)

chance = Clamp01(chance + SkillEffectApplier.ScoutChanceBonus)
success = Random.Range(0f, 1f) ≤ chance
```

#### デフォルト値での確率分布

| 敵 HP | 成功率 |
|-------|-------|
| 100%  | ~10%  |
| 50%   | ~34%  |
| 30%   | ~58%  |
| 10%   | ~80%  |
| 0%    | ~90%  |

デザート系料理のバフ（`ScoutChanceBonus`）で加算補正が可能。

---

### 1.7 EnemyAIController（重み付きランダム）

**ファイル**: `Assets/Scripts/Battle/EnemyAIController.cs`

#### Inspector パラメータ

| フィールド | デフォルト値 | 説明 |
|-----------|-------------|------|
| `_basicAttackWeight` | `1.0` | 通常攻撃の選択重み |
| `_skillWeight` | `0.5` | スキルの選択重み |
| `_ultimateWeight` | `2.0` | 必殺技の選択重み（準備完了時のみ） |
| `_brokenTargetMultiplier` | `2.0` | ブレイク対象への優先倍率 |

#### アクション選択アルゴリズム

```
totalWeight = basicAttackWeight + skillWeight
            + (IsUltimateReady ? ultimateWeight : 0)

roll = Random(0, totalWeight)

if roll < basicAttackWeight      → 通常攻撃
else if roll < + skillWeight     → スキル
else                             → 必殺技
```

敵は SP 制約なし。スキルは常に使用可能。

#### ターゲット選択アルゴリズム（HP逆比例重み付け）

```
weight(target) = 1 / Max(currentHP / maxHP, 0.1)
if target.IsBroken:
    weight *= _brokenTargetMultiplier

// 全ターゲットの weight を正規化し、累積重みでランダム選択
```

HP が低いターゲットほど狙われやすく、ブレイク中のターゲットはさらに優先される。

---

### 1.8 必殺技割り込みシステム

**ファイル**: `Assets/Scripts/Battle/BattleManager.cs` 内 `UltimateInterruptSequence()`

必殺技は**任意の BattlePhase で割り込み発動**できる。

#### 割り込みフロー

```
1. 現在の Phase と ActiveCharacter を保存
2. _isUltimateInProgress = true（再入防止ガード）
3. EP 全消費: character.ConsumeAllEP()
4. カメラ演出:
   - SwitchToUltimateCamera (クローズアップ + スローモーション 0.3x, 0.25秒)
   - SwitchToUltimateActionCamera (TargetGroup ワイドショット)
5. ダメージ処理: CalculateUltimateDamage()
6. SHAKE_ULTIMATE_IMPACT (強度 0.7, 周波数 15, 時間 0.25)
7. CheckBattleEnd() — 必殺技でトドメの場合はここで Victory
8. 保存した Phase と ActiveCharacter を復元
9. _isUltimateInProgress = false
```

---

### 1.9 食事バフパイプライン

食事（料理）をバトル中に使用した場合のデータフロー。

#### パイプライン全体図

```
MealAction.ExecuteActionCoroutine(target, dish)
  ├→ target.Heal(dish.HealAmount)              // HP 回復
  ├→ MealBuffApplier.ApplyBuff(dish)            // SkillEffectApplier に書き込み
  ├→ BuffDurationTracker.RegisterBuff(dish)     // ターン持続管理に登録
  └→ Inventory.RemoveDish(dish)                 // インベントリから消費

MealBuffApplier.ApplyBuff(dish):
  switch (dish.Category):
    Meat    → SkillEffectApplier.AttackMultiplier  += buffAmount
    Fish    → SkillEffectApplier.SpeedMultiplier   += buffAmount
    Salad   → SkillEffectApplier.DefenseMultiplier += buffAmount
    Dessert → SkillEffectApplier.RegenPerTurn      += Round(buffAmount × 100)
    全共通  → SkillEffectApplier.ScoutChanceBonus  += scoutBonus

BuffDurationTracker.ProcessTurnEnd():
  1. HP 自動回復: RegenPerTurn があれば Heal()
  2. 全バフの RemainingTurns を -1
  3. 期限切れバフ: RemoveBuffFromApplier() で加算分を減算
```

**同カテゴリのバフは上書き（スタック不可）** — 持続ターンのみリフレッシュ。

---

## 2. カメラシステム

**ファイル**: `Assets/Scripts/Battle/BattleCameraManager.cs`

Cinemachine 3.x を使用。10台の仮想カメラを Priority 切替で管理。

#### カメラ一覧

| 仮想カメラ | 用途 | Priority |
|-----------|------|----------|
| `_vcamOverview` | 全体俯瞰 | 通常 20 |
| `_vcamTurnFocus` | ターン開始フォーカス | 切替時 20 |
| `_vcamBasicAttack` | プレイヤー通常攻撃 | 切替時 20 |
| `_vcamSkill` | スキル発動 | 切替時 20 |
| `_vcamEnemyWide` | 敵ターン（180度ルール準拠）| 切替時 20 |
| `_vcamUltimateClose` | 必殺技クローズアップ | 切替時 20 |
| `_vcamUltimateWide` | 必殺技ワイドショット | 切替時 20 |
| `_vcamImpact` | インパクトフラッシュ | 一瞬 40 |
| `_vcamVictory` | 勝利演出 | 切替時 20 |
| `_vcamDefeat` | 敗北演出 | 切替時 20 |

#### 180度ルールの実装

```csharp
public void SwitchToEnemyCamera(Transform enemy, Transform playerTarget)
{
    _vcamEnemyWide.Follow = playerTarget;   // カメラはプレイヤー側に配置
    _vcamEnemyWide.LookAt = enemy;          // 敵を見る
    ActivateVCam(_vcamEnemyWide);
}
```

敵ターン時でも必ず**プレイヤー側**にカメラを配置し、映画の180度ルールを厳守。
「敵の背後からプレイヤーを見る」アングルは設計上使用しない。

#### カメラシェイクプリセット

```csharp
static readonly Vector3 SHAKE_BASIC_HIT       = new(0.15f, 25f, 0.12f);  // (強度, 周波数, 時間)
static readonly Vector3 SHAKE_SKILL_HIT       = new(0.35f, 20f, 0.2f);
static readonly Vector3 SHAKE_ULTIMATE_IMPACT = new(0.7f,  15f, 0.25f);
static readonly Vector3 SHAKE_ENEMY_HIT       = new(0.08f, 25f, 0.1f);
static readonly Vector3 SHAKE_BREAK           = new(0.5f,  14f, 0.25f);
```

`CinemachineImpulseManager.Instance.IgnoreTimeScale = true` により、ヒットストップ中でもシェイクが動作。

---

## 3. 経営システム

### 3.1 CookingManager（調理アルゴリズム）

**ファイル**: `Assets/Scripts/Management/CookingManager.cs`

#### 品質スコア算出式

```
baseScore = recipe.AverageIngredientRarity()

freshnessMultiplier = Max(0.5, dailyFreshnessBuff + staffBuffs.FreshnessBonus)

calendarMultiplier  = (カレンダーイベント有効 && カテゴリ一致)
                      ? event.FreshnessMultiplier : 1.0

staffMultiplier     = 1 + staffBuffs.QualityBonus + staffBuffs.CategoryBonus(recipe.Category)

qualityScore = baseScore × freshnessMultiplier × calendarMultiplier × staffMultiplier
```

#### 品質判定閾値

| スコア範囲 | 品質 |
|-----------|------|
| < 1.0 | Poor |
| 1.0 ≤ score < 2.0 | Normal |
| 2.0 ≤ score < 3.5 | Fine |
| ≥ 3.5 | Exquisite |

#### 経験値獲得

```
xp = chefLevel × 20 + qualityBonus
qualityBonus = { Poor: 0, Normal: 10, Fine: 25, Exquisite: 50 }
```

---

### 3.2 DinerService（営業シミュレーション）

**ファイル**: `Assets/Scripts/Management/DinerService.cs`

#### 来客数算出

```
customersServed = _baseCustomerCount + (Reputation / 50) + HousingCustomerBonus
```

`_baseCustomerCount` = 5（デフォルト）

#### 各客の満足度計算

```
satisfaction = baseSatisfaction
             × (1 + staffBuffs.SatisfactionBonus + furnitureSatBonus)
             × calendarMultiplier

tip = RoundToInt(satisfaction × _tipMultiplier)
```

#### 営業結果集計

```
TotalRevenue = Σ(各客の料理価格)
TotalTips    = Σ(各客のチップ)
AverageSatisfaction = Σ(satisfaction) / customersServed
ReputationChange = RoundToInt(totalSatisfaction × _reputationMultiplier)
```

売上 + チップ → `GameManager.AddGold()`, 評判変動 → `GameManager.AddReputation()`

---

### 3.3 StaffManager（スタッフバフ集計）

**ファイル**: `Assets/Scripts/Management/StaffManager.cs`

#### スタッフ構成

| スロット | 上限 | 特徴 |
|---------|------|------|
| 常勤（Permanent）| 3名 | 解雇しない限り永続 |
| 臨時（Temporary）| 2名 | 翌日の朝にクリア |

#### GetActiveBonuses() 集計アルゴリズム

```
for each staff in (permanent + temporary):
    // 固定効果（種族由来）
    switch staff.Race.FixedEffect:
        Freshness  → summary.FreshnessBonus += value
        Quality    → summary.QualityBonus += value
        Speed      → summary.CookSpeedBonus += value
        Customer   → summary.CustomerBonus += value

    // ランダムバフ（個体ごとに異なる）
    for each buff in staff.RandomBuffs:
        switch buff.Type:
            CategorySpecialty → summary.AddCategoryBonus(buff.Category, buff.Value)
            Satisfaction      → summary.SatisfactionBonus += value
            ...
```

#### 朝の給与処理

```
ProcessMorningPayroll():
  for each staff:
    if TrySpendGold(staff.Salary) → OK
    else → staff.AddMoralePenalty()
           if penalty ≥ 3 → 自動退職（解雇）
```

安全な2パス処理: 退職者を `toRemove` リストに収集 → イテレーション後に一括削除。

---

### 3.4 CalendarEventData（カレンダーイベント）

**ファイル**: `Assets/Scripts/Data/CalendarEventData.cs`

ScriptableObject として定義。専用の Manager クラスは存在しない。

#### データ構造

```csharp
[SerializeField] int[] _triggerDays;            // 発動するゲーム日
[SerializeField] DishCategory _bonusCategory;   // ボーナス対象カテゴリ
[SerializeField] float _satisfactionMultiplier; // 満足度倍率（デフォルト 1.5）
[SerializeField] float _freshnessMultiplier;    // 鮮度倍率（デフォルト 1.2）
```

`IsActiveOnDay(int day)` で `_triggerDays` を線形走査して判定。
`CookingManager.Cook()` と `DinerService.RunService()` にパラメータとして渡される。

---

## 4. 品質スケールシステム

**ファイル**: `Assets/Scripts/Data/QualityScaleTable.cs`

`DishQuality`（Poor/Normal/Fine/Exquisite）に応じた倍率テーブル。

#### 倍率テーブル

| 品質 | HP回復 | バフ量 | スカウト | 売価 | 満足度 |
|------|--------|--------|---------|------|--------|
| Poor | 0.5 | 0.5 | 0.5 | 0.3 | 0.5 |
| Normal | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Fine | 1.3 | 1.3 | 1.5 | 1.5 | 1.4 |
| Exquisite | 1.8 | 1.8 | 2.0 | 2.5 | 2.0 |

`DishData` が `QualityScaleTable` への参照を保持し、`GetHealAmount(quality)` 等のメソッドで
基礎値 × 品質倍率を返す。`DishInstance` が実際の品質を保持する。

---

## 5. ドロップ計算

**ファイル**: `Assets/Scripts/Core/DropResolver.cs`

#### 判定フロー

```
ResolveDropWithResult(enemyData, isCritical):

  if isCritical && enemyData.DropItemJust != null:
      item = DropItemJust
      rate = DropRateJust
  else:
      item = DropItemNormal
      rate = DropRateNormal

  effectiveRate = Clamp01(rate + SkillEffectApplier.DropRateBonus)
  success = Random.value ≤ effectiveRate

  return DropResult { item, success }
```

- `isCritical` = ジャストアタック成功フラグ → ジャスト報酬ドロップ
- `DropRateBonus` = 食事バフによる上乗せ

---

## 6. バフ管理

### SkillEffectApplier（グローバルバフバス）

**ファイル**: `Assets/Scripts/Core/SkillEffectApplier.cs`

全 static プロパティによるグローバルパラメータバス:

| プロパティ | デフォルト | 書き込み元 | 読み取り元 |
|-----------|-----------|-----------|-----------|
| `AttackMultiplier` | 1.0 | MealBuffApplier | CharacterBattleController |
| `DefenseMultiplier` | 1.0 | MealBuffApplier | (将来拡張) |
| `SpeedMultiplier` | 1.0 | MealBuffApplier | (将来拡張) |
| `RegenPerTurn` | 0 | MealBuffApplier | BuffDurationTracker |
| `ScoutChanceBonus` | 0.0 | MealBuffApplier | ScoutAction |
| `DropRateBonus` | 0.0 | (将来拡張) | DropResolver |

`Start()` で `ResetAll()` を呼び、シーン間のバフ持ち越しを防止。

### BuffDurationTracker（ターン持続管理）

**ファイル**: `Assets/Scripts/Battle/BuffDurationTracker.cs`

- **同カテゴリ上書きルール**: 同じ `DishCategory` のバフは新しいもので上書き（スタックしない）
- `ProcessTurnEnd()` でターン経過を管理、期限切れバフは `SkillEffectApplier` から減算

---

## 7. UI 実装技術

### 7.1 UI Toolkit vs uGUI Canvas 使い分け

本プロジェクトでは**2つの UI 技術を併用**する:

| 技術 | 使用箇所 | 理由 |
|------|---------|------|
| **UI Toolkit** | TitleScreenUI, BattleResultUI, RevolverMenuController | 構造化されたレイアウト向き |
| **uGUI Canvas** | ActionTimelineUI, CharacterStatusUI, DamageNumberUI, BattleEffectsUI, UltimatePortraitUI, SkillCommandUI, EnemyStatusUI | プロシージャル生成 + 高度なアニメーション |

**原則**: 同一コンポーネント内で両技術を混在させない。

---

### 7.2 プロシージャルスプライト生成

全 UI コンポーネントで共通する中核技術。**外部画像アセットを一切使用しない**。

#### 基本パターン

```csharp
private static Sprite MakeCircleSprite(int size, Color col, float border)
{
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    Color[] pixels = new Color[size * size];
    float center = size * 0.5f;
    float radius = center - 1f;

    for (int y = 0; y < size; y++)
    for (int x = 0; x < size; x++)
    {
        float dist = Vector2.Distance(new(x, y), new(center, center));
        // アンチエイリアス + ボーダー処理
        pixels[y * size + x] = CalculatePixelColor(dist, radius, border, col);
    }

    tex.SetPixels(pixels);
    tex.Apply();
    return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
}
```

#### 生成されるスプライト種類

| 種類 | 使用箇所 |
|------|---------|
| 円（Circle）| タイムラインアイコン、ポートレート枠 |
| 角丸矩形（RoundedRect）| HP カード背景、コマンドボタン |
| グラデーションバー（GradientBar）| HP バー、タフネスバー |
| グロー円（GlowCircle）| アクティブ発光エフェクト |
| シマー帯（Shimmer）| HP バーのシマー演出 |
| リング（Ring）| EP リング |
| ダイヤモンド（Diamond）| スパークルパーティクル |

#### 静的キャッシュ

```csharp
private static Sprite _sCircle;
private static Sprite _sBarBg;
// ...
private static void EnsureSprites()
{
    if (_sCircle != null) return;  // 既に生成済みならスキップ
    _sCircle = MakeCircleSprite(64, Color.white, 2f);
    // ...
}
```

インスタンス間で共有。一度だけ生成して `static` フィールドにキャッシュ。

---

### 7.3 ActionTimelineUI（タイムライン表示）

**ファイル**: `Assets/Scripts/UI/ActionTimelineUI.cs`

Star Rail 風の縦型行動順リスト。

#### アニメーション技術

- `Time.unscaledDeltaTime` を使用 → ヒットストップ中も UI が停止しない
- 位置・透明度・スケールを毎フレーム `Lerp` で補間
- アクティブエントリのグロー画像に `Sin` 波パルスアニメーション
- `_prevOrder` と現在の順番を差分比較し、移動したエントリのみアニメーション

#### レイアウト定数

```csharp
const int MAX_VISIBLE = 10;        // 最大表示数
const float ACTIVE_SIZE = 88f;     // アクティブアイコンサイズ
const float QUEUE_SIZE = 70f;      // 待機アイコンサイズ
```

---

### 7.4 CharacterStatusUI（HP カード）

**ファイル**: `Assets/Scripts/UI/CharacterStatusUI.cs`

画面下部のパーティ HP カード行。

#### HP ラグバー技術

```
TargetRatio = currentHP / maxHP
LagRatio は TargetRatio に向かって Lerp で遅延追従（白いバーが徐々に減る）
```

ダメージ時: 緑バー（TargetRatio）が即座に減少 → 白バー（LagRatio）が遅れて追いつく演出。

#### 低 HP アラート

```csharp
const float LOW_HP_THRESHOLD = 0.30f;
```

HP 30% 以下: バー色が緑→オレンジ→赤に変化 + 明度の Sin 波オシレーション。

#### シマースウィープ

約5.5秒ごとにHP バー上を半透明の白い帯が左→右にスウィープ。

---

### 7.5 DamageNumberUI（ダメージポップアップ）

**ファイル**: `Assets/Scripts/UI/DamageNumberUI.cs`

スクリーンスペースオーバーレイ（SortOrder=200、最前面）。

#### アニメーションステップ

```
1. ワールド座標 → スクリーン座標 → Canvas ローカル座標に変換
2. EaseOutBack でポップインスケール（オーバーシュート 2.2）
3. 弱点ヒット時: 減衰 Sin 波によるシェイク
4. √t カーブで上方フロート
5. 2次イーズインでフェードアウト
```

#### EaseOutBack 関数

```csharp
static float EaseOutBack(float t)
{
    const float c1 = 1.70158f;
    const float c3 = c1 + 1;
    return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
}
```

#### 弱点ヒット差分

| 項目 | 通常ヒット | 弱点ヒット |
|------|-----------|-----------|
| フォントサイズ | 通常 | 1.3倍 |
| 色 | 属性色 | ゴールド |
| 接頭辞 | なし | "弱点!" |
| シェイク | なし | あり |

ブレイク時: "BREAK!" サブラベルが追加表示。

---

### 7.6 BattleEffectsUI（演出エフェクト）

**ファイル**: `Assets/Scripts/UI/BattleEffectsUI.cs`

全画面シネマティックエフェクトの集約。Canvas SortOrder=150。
**完全プロシージャル**（外部アセット不使用）。

#### 主要エフェクト一覧

| メソッド | 演出時間 | 内容 |
|---------|---------|------|
| `PlayBattleStartEffect()` | ~3.2秒 | 黒ホールド → 斜めワイプ → テキストスライド + スピードライン |
| `PlayVictoryEffect()` | ~3.5秒 | オーバーレイフェード → テキストスケールイン → 紙吹雪バースト |
| `PlayDefeatEffect()` | ~3.2秒 | ダークオーバーレイ → 赤テキスト → 画面シェイク → ひび割れライン |
| `PlayUltimateCutIn()` | ~1.4秒 | BG フェード → 斜線 → 名前スライド → フラッシュ |
| `PlaySkillNameDisplay()` | ~1.2秒 | スライドイン → ホールド → フェードアウト |
| `PlayBreakExplosion()` | 即座 | 放射状ラインバースト + ビネット |

#### 紙吹雪物理

```
for each piece:
    velocityY -= gravity × dt        // 重力落下
    positionX += Sin(swayPhase) × swayAmplitude  // 横揺れ
    rotation  += rotationSpeed × dt   // 回転
```

---

### 7.7 UltimatePortraitUI（EP リングポートレート）

**ファイル**: `Assets/Scripts/UI/UltimatePortraitUI.cs`

画面左下の EP 進捗リングポートレート（最大4キャラ）。

#### リングフィルアニメーション

```
DisplayedFillAmount = Lerp(DisplayedFillAmount, TargetFillAmount, speed × dt)
```

EP が `MaxEP` に到達:
1. リング色が属性色 → ゴールドパルスに変化
2. スケールバウンス（1.0 → 1.15 → 1.0）
3. フラッシュエフェクト
4. ショックウェーブリング拡大

#### 浮遊パーティクル

- **ダイヤモンドスパークル**: 各ポートレートに4個、上下にフロート
- **ライジングパーティクル**: EP 充填に応じて数が増加、下→上に浮遊 + Sin 揺れ

---

### 7.8 SkillCommandUI（コマンド選択）

**ファイル**: `Assets/Scripts/UI/SkillCommandUI.cs`

画面下部中央のアタック/スキル2ボタン。Star Rail 風デザイン。

#### 2段階操作フロー

```
Stage 1: コマンド選択
  攻撃ボタン or スキルボタン → OnCommandSelected(ActionType) 発火

Stage 2: ターゲット選択
  EnterTargetSelection() でターゲットリスト表示
  → OnTargetConfirmed(ActionType, Target) 発火
  → BattleManager.ExecutePlayerAction() 呼び出し
```

キーボード操作: 矢印/WASD でナビゲート、Enter/Space で確定、Escape でキャンセル。

#### SP ピップアニメーション

SP 増減時に各ピップにアニメーション（ゲイン: スケールバウンス、コンシューム: フェード）。

---

### 7.9 RevolverMenuController（リボルバーメニュー）

**ファイル**: `Assets/Scripts/UI/RevolverMenuController.cs`

**UI Toolkit** ベースのリボルバー型コマンドメニュー。

#### 回転メカニズム

```csharp
// シリンダー全体を回転
_cylinder.style.rotate = new StyleRotate(new Rotate(currentAngle));

// LateUpdate で各弾丸を逆回転（ラベルが読めるように）
foreach bullet:
    bullet.style.rotate = new StyleRotate(new Rotate(-currentAngle));
```

#### 弾丸配置（極座標）

```
angle(i) = i × (360 / bulletCount) - 90°  // 12時方向が起点
x = centerX + radius × Cos(angle)
y = centerY + radius × Sin(angle)
```

#### UIDocument 初期化レースコンディション対策

```csharp
private bool _pendingShow;

private bool TryResolveUI()
{
    _root = _uiDocument.rootVisualElement;
    if (_root == null) return false;
    // UI 構築...
    return true;
}

void Update()
{
    if (_pendingShow && TryResolveUI())
    {
        _pendingShow = false;
        // Show 処理を再実行
    }
}
```

`UIDocument.rootVisualElement` が `null` の場合（UI 未構築）に `_pendingShow` フラグで次フレームにリトライ。

---

## 8. コアシステム

### 8.1 GameManager（シングルトン・永続状態）

**ファイル**: `Assets/Scripts/Core/GameManager.cs`

#### シングルトンパターン

```csharp
public static GameManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
    // サブマネージャーを同一 GameObject に AddComponent
}
```

同一 GameObject に以下を `AddComponent`:
- `InventoryManager`
- `SaveDataManager`
- `StaffManager`
- `HousingManager`
- `AudioManager`
- `DebugController`
- `SkillEffectApplier`

#### ゲームフェーズ遷移

```
Morning → Noon → Evening → Morning (Day+1)
```

```csharp
public void AdvancePhase()
{
    switch (CurrentPhase)
    {
        case Morning:  CurrentPhase = Noon;    LoadSceneAsync("FieldScene"); break;
        case Noon:     CurrentPhase = Evening; LoadSceneAsync("ManagementScene"); break;
        case Evening:  CurrentPhase = Morning; CurrentDay++; LoadSceneAsync("BaseScene"); break;
    }
    OnPhaseChanged?.Invoke(CurrentPhase);
}
```

#### ChefLevel 算出

```csharp
static readonly int[] LEVEL_THRESHOLDS = { 0, 100, 300, 600, 1000, 1500 };

void AddCookingXP(int xp)
{
    CookingXP += xp;
    // 逆順スキャンで最大到達レベルを判定
    for (int i = LEVEL_THRESHOLDS.Length - 1; i >= 0; i--)
    {
        if (CookingXP >= LEVEL_THRESHOLDS[i])
        {
            ChefLevel = i + 1;
            break;
        }
    }
}
```

#### timeScale 安全監視

```csharp
void LateUpdate()
{
    // バトルシーン以外で timeScale が異常値の場合に自動修正
    if (SceneManager.GetActiveScene().name != "BattleScene" && Time.timeScale != 1f)
    {
        ForceRestoreTimeScale();
    }
}
```

ヒットストップのリーク（バトル終了時に timeScale が戻らなかった場合）を検知・修復。

---

### 8.2 InventoryManager（インベントリ管理）

**ファイル**: `Assets/Scripts/Core/InventoryManager.cs`

#### デュアルストア設計

```
Store 1: Dictionary<ItemData, int>        ← 素材・武器（品質なし）
Store 2: Dictionary<DishInstance, int>     ← 料理（品質あり）
```

`DishInstance` は `DishQuality` を保持するため、同じ `DishData` でも品質が異なれば別エントリ。

#### ジェネリック型フィルタ

```csharp
public List<KeyValuePair<T, int>> GetItemsOfType<T>() where T : ItemData
{
    return _items
        .Where(kvp => kvp.Key is T)
        .Select(kvp => new KeyValuePair<T, int>((T)kvp.Key, kvp.Value))
        .ToList();
}
```

`GetItemsOfType<WeaponData>()` で武器のみ、`GetItemsOfType<IngredientData>()` で素材のみを取得。

---

### 8.3 SaveDataManager（セーブ/ロード）

**ファイル**: `Assets/Scripts/Core/SaveDataManager.cs`

#### シリアライゼーション方式

- **フォーマット**: JSON（`JsonUtility.ToJson`/`FromJson`）
- **保存先**: `Application.persistentDataPath/save_data.json`
- **POCO**: ネストされた `[Serializable] SaveData` クラス

#### アイテム復元フロー

```
Load():
  1. Resources.LoadAll<ItemData>("") でプロジェクト内の全 ItemData を辞書化
  2. SaveData.Items をイテレーション
  3. 各 ItemEntry の ItemID で辞書から ItemData を逆引き
  4. Quality フィールドが存在 && itemData is DishData
     → DishInstance 生成 → AddDish()
     → それ以外 → Add()
```

#### 後方互換

旧フォーマットの `Materials` リスト（string ID + int Count）にも対応。
`Resources.LoadAll<ItemData>("")` で旧 MaterialData ID も検索可能。

---

### 8.4 AudioManager / AudioEventConnector

**AudioManager** (`Assets/Scripts/Core/AudioManager.cs`):

```
SE: Dictionary<string, SEEntry> で文字列キー管理 → PlayOneShot（重複再生可）
BGM: 単一 AudioSource でループ再生 → 同じクリップなら再生スキップ（冪等性）
```

**AudioEventConnector** (`Assets/Scripts/Core/AudioEventConnector.cs`):

**純粋なイベント配線ブリッジ**。BattleManager や GameManager のイベントに lambda を購読し、
適切な SE キーで `AudioManager.PlaySE()` を呼ぶ。

```csharp
static void WireBattle(BattleManager bm)
{
    bm.OnDamageDealt += result =>
    {
        string key = result.IsWeakness ? SE_JUST_HIT : SE_ATTACK_HIT;
        AudioManager.Instance.PlaySE(key);
    };
    bm.OnBattleEnd += victory =>
        AudioManager.Instance.PlaySE(victory ? SE_VICTORY : SE_DEFEAT);
    // ...
}

static void WireSceneBGM()
{
    GameManager.Instance.OnSceneLoaded += sceneName =>
    {
        // シーン名に応じた BGM を再生
    };
}
```

呼び出し箇所:
- `WireBattle()` → `BattleSceneBootstrap.Start()` から
- `WireSceneBGM()` → `BootLoader.Start()` から

---

## 9. データアーキテクチャ

### 9.1 ScriptableObject 継承ツリー

```
ScriptableObject
├── ItemData                    (基底: _itemID, _displayName, _icon, _sellPrice)
│   ├── IngredientData          (+_rarity, _dropRate, _gaugeSpeedMultiplier)
│   ├── DishData                (+_category, _hpRecovery, _baseBuff, _qualityTable)
│   └── WeaponData              (+_baseDamage, _justInputFrameBonus, _animatorOverride)
├── CharacterStats              (キャラクター定義: HP/ATK/DEF/SPD/属性/耐性/EP)
├── EnemyData                   (敵固有: ドロップ, ゴールド報酬, StaffRace)
├── MapData                     (マップ: 環境, 必要ShopLevel, シーン名)
├── RecipeData                  (レシピ: 必要素材リスト, 必要ChefLevel)
├── QualityScaleTable           (品質倍率テーブル)
├── StaffRaceData               (スタッフ種族: 固定効果)
├── StaffBuffData               (スタッフバフ: タイプ, 値)
├── CalendarEventData           (カレンダーイベント)
├── FurnitureData               (家具: タイプ, ボーナス値)
└── MaterialData [Obsolete]     (旧素材データ → IngredientData に移行)
```

### 9.2 CharacterStats（キャラクターデータ）

プレイヤーキャラと敵の**共通ステータス定義**。

#### 属性耐性テーブル

```csharp
// 各属性の耐性値
float _physicalRes, _fireRes, _iceRes, _lightningRes, _windRes, _darkRes;

// 耐性値の意味:
//  -0.5 = 弱点（1.5倍ダメージ）
//   0.0 = 通常
//   0.5 = 耐性（0.5倍ダメージ）
//   1.0 = 無効（0倍ダメージ）
```

#### タフネス・弱点システム

```csharp
[SerializeField] int _maxToughness;
[SerializeField] ElementType[] _weakElements;

public bool IsWeakTo(ElementType element) => Array.Contains(_weakElements, element);
```

弱点属性でヒットするとタフネス -30。タフネス 0 でブレイク（全ダメージ +25%）。

### 9.3 EnemyData（敵固有データ）

`CharacterStats` とは**別の ScriptableObject**。経営・ドロップ面のデータを保持。

```
EnemyData
  ├── DropItemNormal : ItemData    (通常ドロップ)
  ├── DropItemJust   : ItemData    (ジャスト報酬ドロップ)
  ├── DropRateNormal : float       (通常ドロップ率)
  ├── DropRateJust   : float       (ジャストドロップ率)
  ├── GoldReward     : int         (撃破報酬ゴールド)
  └── StaffRace      : StaffRaceData  (スカウト時の種族)
```

バトル中は `CharacterBattleController` が `CharacterStats` と `EnemyData` の両方を保持。

---

## 10. エディタツール

### 10.1 SampleDataGenerator

**ファイル**: `Assets/Scripts/Editor/SampleDataGenerator.cs`
**メニュー**: `DevilsDiner > Generate Sample Staff & Calendar Data`

#### SerializedObject パターン

ScriptableObject の `private [SerializeField]` フィールドに Editor から値を設定する方法:

```csharp
static IngredientData CreateIngredient(IngDef def)
{
    var asset = ScriptableObject.CreateInstance<IngredientData>();
    AssetDatabase.CreateAsset(asset, path);

    var so = new SerializedObject(asset);
    so.FindProperty("_itemID").stringValue = def.Id;
    so.FindProperty("_displayName").stringValue = def.DisplayName;
    so.FindProperty("_rarity").intValue = def.Rarity;
    so.FindProperty("_dropRate").floatValue = def.DropRate;
    so.ApplyModifiedPropertiesWithoutUndo();  // Undo 履歴に残さない

    return asset;
}
```

**重要**: `FindProperty()` の引数は C# のフィールド名（`_itemID`）であり、プロパティ名（`ItemID`）ではない。

#### 冪等性保証

```csharp
var existing = AssetDatabase.LoadAssetAtPath<T>(path);
if (existing != null) return existing;  // 既存アセットがあればスキップ
```

再実行しても同じアセットが二重生成されない。

#### 生成順序（依存関係を考慮）

```
1. StaffBuffData[]        ← 依存なし
2. StaffRaceData[]        ← StaffBuffData に依存
3. CalendarEventData      ← 依存なし
4. FurnitureData          ← 依存なし
5. WireEnemyRaces()       ← StaffRaceData + EnemyData
6. IngredientData[]       ← 依存なし
7. QualityScaleTable      ← 依存なし
8. DishData[]             ← QualityScaleTable に依存
9. RecipeData[]           ← DishData + IngredientData に依存
10. WireEnemyDrops()      ← IngredientData + EnemyData
```

---

### 10.2 DevilsDinerSetupTool

**ファイル**: `Assets/Scripts/Editor/DevilsDinerSetupTool.cs`
**メニュー**: `DevilsDiner > Auto Setup Battle Scene`

#### 5ステップ進捗バー

```
Step 1/5: フォルダ作成
Step 2/5: ScriptableObject 作成
Step 3/5: シーン作成/開く
Step 4/5: シーン階層構築
Step 5/5: Build Settings 登録
```

`try/finally` で `EditorUtility.ClearProgressBar()` を保証。

#### シーン階層構築

`BuildSceneHierarchy()` で以下を自動配置:
- Camera + Cinemachine Brain
- Directional Light
- BattleManager + BattleCameraManager
- バトルキャラ（プリミティブ Capsule）
- 10台の Cinemachine VirtualCamera
- UI Canvas + 全 UI コンポーネント
- UI Toolkit UIDocument + DynamicBattleUIController

#### デュアル UI モード

```csharp
static void CreateMetaphorBattleUI()
{
    // UI Toolkit 版を作成
    var uiDoc = CreateUIDocument();
    var dynamicUI = AddComponent<DynamicBattleUIController>();

    // 旧 uGUI 版を無効化（削除はしない）
    DisableLegacyCanvasComponents();
}
```

---

### 10.3 Bootstrap パターン

各シーンに1つの Bootstrap スクリプトが存在し、シーン内の全コンポーネントを自動結線する。

#### 共通パターン

```csharp
public sealed class XxxSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        EnsureGameManagerExists();  // フォールバック生成

        // 1. 既存コンポーネントの検索
        var manager = FindFirstObjectByType<XxxManager>();

        // 2. 不足コンポーネントの自動生成
        if (manager == null)
        {
            manager = new GameObject("XxxManager").AddComponent<XxxManager>();
        }

        // 3. 依存注入（Set* メソッド or Initialize）
        manager.SetDependency(otherComponent);

        // 4. 初期化開始
        manager.StartXxx();
    }

    private static void EnsureGameManagerExists()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager [Fallback]");
        go.AddComponent<GameManager>();
        Debug.LogWarning("GameManager フォールバック生成");
    }
}
```

#### 各シーンの Bootstrap

| シーン | Bootstrap | 主な責務 |
|-------|-----------|---------|
| BootScene | BootLoader | タイトル画面表示、AudioEventConnector.WireSceneBGM() |
| BaseScene | BaseSceneBootstrap | CookingManager 生成、BaseSceneUI 初期化 |
| FieldScene | FieldSceneBootstrap | FieldPlayerController, EnemySymbol, EncounterHandler |
| BattleScene | BattleSceneBootstrap | 全バトルサブシステムの生成・結線・開始 |
| ManagementScene | ManagementSceneBootstrap | DinerService, StaffManager UI |

---

## 11. 設計パターン総覧

| パターン | 使用箇所 | 説明 |
|---------|---------|------|
| **コルーチンステートマシン** | BattleManager | `Update()` を使わず `IEnumerator` チェーンで状態遷移 |
| **依存注入（Setter）** | BattleManager ← Bootstrap | `Set*()` メソッドで全サブシステムを注入 |
| **イベント駆動（Observer）** | BattleManager → BattleUIManager | `event Action<T>` でロジックと表示を分離 |
| **メディエータ** | BattleUIManager | 全 UI コンポーネント間の通信を仲介 |
| **シングルトン + DontDestroyOnLoad** | GameManager, AudioManager | シーン遷移をまたぐ永続オブジェクト |
| **ScriptableObject データ駆動** | CharacterStats, ItemData 等 | ゲームデータを Inspector で編集可能に |
| **プロシージャルスプライト** | 全 UI コンポーネント | 外部アセット不要の動的テクスチャ生成 |
| **Static パラメータバス** | SkillEffectApplier | グローバルバフ値の読み書きハブ |
| **デュアルストア** | InventoryManager | 品質なしアイテムと品質ありDish を別辞書で管理 |
| **Bootstrap 自動結線** | 各 SceneBootstrap | シーン読み込み時に全コンポーネントを検索・生成・接続 |
| **SerializedObject Editor パターン** | SampleDataGenerator | private フィールドへの Editor 時書き込み |
| **冪等アセット生成** | SampleDataGenerator | 既存チェック → スキップで再実行安全性を保証 |
| **timeScale 安全設計** | LateUpdate 監視 + unscaledDeltaTime | ヒットストップのリーク防止と UI の timeScale 独立 |
| **180度ルール** | BattleCameraManager | 映画的カメラワークのルール厳守 |
| **区分線形補間** | ScoutAction | HP に応じた非線形確率曲線 |
| **AV ターン制** | ActionQueueSystem | Star Rail 方式の速度ベースターン順決定 |
| **品質スケール委譲** | DishData → QualityScaleTable | 品質倍率を外部 SO に委譲し、Inspector で調整可能に |

---

*本ドキュメントは Devil's Diner プロジェクトの全65ファイル・約24,000行のソースコードを元に作成されています。*
