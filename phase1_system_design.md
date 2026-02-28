# Devil's Diner ～魔界の荒野とガンブレード～ フェーズ1 統合システム設計書

**作成日**: 2026-02-28
**作成者**: core-architect
**対象バージョン**: Phase 1（プロトタイプ）
**エンジン**: Unity 6 (6000.3.10f1) / C# (.NET Standard 2.1)

---

## 目次

1. [レビュー結果](#1-レビュー結果)
2. [GameManager 設計](#2-gamemanager-singleton-設計)
3. [InventoryManager 設計](#3-inventorymanager-設計)
4. [シーン遷移フロー図](#4-シーン遷移フロー図)
5. [全クラス依存関係・モジュール構成図](#5-全クラス依存関係モジュール構成図)
6. [フェーズ1統合アーキテクチャ図](#6-フェーズ1統合アーキテクチャ図)
7. [データフロー図](#7-データフロー図)
8. [パフォーマンスガイドライン（統合版）](#8-パフォーマンスガイドライン統合版)
9. [フェーズ1実装優先度とタスク依存関係](#9-フェーズ1実装優先度とタスク依存関係)

---

## 1. レビュー結果

### 1.1 不整合・矛盾点

#### (A) データ構造の命名と定義の不一致

| 項目 | action-programmer | sim-programmer | 問題点 |
|------|-------------------|----------------|--------|
| 武器データ | `WeaponDataSO` (ScriptableObject) | 要件定義の `WeaponData` を参照 | 命名規則が不統一。SOサフィックスの付与ルールを統一すべき |
| 素材データ | `DamageInfo.baseDamage` (struct) | `MaterialData._basePrice` (SO) | ドロップ素材とダメージ計算の接続点が未定義。敵撃破時に何がドロップするかのフローが不明 |
| ドロップ判定 | 設計に記載なし | `MaterialData` は存在するが取得ロジックなし | **重大**: 敵撃破 → 素材ドロップ → インベントリ追加のパイプラインが両者とも未設計 |

#### (B) インターフェースの接続断絶

1. **IDamageable と敵AI の接続**: action-programmer は `IDamageable` インターフェースを定義しているが、敵の具体実装（EnemyController等）が未設計。sim-programmer 側にも敵クラスは存在しない。フェーズ1で敵を1体でも動かすには、`EnemyController : MonoBehaviour, IDamageable` の最低限の実装が必要。

2. **HitResult と素材ドロップの接続**: `HitResult.IsJustInput` フラグは存在するが、これが「ジャスト入力時は dropItemID_Just を使う」という要件定義上のロジックに結びついていない。ドロップ判定ロジック (`DropResolver` 等) が必要。

3. **CookingMinigame への素材供給**: sim-programmer の `CookingMinigame` は `RecipeData._requiredMaterials` を参照するが、プレイヤーがその素材を持っているかの在庫確認（InventoryManager連携）が設計に含まれていない。

4. **CustomerAI のオーダーと CookingMinigame の接続**: `CustomerAI` の `OnCustomerOrdered` イベントと `CookingMinigame` の起動トリガーの間にディスパッチャが存在しない。オーダーキュー管理が未設計。

#### (C) イベントシステムの非対称性

- action-programmer: `OnAttackPhaseChanged` (Action型と推測) を使用
- sim-programmer: `OnCookingCompleted`, `OnCustomerOrdered`, `OnPaymentMade` (型の明示なし)
- **問題**: イベントの型安全性が不明。`System.Action<T>` なのか `UnityEvent<T>` なのかが統一されていない。基盤として統一ルールを設ける必要がある。

### 1.2 アーキテクチャ上の懸念

#### (A) 密結合リスク

1. **JustInputAction の Time.timeScale 直接操作**: `Time.timeScale = 0.05f` を直接書き換えるため、他のシステム（UIアニメーション、音声、パーティクル）すべてに影響する。特に sim-programmer 側の `CookingMinigame` が `Time.time` を使って `Mathf.PingPong` しているため、万一アクションシーンと経営シーンの境界が曖昧になった場合に深刻な不具合を引き起こす。
   - **対策**: シーンが完全分離されている限り問題ないが、将来的に両者が同一シーンに共存する可能性を考慮し、CookingMinigame は `Time.unscaledTime` ベースに変更を推奨。

2. **PlayerController の肥大化リスク**: 移動、ジャンプ、スプリント、攻撃、回避、被弾、死亡の全ステートを単一クラスで管理。ステートパターンの導入は示唆されているが、具体的な分離案がない。
   - **対策**: フェーズ1はプロトタイプのため現状許容。フェーズ2で `IPlayerState` インターフェースによるステートパターンへリファクタリングを計画。

3. **CustomerAI の switch-case ステートマシン**: 8ステートを `Update()` 内の `switch` で処理するのは可読性・保守性が低い。
   - **対策**: フェーズ1は許容。ステート数が増加するフェーズ2以降で、ステートパターンまたは StateMachineBehaviour への移行を計画。

#### (B) 責務の曖昧さ

1. **ドロップ判定の責務**: 敵撃破時のドロップ判定は、敵側（EnemyController）が行うのか、ダメージ計算側（DamageCalculator等）が行うのか、あるいは専用の DropSystem が行うのか未定義。
   - **決定**: 敵撃破時に `EnemyController` が `DropResolver.ResolveDrop(EnemyData, HitResult)` を呼び出し、結果を `InventoryManager.AddMaterial()` に渡す方式とする。

2. **支払い処理のデータ反映先**: `CustomerAI.OnPaymentMade` で発生した所持金の変更を、誰が `GameManager.Gold` に反映するのか不明。
   - **決定**: `DinerManager`（経営シーン管理クラス、新設）が `OnPaymentMade` をリッスンし、`GameManager.Instance.AddGold(amount)` を呼び出す。

#### (C) 拡張性の問題

1. **ScriptableObject の ID 管理**: `MaterialData`, `RecipeData` 等に `id` フィールドがあるが、ID の一意性保証メカニズムが未設計。手動割り当ては衝突リスクが高い。
   - **対策**: フェーズ1では手動管理を許容するが、各SOに `[CreateAssetMenu]` を付与し、命名規則 `MAT_001`, `RCP_001` 等でプレフィックス管理。フェーズ2でエディター拡張による自動採番を検討。

### 1.3 パフォーマンスリスク

#### (A) timeScale 変更の副作用（重要度: 高）

- `Time.fixedDeltaTime` を `timeScale` に比例させる設計は正しいが、復帰時に元の `fixedDeltaTime` を正確に復元する必要がある。`OnDisable` での `ForceRestoreTimeScale()` は良い設計だが、例外発生時のフェイルセーフとして `Time.timeScale != 1f` をフレーム冒頭で検知する安全弁を GameManager に設けるべき。
- `Animator`, `AudioSource`, `ParticleSystem` は `timeScale` の影響を受けるため、ヒットストップ中のUI演出は `AnimatorUpdateMode.UnscaledTime` を使用すること。

#### (B) NavMesh 負荷（重要度: 中）

- 30体制限は妥当。ただし `NavMeshAgent.SetDestination()` を毎フレーム呼ぶとCPU負荷が高い。ステート遷移時のみ1回呼び出すことを厳守。
- NavMesh の Bake は ManagementScene ロード時に1回のみ。動的障害物は `NavMeshObstacle` (Carve モード) で対応。

#### (C) GC アロケーション（重要度: 中）

- `DamageInfo` と `HitResult` が struct であるのは良い判断。ただし `CookingResult` も struct であることを確認。
- `CustomerAI` のメニュー選択で `List` のシャッフルを毎回行う場合、一時リスト生成で GC が発生する。`SeatManager.TryReserveSeat()` のシャッフルも同様。
- **対策**: 共有バッファ（`static List<T>` のクリア＆再利用パターン）を使用する。

### 1.4 改善提案まとめ

| 優先度 | 項目 | 対策 |
|--------|------|------|
| 必須 | ドロップシステム未設計 | `DropResolver` 静的クラスを新設。`EnemyController` から呼び出し |
| 必須 | オーダーキュー未設計 | `OrderQueue` クラスを新設。CustomerAI → OrderQueue → CookingMinigame |
| 必須 | 所持金反映フロー未設計 | `DinerManager` を新設。OnPaymentMade → GameManager.AddGold |
| 高 | SO命名規則統一 | `WeaponDataSO` → `WeaponData` に統一（SOサフィックス廃止）、ファイル名はSO付き |
| 高 | イベント型統一 | シーン内通信は `System.Action<T>`, シーン横断は `GameManager` 経由 |
| 高 | timeScale安全弁 | GameManager に timeScale 監視ロジック追加 |
| 中 | CookingMinigame の時間ベース | `Time.unscaledTime` への変更を推奨 |
| 中 | GC対策 | 共有バッファパターンの適用 |
| 低 | ステートパターン導入 | フェーズ2で PlayerController, CustomerAI をリファクタリング |

---

## 2. GameManager (Singleton) 設計

### 2.1 設計方針

- `DontDestroyOnLoad` による永続化
- 厳密な Singleton パターン（二重生成防止）
- シーン遷移の一元管理
- ゲーム進行データの保持
- timeScale 安全弁の提供

### 2.2 GamePhase 定義

```
Morning（朝: 出撃準備） → Noon（昼: 狩猟アクション） → Evening（夕方: 調理・開店準備） → Night（夜: 経営・鑑賞） → Midnight（深夜: リザルト）
```

### 2.3 GameManager コード

```csharp
// ============================================================
// GameManager.cs
// BootScene に配置。DontDestroyOnLoad で全シーンをまたいで永続化。
// ゲーム進行状態の管理、シーン遷移、timeScale 安全弁を担当。
// ============================================================
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体の進行状態を管理するシングルトン。
/// BootScene 上の GameObject にアタッチし、DontDestroyOnLoad で永続化する。
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Singleton
    // ──────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const string BOOT_SCENE    = "BootScene";
    private const string ACTION_SCENE  = "ActionScene";
    private const string MANAGEMENT_SCENE = "ManagementScene";
    private const float  DEFAULT_FIXED_DELTA_TIME = 0.02f; // 50 Hz
    private const int    STARTING_GOLD = 500;
    private const int    STARTING_DAY  = 1;

    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>1日のゲームフェーズ。</summary>
    public enum GamePhase
    {
        /// <summary>朝 — 出撃準備（ActionScene ロード前）</summary>
        Morning,
        /// <summary>昼 — 狩猟アクション（ActionScene）</summary>
        Noon,
        /// <summary>夕方 — 調理・開店準備（ManagementScene ロード直後）</summary>
        Evening,
        /// <summary>夜 — 経営・鑑賞（ManagementScene）</summary>
        Night,
        /// <summary>深夜 — リザルト表示</summary>
        Midnight
    }

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>フェーズが変化したとき。引数は新フェーズ。</summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary>日数が進んだとき。引数は新しい日数。</summary>
    public event Action<int> OnDayAdvanced;

    /// <summary>所持金が変化したとき。引数は変化後の所持金。</summary>
    public event Action<int> OnGoldChanged;

    /// <summary>シーンロードが完了したとき。引数はロードされたシーン名。</summary>
    public event Action<string> OnSceneLoaded;

    // ──────────────────────────────────────────────
    // ゲーム進行データ
    // ──────────────────────────────────────────────

    /// <summary>現在の日数（1始まり）。</summary>
    public int CurrentDay { get; private set; } = STARTING_DAY;

    /// <summary>現在のフェーズ。</summary>
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Morning;

    /// <summary>所持金。</summary>
    public int Gold { get; private set; } = STARTING_GOLD;

    /// <summary>店舗レベル（1始まり）。</summary>
    public int ShopLevel { get; private set; } = 1;

    /// <summary>現在ロード中のシーン名。</summary>
    public string CurrentSceneName { get; private set; } = BOOT_SCENE;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private bool _isTransitioning;

    // ──────────────────────────────────────────────
    // InventoryManager 参照
    // ──────────────────────────────────────────────

    /// <summary>インベントリ管理。GameManager と同じ GameObject にアタッチ。</summary>
    public InventoryManager Inventory { get; private set; }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // Singleton ガード
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 同一 GameObject 上の InventoryManager をキャッシュ
        if (!TryGetComponent(out InventoryManager inventory))
        {
            inventory = gameObject.AddComponent<InventoryManager>();
        }
        Inventory = inventory;

        // シーンロード完了コールバック登録
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// timeScale 安全弁。毎フレーム、遷移中でないのに timeScale が異常なら復帰させる。
    /// </summary>
    private void LateUpdate()
    {
        // シーン遷移中やヒットストップ中は除外する。
        // ヒットストップは JustInputAction が _isHitStopActive フラグで管理するため、
        // ここでは「アクションシーン以外で timeScale != 1」だけを安全弁対象とする。
        if (!_isTransitioning
            && CurrentSceneName != ACTION_SCENE
            && !Mathf.Approximately(Time.timeScale, 1f))
        {
            Debug.LogWarning(
                $"[GameManager] timeScale 異常検知 ({Time.timeScale:F3})。1.0 に復帰します。");
            ForceRestoreTimeScale();
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — フェーズ進行
    // ──────────────────────────────────────────────

    /// <summary>
    /// 次のフェーズへ進行する。Midnight → Morning の場合は日数も加算する。
    /// 必要に応じてシーン遷移も自動実行する。
    /// </summary>
    public void AdvancePhase()
    {
        if (_isTransitioning) return;

        GamePhase nextPhase;

        switch (CurrentPhase)
        {
            case GamePhase.Morning:
                nextPhase = GamePhase.Noon;
                // Morning → Noon: ActionScene へ遷移
                SetPhase(nextPhase);
                LoadSceneAsync(ACTION_SCENE);
                break;

            case GamePhase.Noon:
                nextPhase = GamePhase.Evening;
                // Noon → Evening: ManagementScene へ遷移
                SetPhase(nextPhase);
                LoadSceneAsync(MANAGEMENT_SCENE);
                break;

            case GamePhase.Evening:
                nextPhase = GamePhase.Night;
                // Evening → Night: 同一シーン内遷移（ManagementScene）
                SetPhase(nextPhase);
                break;

            case GamePhase.Night:
                nextPhase = GamePhase.Midnight;
                // Night → Midnight: 同一シーン内遷移（リザルト表示）
                SetPhase(nextPhase);
                break;

            case GamePhase.Midnight:
                nextPhase = GamePhase.Morning;
                // Midnight → Morning: 日数加算し、ManagementScene のまま準備画面へ
                AdvanceDay();
                SetPhase(nextPhase);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — 所持金
    // ──────────────────────────────────────────────

    /// <summary>所持金を加算する（負値で減算可能）。0 未満にはならない。</summary>
    public void AddGold(int amount)
    {
        Gold = Mathf.Max(0, Gold + amount);
        OnGoldChanged?.Invoke(Gold);
    }

    /// <summary>指定額を支払えるか判定する。</summary>
    public bool CanAfford(int cost) => Gold >= cost;

    /// <summary>
    /// 指定額を支払う。残高不足の場合は false を返し、何もしない。
    /// </summary>
    public bool TrySpendGold(int cost)
    {
        if (cost < 0 || Gold < cost) return false;
        AddGold(-cost);
        return true;
    }

    // ──────────────────────────────────────────────
    // 公開 API — 店舗レベル
    // ──────────────────────────────────────────────

    /// <summary>店舗レベルを1上げる。</summary>
    public void LevelUpShop()
    {
        ShopLevel++;
    }

    // ──────────────────────────────────────────────
    // 公開 API — timeScale 安全復帰
    // ──────────────────────────────────────────────

    /// <summary>timeScale と fixedDeltaTime を安全にデフォルトへ復帰させる。</summary>
    public static void ForceRestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = DEFAULT_FIXED_DELTA_TIME;
    }

    // ──────────────────────────────────────────────
    // 公開 API — データリセット（デバッグ / ニューゲーム）
    // ──────────────────────────────────────────────

    /// <summary>進行データを初期状態にリセットする。</summary>
    public void ResetProgress()
    {
        CurrentDay   = STARTING_DAY;
        CurrentPhase = GamePhase.Morning;
        Gold         = STARTING_GOLD;
        ShopLevel    = 1;
        Inventory.ClearAll();
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void AdvanceDay()
    {
        CurrentDay++;
        OnDayAdvanced?.Invoke(CurrentDay);
    }

    private void LoadSceneAsync(string sceneName)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        // timeScale を安全な状態に復帰してからシーンロード
        ForceRestoreTimeScale();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[GameManager] シーン '{sceneName}' のロードに失敗しました。");
            _isTransitioning = false;
            return;
        }
        op.allowSceneActivation = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentSceneName = scene.name;
        _isTransitioning = false;
        OnSceneLoaded?.Invoke(scene.name);
    }
}
```

---

## 3. InventoryManager 設計

### 3.1 設計方針

- GameManager と同一 GameObject 上にアタッチし、DontDestroyOnLoad で永続化
- 素材、レシピ（調理済み料理）、武器、家具の4カテゴリを管理
- Dictionary ベースで O(1) アクセス
- イベント通知による疎結合

### 3.2 InventoryManager コード

```csharp
// ============================================================
// InventoryManager.cs
// GameManager と同一 GameObject にアタッチされ、DontDestroyOnLoad で永続化。
// 素材・料理・武器・家具の在庫を一元管理する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのインベントリ（素材・料理・武器・家具）を管理する。
/// GameManager.Inventory でアクセスする。
/// </summary>
public sealed class InventoryManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const int MAX_STACK_SIZE = 999;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>素材が追加されたとき。引数は (MaterialData, 変化後の個数)。</summary>
    public event Action<MaterialData, int> OnMaterialAdded;

    /// <summary>素材が消費されたとき。引数は (MaterialData, 変化後の個数)。</summary>
    public event Action<MaterialData, int> OnMaterialConsumed;

    /// <summary>料理（調理済み）が追加されたとき。</summary>
    public event Action<CookedDishData> OnDishAdded;

    /// <summary>料理が提供（消費）されたとき。</summary>
    public event Action<CookedDishData> OnDishServed;

    // ──────────────────────────────────────────────
    // データ構造
    // ──────────────────────────────────────────────

    /// <summary>素材 ID → 所持数。</summary>
    private readonly Dictionary<string, int> _materials = new Dictionary<string, int>();

    /// <summary>素材 ID → MaterialData 参照（逆引き用）。</summary>
    private readonly Dictionary<string, MaterialData> _materialDataMap
        = new Dictionary<string, MaterialData>();

    /// <summary>調理済み料理のストック。キューで先入れ先出し。</summary>
    private readonly Queue<CookedDishData> _cookedDishes = new Queue<CookedDishData>();

    /// <summary>所持武器リスト。</summary>
    private readonly List<WeaponData> _weapons = new List<WeaponData>();

    /// <summary>所持家具リスト。</summary>
    private readonly List<FurnitureData> _furniture = new List<FurnitureData>();

    // ──────────────────────────────────────────────
    // 公開 API — 素材
    // ──────────────────────────────────────────────

    /// <summary>素材を追加する。</summary>
    public void AddMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return;

        string id = data.Id;

        if (!_materialDataMap.ContainsKey(id))
        {
            _materialDataMap[id] = data;
        }

        if (!_materials.ContainsKey(id))
        {
            _materials[id] = 0;
        }

        _materials[id] = Mathf.Min(_materials[id] + amount, MAX_STACK_SIZE);
        OnMaterialAdded?.Invoke(data, _materials[id]);
    }

    /// <summary>素材を消費する。不足時は false を返し何もしない。</summary>
    public bool TryConsumeMaterial(MaterialData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;

        string id = data.Id;

        if (!_materials.TryGetValue(id, out int current) || current < amount)
        {
            return false;
        }

        _materials[id] = current - amount;

        if (_materials[id] <= 0)
        {
            _materials.Remove(id);
            _materialDataMap.Remove(id);
        }

        OnMaterialConsumed?.Invoke(data, _materials.GetValueOrDefault(id, 0));
        return true;
    }

    /// <summary>レシピの必要素材をすべて持っているか判定する。</summary>
    public bool HasMaterialsForRecipe(RecipeData recipe)
    {
        if (recipe == null) return false;

        foreach (RecipeData.RequiredMaterial req in recipe.RequiredMaterials)
        {
            if (!_materials.TryGetValue(req.Material.Id, out int owned) || owned < req.Amount)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>レシピの必要素材をまとめて消費する。不足なら false（何も消費しない）。</summary>
    public bool TryConsumeMaterialsForRecipe(RecipeData recipe)
    {
        if (!HasMaterialsForRecipe(recipe)) return false;

        foreach (RecipeData.RequiredMaterial req in recipe.RequiredMaterials)
        {
            TryConsumeMaterial(req.Material, req.Amount);
        }
        return true;
    }

    /// <summary>指定素材の所持数を返す。</summary>
    public int GetMaterialCount(MaterialData data)
    {
        if (data == null) return 0;
        return _materials.GetValueOrDefault(data.Id, 0);
    }

    /// <summary>所持中の全素材を返す（読み取り専用）。</summary>
    public IReadOnlyDictionary<string, int> GetAllMaterials() => _materials;

    // ──────────────────────────────────────────────
    // 公開 API — 調理済み料理
    // ──────────────────────────────────────────────

    /// <summary>調理済み料理をストックに追加する。</summary>
    public void AddCookedDish(CookedDishData dish)
    {
        _cookedDishes.Enqueue(dish);
        OnDishAdded?.Invoke(dish);
    }

    /// <summary>ストックから料理を1つ取り出す（FIFO）。空なら null。</summary>
    public CookedDishData ServeDish()
    {
        if (_cookedDishes.Count == 0) return null;
        CookedDishData dish = _cookedDishes.Dequeue();
        OnDishServed?.Invoke(dish);
        return dish;
    }

    /// <summary>指定レシピの調理済み料理があるか確認する。</summary>
    public bool HasCookedDish(RecipeData recipe)
    {
        foreach (CookedDishData dish in _cookedDishes)
        {
            if (dish.OriginalRecipe == recipe) return true;
        }
        return false;
    }

    /// <summary>調理済み料理のストック数。</summary>
    public int CookedDishCount => _cookedDishes.Count;

    // ──────────────────────────────────────────────
    // 公開 API — 武器
    // ──────────────────────────────────────────────

    /// <summary>武器を追加する。</summary>
    public void AddWeapon(WeaponData weapon)
    {
        if (weapon != null) _weapons.Add(weapon);
    }

    /// <summary>所持武器リスト（読み取り専用）。</summary>
    public IReadOnlyList<WeaponData> Weapons => _weapons;

    // ──────────────────────────────────────────────
    // 公開 API — 家具
    // ──────────────────────────────────────────────

    /// <summary>家具を追加する。</summary>
    public void AddFurniture(FurnitureData item)
    {
        if (item != null) _furniture.Add(item);
    }

    /// <summary>所持家具リスト（読み取り専用）。</summary>
    public IReadOnlyList<FurnitureData> Furniture => _furniture;

    // ──────────────────────────────────────────────
    // 公開 API — 全クリア
    // ──────────────────────────────────────────────

    /// <summary>全インベントリを空にする。</summary>
    public void ClearAll()
    {
        _materials.Clear();
        _materialDataMap.Clear();
        _cookedDishes.Clear();
        _weapons.Clear();
        _furniture.Clear();
    }
}
```

### 3.3 補助データ構造

```csharp
// ============================================================
// CookedDishData.cs
// 調理済み料理の情報。CookingMinigame の結果を格納する。
// ============================================================
using System;

/// <summary>調理済み料理データ。インベントリに格納される。</summary>
[Serializable]
public sealed class CookedDishData
{
    /// <summary>元のレシピ。</summary>
    public RecipeData OriginalRecipe { get; }

    /// <summary>調理ランク。</summary>
    public CookingRank Rank { get; }

    /// <summary>最終売値。</summary>
    public int FinalPrice { get; }

    public CookedDishData(RecipeData recipe, CookingRank rank, int finalPrice)
    {
        OriginalRecipe = recipe;
        Rank           = rank;
        FinalPrice     = finalPrice;
    }
}
```

```csharp
// ============================================================
// MaterialData.cs
// 素材データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>素材データ。敵ドロップ、採集、購入で取得する。</summary>
[CreateAssetMenu(fileName = "MAT_New", menuName = "DevilsDiner/MaterialData")]
public sealed class MaterialData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _materialName;
    [SerializeField] private Sprite _icon;
    [SerializeField, Range(1, 5)] private int _rarity = 1;
    [SerializeField] private float _dropRate = 1f;
    [SerializeField] private int _basePrice = 10;
    [SerializeField, Tooltip("調理ゲージ速度倍率")]
    private float _gaugeSpeedMultiplier = 1f;

    public string Id                   => _id;
    public string MaterialName         => _materialName;
    public Sprite Icon                 => _icon;
    public int    Rarity               => _rarity;
    public float  DropRate             => _dropRate;
    public int    BasePrice            => _basePrice;
    public float  GaugeSpeedMultiplier => _gaugeSpeedMultiplier;
}
```

```csharp
// ============================================================
// RecipeData.cs
// レシピデータの ScriptableObject。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>レシピデータ。必要素材と売値を定義する。</summary>
[CreateAssetMenu(fileName = "RCP_New", menuName = "DevilsDiner/RecipeData")]
public sealed class RecipeData : ScriptableObject
{
    [Serializable]
    public struct RequiredMaterial
    {
        public MaterialData Material;
        public int Amount;
    }

    [SerializeField] private string _id;
    [SerializeField] private string _recipeName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private List<RequiredMaterial> _requiredMaterials;
    [SerializeField] private int _basePrice = 100;
    [SerializeField] private float _perfectMultiplier = 1.5f;
    [SerializeField] private GameObject _modelPrefab;

    public string                      Id                 => _id;
    public string                      RecipeName         => _recipeName;
    public Sprite                      Icon               => _icon;
    public IReadOnlyList<RequiredMaterial> RequiredMaterials => _requiredMaterials;
    public int                         BasePrice          => _basePrice;
    public float                       PerfectMultiplier  => _perfectMultiplier;
    public GameObject                  ModelPrefab        => _modelPrefab;
}
```

```csharp
// ============================================================
// WeaponData.cs
// 武器データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>武器データ。攻撃力やジャスト入力ボーナスを定義する。</summary>
[CreateAssetMenu(fileName = "WPN_New", menuName = "DevilsDiner/WeaponData")]
public sealed class WeaponData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _weaponName;
    [SerializeField] private int _baseDamage = 100;
    [SerializeField] private int _basePartBreakValue = 10;
    [SerializeField] private int _justInputFrameBonus;
    [SerializeField] private AnimatorOverrideController _animatorOverride;

    public string                    Id                    => _id;
    public string                    WeaponName            => _weaponName;
    public int                       BaseDamage            => _baseDamage;
    public int                       BasePartBreakValue    => _basePartBreakValue;
    public int                       JustInputFrameBonus   => _justInputFrameBonus;
    public AnimatorOverrideController AnimatorOverride      => _animatorOverride;
}
```

```csharp
// ============================================================
// FurnitureData.cs
// 家具データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>家具データ。居心地度ボーナスを定義する。</summary>
[CreateAssetMenu(fileName = "FRN_New", menuName = "DevilsDiner/FurnitureData")]
public sealed class FurnitureData : ScriptableObject
{
    public enum FurnitureType
    {
        Table,
        Chair,
        Decoration,
        Lighting,
        Kitchen
    }

    [SerializeField] private string _id;
    [SerializeField] private string _furnitureName;
    [SerializeField] private FurnitureType _type;
    [SerializeField] private int _price = 100;
    [SerializeField] private float _comfortBonus;
    [SerializeField] private GameObject _prefab;

    public string        Id            => _id;
    public string        FurnitureName => _furnitureName;
    public FurnitureType Type          => _type;
    public int           Price         => _price;
    public float         ComfortBonus  => _comfortBonus;
    public GameObject    Prefab        => _prefab;
}
```

```csharp
// ============================================================
// EnemyData.cs
// 敵データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>敵データ。HP、攻撃力、ドロップ情報を定義する。</summary>
[CreateAssetMenu(fileName = "ENM_New", menuName = "DevilsDiner/EnemyData")]
public sealed class EnemyData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _enemyName;
    [SerializeField] private int _maxHP = 1000;
    [SerializeField] private int _baseAttack = 50;

    [Header("ドロップ設定")]
    [SerializeField] private MaterialData _dropItemNormal;
    [SerializeField] private MaterialData _dropItemJust;
    [SerializeField, Range(0f, 1f)] private float _dropRateNormal = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _dropRateJust = 1.0f;

    public string       Id              => _id;
    public string       EnemyName       => _enemyName;
    public int          MaxHP           => _maxHP;
    public int          BaseAttack      => _baseAttack;
    public MaterialData DropItemNormal  => _dropItemNormal;
    public MaterialData DropItemJust    => _dropItemJust;
    public float        DropRateNormal  => _dropRateNormal;
    public float        DropRateJust    => _dropRateJust;
}
```

```csharp
// ============================================================
// CookingRank.cs
// 調理ランク列挙型。action / sim 双方から参照される共通型。
// ============================================================

/// <summary>調理ミニゲームの結果ランク。</summary>
public enum CookingRank
{
    Perfect,
    Good,
    Miss
}
```

```csharp
// ============================================================
// DropResolver.cs
// 敵撃破時のドロップ判定ロジック（静的ユーティリティ）。
// ============================================================
using UnityEngine;

/// <summary>
/// 敵撃破時の素材ドロップを判定する静的クラス。
/// EnemyController から呼び出される。
/// </summary>
public static class DropResolver
{
    /// <summary>
    /// ドロップ判定を行い、結果の素材を InventoryManager に直接追加する。
    /// </summary>
    /// <param name="enemyData">撃破した敵のデータ。</param>
    /// <param name="wasJustInput">トドメがジャスト入力だったか。</param>
    public static void ResolveDrop(EnemyData enemyData, bool wasJustInput)
    {
        if (enemyData == null) return;

        MaterialData dropItem;
        float dropRate;

        if (wasJustInput && enemyData.DropItemJust != null)
        {
            dropItem = enemyData.DropItemJust;
            dropRate = enemyData.DropRateJust;
        }
        else
        {
            dropItem = enemyData.DropItemNormal;
            dropRate = enemyData.DropRateNormal;
        }

        if (dropItem == null) return;

        if (Random.value <= dropRate)
        {
            GameManager.Instance.Inventory.AddMaterial(dropItem);
        }
    }
}
```

---

## 4. シーン遷移フロー図

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ゲーム全体フロー                                  │
└─────────────────────────────────────────────────────────────────────────┘

 [アプリ起動]
      │
      ▼
 ╔═══════════════╗
 ║   BootScene   ║  GameManager + InventoryManager 生成
 ║               ║  DontDestroyOnLoad 登録
 ║  (初期化専用)  ║  初期データロード
 ╚═══════╤═══════╝
         │ 自動遷移
         ▼
 ┌───────────────────────────────────────────────────────────────────┐
 │                     ◆ デイサイクル ループ ◆                        │
 │                                                                   │
 │  ┌─────────────┐                                                  │
 │  │   Morning    │ フェーズ: 出撃準備                                │
 │  │ (Management  │ ・装備選択、素材確認                              │
 │  │   Scene)     │ ・出撃ボタンで AdvancePhase()                    │
 │  └──────┬──────┘                                                  │
 │         │ LoadSceneAsync("ActionScene")                           │
 │         ▼                                                         │
 │  ╔═════════════╗                                                  │
 │  ║ ActionScene ║ フェーズ: Noon（狩猟アクション）                    │
 │  ║             ║                                                  │
 │  ║ ・TPS移動    ║ PlayerController                                 │
 │  ║ ・大剣攻撃   ║ JustInputAction                                  │
 │  ║ ・ヒットストップ║ WeaponColliderHandler                          │
 │  ║ ・敵撃破     ║ EnemyController → DropResolver                   │
 │  ║ ・素材獲得   ║   → InventoryManager.AddMaterial()               │
 │  ║             ║                                                  │
 │  ║ 帰還条件達成 ║ AdvancePhase()                                   │
 │  ╚══════╤══════╝                                                  │
 │         │ LoadSceneAsync("ManagementScene")                       │
 │         ▼                                                         │
 │  ╔════════════════════╗                                           │
 │  ║ ManagementScene    ║ フェーズ: Evening（調理・開店準備）           │
 │  ║                    ║                                           │
 │  ║ ・CookingMinigame  ║ 素材消費 → CookedDishData 生成             │
 │  ║ ・メニュー設定      ║   → InventoryManager.AddCookedDish()      │
 │  ║                    ║                                           │
 │  ║ 準備完了           ║ AdvancePhase()                             │
 │  ╠════════════════════╣                                           │
 │  ║                    ║ フェーズ: Night（経営・鑑賞）                │
 │  ║ ・CustomerSpawner  ║ 客NPCスポーン                              │
 │  ║ ・CustomerAI       ║ 着席→注文→食事→支払い→退店                  │
 │  ║ ・SeatManager      ║ 席管理                                    │
 │  ║ ・DinerManager     ║ OnPaymentMade → GameManager.AddGold()     │
 │  ║                    ║                                           │
 │  ║ 閉店条件達成        ║ AdvancePhase()                            │
 │  ╠════════════════════╣                                           │
 │  ║                    ║ フェーズ: Midnight（リザルト）               │
 │  ║ ・本日の売上表示    ║                                            │
 │  ║ ・獲得素材まとめ    ║                                            │
 │  ║ ・店舗レベルチェック ║                                            │
 │  ║                    ║                                           │
 │  ║ 確認ボタン         ║ AdvancePhase() → 日数加算 → Morning へ     │
 │  ╚════════════════════╝                                           │
 │                                                                   │
 │  ※ Morning は ManagementScene 上で表示                             │
 │  ※ Noon 開始時に ActionScene をロード                               │
 │  ※ Evening 開始時に ManagementScene をロード                        │
 └───────────────────────────────────────────────────────────────────┘
```

---

## 5. 全クラス依存関係・モジュール構成図

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      モジュール構成図                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ◆ Core Module (DontDestroyOnLoad / シーン横断)                          │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │  GameManager (Singleton)                                    │        │
│  │    ├── GamePhase (enum)                                     │        │
│  │    ├── CurrentDay, Gold, ShopLevel                          │        │
│  │    ├── OnPhaseChanged, OnDayAdvanced, OnGoldChanged         │        │
│  │    ├── AdvancePhase(), AddGold(), ForceRestoreTimeScale()   │        │
│  │    │                                                        │        │
│  │    └── InventoryManager (同一GameObject)                     │        │
│  │          ├── Materials (Dictionary<string, int>)             │        │
│  │          ├── CookedDishes (Queue<CookedDishData>)           │        │
│  │          ├── Weapons (List<WeaponData>)                     │        │
│  │          ├── Furniture (List<FurnitureData>)                 │        │
│  │          └── OnMaterialAdded, OnDishAdded 等                 │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                         │
│  ◆ Data Module (ScriptableObject / 全シーン共通参照)                      │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │  MaterialData (SO)    素材定義                               │        │
│  │  RecipeData (SO)      レシピ定義                             │        │
│  │  WeaponData (SO)      武器定義                               │        │
│  │  FurnitureData (SO)   家具定義                               │        │
│  │  EnemyData (SO)       敵定義                                │        │
│  │  JustInputConfig (SO) ジャスト入力パラメータ                   │        │
│  │  CookingConfig (SO)   調理パラメータ                          │        │
│  │  CookingRank (enum)   調理ランク                             │        │
│  │  CookedDishData       調理済み料理データ (class)              │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                         │
│  ◆ Action Module (ActionScene)                                          │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │  PlayerController                                           │        │
│  │    ├── PlayerState (enum)                                   │        │
│  │    ├── [依存] CharacterController, Animator                 │        │
│  │    ├── [依存] InputSystem_Actions                            │        │
│  │    └── [参照] WeaponData                                    │        │
│  │                                                             │        │
│  │  JustInputAction                                            │        │
│  │    ├── AttackPhase (enum)                                   │        │
│  │    ├── [依存] JustInputConfig (SO)                          │        │
│  │    ├── [通知先] PlayerController.ForceAttackPhase()          │        │
│  │    └── [通知先] CameraShakeHandler                          │        │
│  │                                                             │        │
│  │  WeaponColliderHandler                                      │        │
│  │    ├── [依存] PlayerController (AttackPhase 参照)            │        │
│  │    └── [出力] IDamageable.TakeDamage(HitResult)             │        │
│  │                                                             │        │
│  │  CameraShakeHandler                                         │        │
│  │    └── unscaledDeltaTime ベースのシェイク                     │        │
│  │                                                             │        │
│  │  EnemyController : IDamageable                              │        │
│  │    ├── [参照] EnemyData (SO)                                │        │
│  │    └── [呼出] DropResolver.ResolveDrop()                    │        │
│  │                                                             │        │
│  │  DropResolver (static)                                      │        │
│  │    └── [呼出] InventoryManager.AddMaterial()                │        │
│  │                                                             │        │
│  │  DamageInfo (struct)                                        │        │
│  │  HitResult (struct)                                         │        │
│  │  IDamageable (interface)                                    │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                         │
│  ◆ Management Module (ManagementScene)                                  │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │  DinerManager (経営シーン統括)                                │        │
│  │    ├── [リッスン] CustomerAI.OnPaymentMade                   │        │
│  │    ├── [呼出] GameManager.Instance.AddGold()                │        │
│  │    └── [管理] 居心地度 (ComfortScore) 集計                   │        │
│  │                                                             │        │
│  │  CookingMinigame                                            │        │
│  │    ├── [参照] RecipeData, CookingConfig (SO)                │        │
│  │    ├── [呼出] InventoryManager.TryConsumeMaterialsForRecipe│        │
│  │    ├── [出力] CookingResult → CookedDishData                │        │
│  │    └── [呼出] InventoryManager.AddCookedDish()              │        │
│  │                                                             │        │
│  │  OrderQueue (新設)                                           │        │
│  │    ├── [入力] CustomerAI.OnCustomerOrdered                  │        │
│  │    └── [出力] 調理対象レシピの提供                             │        │
│  │                                                             │        │
│  │  CustomerSpawner                                            │        │
│  │    ├── [参照] DinerManager.ComfortScore                     │        │
│  │    └── [生成] CustomerAI (Instantiate / ObjectPool)         │        │
│  │                                                             │        │
│  │  CustomerAI                                                 │        │
│  │    ├── CustomerState (enum)                                 │        │
│  │    ├── [依存] NavMeshAgent                                  │        │
│  │    ├── [依存] SeatManager                                   │        │
│  │    ├── [イベント] OnCustomerOrdered, OnPaymentMade          │        │
│  │    └── [入力] CookedDishData (提供される料理)                 │        │
│  │                                                             │        │
│  │  SeatManager                                                │        │
│  │    └── SeatNode (MonoBehaviour) 配列                         │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 依存関係の方向ルール

```
  Core Module ← Action Module
  Core Module ← Management Module
  Data Module ← Core Module
  Data Module ← Action Module
  Data Module ← Management Module

  ※ Action Module と Management Module は互いに直接依存しない。
     データの受け渡しは必ず Core Module (GameManager / InventoryManager) を経由する。
```

---

## 6. フェーズ1統合アーキテクチャ図

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    フェーズ1 統合アーキテクチャ                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─── DontDestroyOnLoad 永続オブジェクト ──────────────────────────┐    │
│  │                                                                 │    │
│  │   [GameManager GameObject]                                      │    │
│  │     ├─ GameManager (Singleton)                                  │    │
│  │     └─ InventoryManager                                         │    │
│  │                                                                 │    │
│  │   [EventSystem GameObject]  ← Input System 用                   │    │
│  │                                                                 │    │
│  │   [AudioManager GameObject] ← BGM再生用（将来実装）              │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│       │                    │                    │                        │
│       │ 参照               │ 参照               │ 参照                  │
│       ▼                    ▼                    ▼                        │
│  ╔══════════╗     ╔══════════════╗     ╔═════════════════╗              │
│  ║BootScene ║     ║ ActionScene  ║     ║ManagementScene  ║              │
│  ║          ║     ║              ║     ║                 ║              │
│  ║ 初期化   ║     ║ Player       ║     ║ DinerManager    ║              │
│  ║ のみ     ║     ║  Controller  ║     ║ CookingMinigame ║              │
│  ║          ║     ║ JustInput    ║     ║ OrderQueue      ║              │
│  ║          ║     ║  Action      ║     ║ CustomerSpawner ║              │
│  ║          ║     ║ WeaponCollider║    ║ CustomerAI      ║              │
│  ║          ║     ║  Handler     ║     ║ SeatManager     ║              │
│  ║          ║     ║ CameraShake  ║     ║ SeatNode        ║              │
│  ║          ║     ║  Handler     ║     ║                 ║              │
│  ║          ║     ║ EnemyController║   ║                 ║              │
│  ║          ║     ║              ║     ║                 ║              │
│  ╚══════════╝     ╚══════════════╝     ╚═════════════════╝              │
│                                                                         │
│  ┌─── ScriptableObject アセット (シーン非依存) ────────────────────┐    │
│  │                                                                 │    │
│  │  Assets/Data/Materials/   MAT_xxx.asset                         │    │
│  │  Assets/Data/Recipes/     RCP_xxx.asset                         │    │
│  │  Assets/Data/Weapons/     WPN_xxx.asset                         │    │
│  │  Assets/Data/Furniture/   FRN_xxx.asset                         │    │
│  │  Assets/Data/Enemies/     ENM_xxx.asset                         │    │
│  │  Assets/Data/Config/      JustInputConfig.asset                 │    │
│  │  Assets/Data/Config/      CookingConfig.asset                   │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. データフロー図

### 7.1 全体データフロー（アクション → 経営 → 決済）

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     メインデータフロー                                    │
└─────────────────────────────────────────────────────────────────────────┘

 ActionScene                        Core (永続)                ManagementScene
 ──────────                         ──────────                 ──────────────

 ① 敵との戦闘
 PlayerController
   → WeaponColliderHandler
     → OnTriggerEnter("EnemyHurtbox")
       → DamageInfo 生成
         → IDamageable.TakeDamage(HitResult)
           │
           ▼
 ② 敵撃破判定
 EnemyController
   (HP <= 0)
   → DropResolver.ResolveDrop(
       enemyData,
       hitResult.IsJustInput
     )
     │
     │  ③ ドロップ素材をインベントリに追加
     └──────────────────────────►  InventoryManager
                                     .AddMaterial(materialData)
                                      │
                                      │  OnMaterialAdded イベント
                                      │  (UI更新用)
                                      │
 ④ 帰還（シーン遷移）                  │
 GameManager.AdvancePhase()            │
   → LoadSceneAsync                    │
     ("ManagementScene")               │
                                      │
                                      ▼
                                   InventoryManager
                                   (データ保持中)
                                      │
                                      │ ⑤ レシピ選択 & 素材チェック
                                      │◄─────────────────── CookingMinigame
                                      │  HasMaterialsForRecipe()   .StartCooking(recipe)
                                      │                            │
                                      │ ⑥ 素材消費                  │
                                      │◄─────────────────── TryConsumeMaterialsForRecipe()
                                      │                            │
                                      │                            ▼
                                      │                    ⑦ ゲージミニゲーム
                                      │                       PingPong判定
                                      │                       → CookingRank 決定
                                      │                       → CookedDishData 生成
                                      │                            │
                                      │ ⑧ 料理をストックに追加       │
                                      │◄─────────────────── AddCookedDish()
                                      │                            │
                                      │                    ⑨ 客NPC 来店
                                      │                    CustomerSpawner
                                      │                       → CustomerAI 生成
                                      │                            │
                                      │                    ⑩ 着席 & 注文
                                      │                    CustomerAI
                                      │                       → SeatManager
                                      │                         .TryReserveSeat()
                                      │                       → OnCustomerOrdered
                                      │                            │
                                      │                    ⑪ オーダー受付
                                      │                    OrderQueue
                                      │                       .Enqueue(order)
                                      │                       → 料理提供トリガー
                                      │                            │
                                      │ ⑫ 料理を取り出し             │
                                      │  ServeDish()         ◄─────┘
                                      │  → CookedDishData
                                      │                            │
                                      │                    ⑬ 食事 & 支払い
                                      │                    CustomerAI
                                      │                       → Eating → Paying
                                      │                       → OnPaymentMade
                                      │                            │
                                      │                    ⑭ 売上反映
                                      │                    DinerManager
                                      │                       .HandlePayment()
                                      │                            │
 ⑮ 所持金更新                          │                            │
 GameManager                    ◄──────┘◄───────────────────────────┘
   .AddGold(amount)                    AddGold()
    │
    │ OnGoldChanged イベント
    ▼
 (UI更新)
```

### 7.2 ジャスト入力フロー（詳細）

```
 プレイヤー操作                   システム処理
 ──────────                      ──────────

 [攻撃ボタン]
   │
   ▼
 PlayerController
   → State = Attack
   → Animator.SetTrigger("Attack")
   │
   │ AnimEvent_ActiveStart()
   ▼
 AttackPhase = Active
   │
   │ WeaponCollider 有効化
   ▼
 WeaponColliderHandler.OnTriggerEnter()
   → DamageInfo 生成
   → JustInputAction.NotifyWeaponHit(damageInfo)
   │
   ▼
 JustInputAction.StartHitStop()
   → Time.timeScale = 0.05f (JustInputConfig)
   → Time.fixedDeltaTime = 0.02 * 0.05
   → ヒットストップ演出開始
   │
   │ Update() ループ (unscaledDeltaTime)
   │ → IsShootButtonPressed() ポーリング
   │
   ├─── [射撃ボタン押下 (justWindow内)] ──→ OnJustInputSuccessInternal()
   │                                         → DamageMultiplier = 2.5x
   │                                         → PartBreakBonus = +50
   │                                         → シリンダー回転Anim
   │                                         → 青い炎パーティクル
   │                                         → カメラシェイク
   │                                         → コントローラー振動
   │                                         → timeScale 復帰
   │
   └─── [時間切れ] ──────────────────────→ OnHitStopExpired()
                                            → DamageMultiplier = 1.0x
                                            → 通常ダメージ確定
                                            → timeScale 復帰
```

---

## 8. パフォーマンスガイドライン（統合版）

### 8.1 Time.timeScale 関連

| ルール | 詳細 |
|--------|------|
| fixedDeltaTime 復帰 | timeScale 復帰時に `Time.fixedDeltaTime = 0.02f` を明示的に再設定する |
| 安全弁 | GameManager.LateUpdate() で非アクションシーンでの timeScale 異常を検知・復帰 |
| UI アニメーション | ヒットストップ中のUI演出は `AnimatorUpdateMode.UnscaledTime` を使用 |
| Audio | ヒットストップ中のSEは `AudioSource.PlayOneShot` で再生（Pitch影響に注意） |
| ParticleSystem | ヒットストップ演出用パーティクルは `useUnscaledTime = true` に設定 |
| Input System | **Dynamic Update モード**を使用し、timeScale の影響を受けないようにする |

### 8.2 NavMesh / AI 関連

| ルール | 詳細 |
|--------|------|
| 同時エージェント数 | 最大30体。CustomerSpawner で `MAX_CUSTOMERS` を定数管理 |
| SetDestination 呼び出し | ステート遷移時に1回のみ。毎フレーム呼び出し禁止 |
| NavMesh Bake | ManagementScene ロード時に1回。Runtime NavMesh Bake は使用しない |
| 動的障害物 | `NavMeshObstacle` (Carve モード) を使用。Bake し直さない |
| 経路キャッシュ | 同じ目的地への繰り返し移動はキャッシュ（将来拡張） |

### 8.3 GC アロケーション対策

| 対象 | 対策 |
|------|------|
| DamageInfo, HitResult | `struct` で定義済み（ヒープ割り当て回避） |
| CookingResult | `struct` であることを保証 |
| SeatManager シャッフル | `static List<SeatNode>` を共有バッファとして再利用。毎回 new しない |
| CustomerAI メニュー選択 | 同様に共有バッファパターンで `List<RecipeData>` を再利用 |
| 文字列結合 | UI テキスト更新時は `StringBuilder` または `$"{value}"` のキャッシュ |
| イベント引数 | 値型（int, enum, struct）を優先。class 引数が必要な場合はプール検討 |

### 8.4 オブジェクトプール

| 対象 | 方針 |
|------|------|
| CustomerAI | `ObjectPool<CustomerAI>` で管理。Destroy の代わりに `Release()` |
| ParticleSystem (ヒット演出) | 事前プールで 10～20 個確保 |
| ダメージ数値UI | フローティングテキスト用にプール管理 |
| 弾丸エフェクト（将来） | 将来の射撃実装に備えプール設計を準備 |

### 8.5 描画・物理最適化

| ルール | 詳細 |
|--------|------|
| Physics Layer | 以下のレイヤーを分離: Player, Enemy, PlayerWeapon, EnemyAttack, Environment, UI |
| Layer Collision Matrix | 必要な組み合わせのみ有効化（例: PlayerWeapon ↔ Enemy のみ） |
| Rigidbody | 静的オブジェクトには Rigidbody を付けない（Static Collider として扱う） |
| LOD | フェーズ1では不要。フェーズ2以降で敵の多い場面に導入検討 |

### 8.6 デバッグ・プロファイリング

| ルール | 詳細 |
|--------|------|
| Debug.Log | `#if UNITY_EDITOR` または `[Conditional("UNITY_EDITOR")]` で囲む |
| Profiler | Weekly でプロファイラチェック。フレームバジェット: 16.6ms (60fps) |
| Memory | Managed Heap の増加監視。GC Spike が 2ms を超えたら対策必須 |

---

## 9. フェーズ1実装優先度とタスク依存関係

### 9.1 タスク依存関係図

```
 PG-1 環境構築 (2日)
   │
   ├──────────────┬──────────────────────────────────────┐
   ▼              ▼                                      ▼
 PG-8           PG-2                              Data Module
 シーン遷移      TPS カメラ &                       ScriptableObject
 & データ       プレイヤー移動                       定義
 受け渡し (2日)  (3日)                              (PG-1に含む)
   │              │
   │              ▼
   │            PG-3
   │            大剣の攻撃
   │            モーション制御
   │            (3日)
   │              │
   │              ▼
   │            PG-4 【最重要】
   │            ヒットストップ &
   │            ジャスト入力ロジック
   │            (5日)
   │              │
   │              ├──────────────┐
   │              ▼              ▼
   │            PG-5           EnemyController
   │            ダメージ計算    (最低限の実装)
   │            処理 (3日)     (PG-5と並行, 2日)
   │              │              │
   │              └──────┬───────┘
   │                     ▼
   │                  DropResolver
   │                  (ドロップ統合, 1日)
   │                     │
   ├─────────────────────┘
   ▼
 PG-6
 調理ミニゲーム
 (3日)
   │
   ▼
 PG-7
 客NPC AI ステート
 (4日)
   │
   ▼
 統合テスト &
 デバッグ (3日)
```

### 9.2 実装スケジュール（推奨）

| 週 | 日数 | タスク | 担当 | 依存 | 成果物 |
|----|------|--------|------|------|--------|
| W1 | D1-D2 | **PG-1**: 環境構築 + SO定義 + BootScene | core | なし | GameManager, InventoryManager, 全SO定義, BootScene |
| W1 | D1-D2 | **PG-8**: シーン遷移 + データ受け渡し | core | PG-1 (並行) | シーン遷移フロー動作確認 |
| W1-W2 | D2-D4 | **PG-2**: TPSカメラ + プレイヤー移動 | action | PG-1 | PlayerController (移動部分) |
| W2 | D4-D6 | **PG-3**: 大剣攻撃モーション | action | PG-2 | PlayerController (攻撃部分), AttackPhase |
| W2-W3 | D6-D10 | **PG-4**: ヒットストップ & ジャスト入力 | action | PG-3 | JustInputAction, WeaponColliderHandler, CameraShakeHandler |
| W3 | D9-D11 | **PG-5**: ダメージ計算 + 敵最低限実装 | action | PG-4 | DamageInfo, HitResult, IDamageable, EnemyController, DropResolver |
| W3-W4 | D10-D12 | **PG-6**: 調理ミニゲーム | sim | PG-1, PG-8 | CookingMinigame, OrderQueue |
| W4-W5 | D12-D15 | **PG-7**: 客NPC AI | sim | PG-6 | CustomerAI, CustomerSpawner, SeatManager, DinerManager |
| W5 | D15-D17 | **統合テスト** | all | 全タスク | フルループ動作確認 |

### 9.3 マイルストーン定義

| マイルストーン | 条件 | 目標日 |
|---------------|------|--------|
| **MS-1**: コア基盤完成 | GameManager + InventoryManager + シーン遷移が動作 | D2 |
| **MS-2**: アクション基本動作 | プレイヤーが移動・攻撃できる | D6 |
| **MS-3**: ジャスト入力完成 | ヒットストップ → ジャスト入力 → ダメージ倍率が動作 | D10 |
| **MS-4**: 戦闘→素材ループ | 敵撃破 → 素材ドロップ → インベントリ追加が動作 | D11 |
| **MS-5**: 調理ループ | 素材消費 → ミニゲーム → 料理ストックが動作 | D12 |
| **MS-6**: 経営ループ | 客来店 → 注文 → 料理提供 → 支払い → 所持金更新が動作 | D15 |
| **MS-7**: フルループ | Morning → Noon → Evening → Night → Midnight → Morning が通る | D17 |

### 9.4 リスク項目と対策

| リスク | 影響度 | 発生確率 | 対策 |
|--------|--------|----------|------|
| ジャスト入力の手触り調整に時間超過 | 高 | 高 | JustInputConfig をSOにしてイテレーション高速化。最悪の場合、ダメージ倍率のみ先行実装し演出は後回し |
| NavMesh と CustomerAI の統合不具合 | 中 | 中 | 最初に2体で動作確認。段階的にスポーン数を増やす |
| シーン遷移時のデータ消失 | 高 | 低 | DontDestroyOnLoad + 遷移前後のデータ整合性テストを MS-1 で実施 |
| timeScale 復帰漏れ | 高 | 中 | GameManager 安全弁 + JustInputAction.OnDisable での強制復帰 |
| Input System の timeScale 影響 | 中 | 中 | Dynamic Update モード設定を PG-1 で確定。ポーリング方式をフォールバックとして維持 |

---

## 付録 A: ファイル配置規則

```
Assets/
├── Scenes/
│   ├── BootScene.unity
│   ├── ActionScene.unity
│   └── ManagementScene.unity
│
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── InventoryManager.cs
│   │   └── DropResolver.cs
│   │
│   ├── Data/
│   │   ├── MaterialData.cs
│   │   ├── RecipeData.cs
│   │   ├── WeaponData.cs
│   │   ├── FurnitureData.cs
│   │   ├── EnemyData.cs
│   │   ├── CookedDishData.cs
│   │   ├── CookingRank.cs
│   │   ├── JustInputConfig.cs
│   │   └── CookingConfig.cs
│   │
│   ├── Action/
│   │   ├── PlayerController.cs
│   │   ├── PlayerState.cs
│   │   ├── AttackPhase.cs
│   │   ├── JustInputAction.cs
│   │   ├── WeaponColliderHandler.cs
│   │   ├── CameraShakeHandler.cs
│   │   ├── EnemyController.cs
│   │   ├── DamageInfo.cs
│   │   ├── HitResult.cs
│   │   └── IDamageable.cs
│   │
│   ├── Management/
│   │   ├── DinerManager.cs
│   │   ├── CookingMinigame.cs
│   │   ├── OrderQueue.cs
│   │   ├── CustomerAI.cs
│   │   ├── CustomerSpawner.cs
│   │   ├── SeatManager.cs
│   │   └── SeatNode.cs
│   │
│   └── Editor/
│       └── (エディター拡張スクリプト)
│
├── Data/
│   ├── Materials/     (MaterialData SO アセット)
│   ├── Recipes/       (RecipeData SO アセット)
│   ├── Weapons/       (WeaponData SO アセット)
│   ├── Furniture/     (FurnitureData SO アセット)
│   ├── Enemies/       (EnemyData SO アセット)
│   └── Config/        (JustInputConfig, CookingConfig SO アセット)
│
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Customers/
│   ├── Furniture/
│   └── Effects/
│
└── (その他: Materials, Textures, Audio, Animations, UI)
```

---

## 付録 B: イベントシステム統一規則

### 規則

1. **シーン内通信**: `System.Action<T>` を使用する。UnityEvent は Inspector からの設定が必要な場合のみ。
2. **シーン横断通信**: `GameManager` のイベント (`OnPhaseChanged`, `OnGoldChanged` 等) を経由する。
3. **イベントの命名規則**: `On` + 動詞の過去分詞 + 名詞（例: `OnPaymentMade`, `OnMaterialAdded`）。
4. **購読解除**: `OnDestroy()` または `OnDisable()` で必ずイベント購読を解除する。メモリリーク防止。

### イベント一覧

| イベント名 | 定義元 | 型 | 用途 |
|-----------|--------|-----|------|
| `OnPhaseChanged` | GameManager | `Action<GamePhase>` | フェーズ変更通知 |
| `OnDayAdvanced` | GameManager | `Action<int>` | 日数進行通知 |
| `OnGoldChanged` | GameManager | `Action<int>` | 所持金変更通知 |
| `OnSceneLoaded` | GameManager | `Action<string>` | シーンロード完了通知 |
| `OnAttackPhaseChanged` | PlayerController | `Action<AttackPhase>` | 攻撃フェーズ変更 |
| `OnJustInputSuccess` | JustInputAction | `Action<HitResult>` | ジャスト入力成功 |
| `OnMaterialAdded` | InventoryManager | `Action<MaterialData, int>` | 素材追加通知 |
| `OnMaterialConsumed` | InventoryManager | `Action<MaterialData, int>` | 素材消費通知 |
| `OnDishAdded` | InventoryManager | `Action<CookedDishData>` | 料理ストック追加 |
| `OnDishServed` | InventoryManager | `Action<CookedDishData>` | 料理提供通知 |
| `OnCookingCompleted` | CookingMinigame | `Action<CookingResult>` | 調理完了通知 |
| `OnCustomerOrdered` | CustomerAI | `Action<RecipeData>` | 客注文通知 |
| `OnPaymentMade` | CustomerAI | `Action<int>` | 支払い完了通知 |

---

## 付録 C: 命名規則統一表

| カテゴリ | ScriptableObject クラス名 | ファイル名プレフィックス | 例 |
|---------|--------------------------|------------------------|-----|
| 素材 | `MaterialData` | `MAT_` | `MAT_DragonScale.asset` |
| レシピ | `RecipeData` | `RCP_` | `RCP_DragonSteak.asset` |
| 武器 | `WeaponData` | `WPN_` | `WPN_GreatSword01.asset` |
| 家具 | `FurnitureData` | `FRN_` | `FRN_WoodenTable.asset` |
| 敵 | `EnemyData` | `ENM_` | `ENM_Goblin01.asset` |
| 設定 | 各種Config | なし | `JustInputConfig.asset` |

> **注意**: action-programmer の `WeaponDataSO` は `WeaponData` に統一する。クラス名に `SO` サフィックスは付けない（Unity の慣習に合わせる）。

---

*以上、フェーズ1統合システム設計書 終わり*
