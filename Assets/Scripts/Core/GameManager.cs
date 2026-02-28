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
    // セーブデータ復元用セッター
    // ──────────────────────────────────────────────

    /// <summary>セーブデータ復元用。外部から日数を設定する。</summary>
    public void SetCurrentDay(int day) { CurrentDay = day; }

    /// <summary>セーブデータ復元用。外部から所持金を設定する。</summary>
    public void SetGold(int gold) { Gold = gold; OnGoldChanged?.Invoke(Gold); }

    /// <summary>セーブデータ復元用。外部からShopLevelを設定する。</summary>
    public void SetShopLevel(int level) { ShopLevel = level; }

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
    // SaveDataManager 参照
    // ──────────────────────────────────────────────

    /// <summary>セーブデータ管理。GameManager と同じ GameObject にアタッチ。</summary>
    public SaveDataManager SaveData { get; private set; }

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

        // デバッグコントローラーを追加
        if (!TryGetComponent(out DebugController _))
        {
            gameObject.AddComponent<DebugController>();
        }

        // SaveDataManager を追加
        if (!TryGetComponent(out SaveDataManager saveDataMgr))
        {
            saveDataMgr = gameObject.AddComponent<SaveDataManager>();
        }
        SaveData = saveDataMgr;

        // AudioManager を追加
        if (!TryGetComponent(out AudioManager _audioMgr))
        {
            gameObject.AddComponent<AudioManager>();
        }

        // SkillManager を追加
        if (!TryGetComponent(out SkillManager _skillMgr))
        {
            gameObject.AddComponent<SkillManager>();
        }

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

                // Midnight → Morning 遷移時に自動セーブ
                if (SaveData != null)
                {
                    SaveData.Save();
                    Debug.Log("[GameManager] フェーズ遷移 (Midnight → Morning) で自動セーブ実行。");
                }
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
    // 公開 API — シーン遷移（タイトル画面等から使用）
    // ──────────────────────────────────────────────

    /// <summary>指定シーンへ非同期遷移する。</summary>
    public void TransitionToScene(string sceneName)
    {
        LoadSceneAsync(sceneName);
    }

    /// <summary>ManagementScene へ遷移し Morning フェーズを開始する。</summary>
    public void StartMorningPhase()
    {
        SetPhase(GamePhase.Morning);
        LoadSceneAsync(MANAGEMENT_SCENE);
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
