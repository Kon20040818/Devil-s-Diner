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
    private const string FIELD_SCENE   = "FieldScene";
    private const float  DEFAULT_FIXED_DELTA_TIME = 0.02f; // 50 Hz
    private const int    STARTING_GOLD = 500;
    private const int    STARTING_DAY  = 1;

    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>ゲームフェーズ。</summary>
    public enum GamePhase
    {
        /// <summary>出撃準備（ActionScene ロード前）</summary>
        Morning,
        /// <summary>狩猟アクション（ActionScene）</summary>
        Noon
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

    /// <summary>現在ロード中のシーン名。</summary>
    public string CurrentSceneName { get; private set; } = BOOT_SCENE;

    // ──────────────────────────────────────────────
    // セーブデータ復元用セッター
    // ──────────────────────────────────────────────

    /// <summary>セーブデータ復元用。外部から日数を設定する。</summary>
    public void SetCurrentDay(int day) { CurrentDay = day; }

    /// <summary>セーブデータ復元用。外部から所持金を設定する。</summary>
    public void SetGold(int gold) { Gold = gold; OnGoldChanged?.Invoke(Gold); }

    // ──────────────────────────────────────────────
    // バトル遷移データ
    // ──────────────────────────────────────────────

    /// <summary>フィールド→バトル遷移時の敵構成データ。BattleSceneBootstrap が消費する。</summary>
    public BattleTransitionData PendingBattleData { get; set; }

    /// <summary>PendingBattleData をクリアする。</summary>
    public void ClearBattleTransitionData() => PendingBattleData = null;

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
        if (!_isTransitioning
            && CurrentSceneName != FIELD_SCENE
            && CurrentSceneName != "BattleScene"
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
    /// 次のフェーズへ進行する。
    /// Morning → Noon で ActionScene へ遷移。
    /// Noon → Morning で日数加算し自動セーブ。
    /// </summary>
    public void AdvancePhase()
    {
        if (_isTransitioning) return;

        switch (CurrentPhase)
        {
            case GamePhase.Morning:
                SetPhase(GamePhase.Noon);
                LoadSceneAsync(FIELD_SCENE);
                break;

            case GamePhase.Noon:
                AdvanceDay();
                SetPhase(GamePhase.Morning);

                if (SaveData != null)
                {
                    SaveData.Save();
                    Debug.Log("[GameManager] フェーズ遷移 (Noon → Morning) で自動セーブ実行。");
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
        Inventory.ClearAll();
    }

    // ──────────────────────────────────────────────
    // 公開 API — シーン遷移
    // ──────────────────────────────────────────────

    /// <summary>指定シーンへ非同期遷移する。</summary>
    public void TransitionToScene(string sceneName)
    {
        LoadSceneAsync(sceneName);
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
