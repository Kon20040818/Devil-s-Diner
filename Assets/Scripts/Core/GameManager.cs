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
    private const string BOOT_SCENE       = "BootScene";
    private const string BASE_SCENE       = "BaseScene";
    private const string FIELD_SCENE      = "FieldScene";
    private const string MANAGEMENT_SCENE = "ManagementScene";
    private const float  DEFAULT_FIXED_DELTA_TIME = 0.02f; // 50 Hz
    private const int    STARTING_GOLD = 500;
    private const int    STARTING_DAY  = 1;

    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>ゲームフェーズ。</summary>
    public enum GamePhase
    {
        /// <summary>出撃準備（BaseScene）</summary>
        Morning,
        /// <summary>フィールド探索＋バトル（FieldScene / BattleScene）</summary>
        Noon,
        /// <summary>経営パート（ManagementScene）</summary>
        Evening
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
    // StaffManager 参照
    // ──────────────────────────────────────────────

    /// <summary>スタッフ管理。GameManager と同じ GameObject にアタッチ。</summary>
    public StaffManager Staff { get; private set; }

    // ──────────────────────────────────────────────
    // HousingManager 参照
    // ──────────────────────────────────────────────

    /// <summary>家具管理。GameManager と同じ GameObject にアタッチ。</summary>
    public HousingManager Housing { get; private set; }

    // ──────────────────────────────────────────────
    // シェフレベル / 調理経験値
    // ──────────────────────────────────────────────

    /// <summary>レベルアップに必要な累計 XP 閾値。</summary>
    private static readonly int[] LEVEL_THRESHOLDS = { 0, 100, 300, 600, 1000, 1500 };

    /// <summary>現在の調理経験値。</summary>
    public int CookingXP { get; private set; } = 0;

    /// <summary>現在のシェフレベル（1始まり）。</summary>
    public int ChefLevel { get; private set; } = 1;

    /// <summary>シェフレベルが上がったとき。引数は新レベル。</summary>
    public event Action<int> OnChefLevelUp;

    /// <summary>調理経験値を加算し、レベルアップ判定を行う。</summary>
    public void AddCookingXP(int xp)
    {
        CookingXP += xp;
        int newLevel = 1;
        for (int i = LEVEL_THRESHOLDS.Length - 1; i >= 0; i--)
        {
            if (CookingXP >= LEVEL_THRESHOLDS[i]) { newLevel = i + 1; break; }
        }
        if (newLevel > ChefLevel)
        {
            ChefLevel = newLevel;
            OnChefLevelUp?.Invoke(ChefLevel);
            Debug.Log($"[GameManager] シェフレベルアップ！ Lv.{ChefLevel}");
        }
    }

    /// <summary>セーブデータ復元用。外部からシェフレベルを設定する。</summary>
    public void SetChefLevel(int lv) { ChefLevel = Mathf.Max(1, lv); }

    /// <summary>セーブデータ復元用。外部から調理経験値を設定する。</summary>
    public void SetCookingXP(int xp) { CookingXP = xp; }

    /// <summary>次のレベルアップまでに必要な XP 閾値を返す。最大レベルなら -1。</summary>
    public int GetNextLevelThreshold()
    {
        if (ChefLevel >= LEVEL_THRESHOLDS.Length) return -1;
        return LEVEL_THRESHOLDS[ChefLevel];
    }

    // ──────────────────────────────────────────────
    // 評判
    // ──────────────────────────────────────────────

    /// <summary>店舗の評判値。DinerService の営業結果で増減する。</summary>
    public int Reputation { get; private set; } = 0;

    /// <summary>評判変動時イベント。引数は変動後の評判値。</summary>
    public event Action<int> OnReputationChanged;

    /// <summary>評判を加算する（負値で減算可能）。0 未満にはならない。</summary>
    public void AddReputation(int amount)
    {
        Reputation = Mathf.Max(0, Reputation + amount);
        OnReputationChanged?.Invoke(Reputation);
    }

    /// <summary>セーブデータ復元用。外部から評判を設定する。</summary>
    public void SetReputation(int rep) { Reputation = rep; }

    // ──────────────────────────────────────────────
    // 鮮度バフ（バトル成績由来）
    // ──────────────────────────────────────────────

    /// <summary>当日の鮮度バフ。BattleResult から書き込まれ、CookingManager が参照する。</summary>
    public float DailyFreshnessBuff { get; set; } = 1f;

    // ──────────────────────────────────────────────
    // 装備武器
    // ──────────────────────────────────────────────

    /// <summary>装備中の武器の ItemID。空文字は未装備。</summary>
    public string EquippedWeaponID { get; private set; } = "";

    /// <summary>武器を装備する。</summary>
    public void EquipWeapon(string weaponItemID)
    {
        EquippedWeaponID = weaponItemID ?? "";
        Debug.Log($"[GameManager] 武器装備: {(string.IsNullOrEmpty(EquippedWeaponID) ? "なし" : EquippedWeaponID)}");
    }

    /// <summary>セーブデータ復元用。</summary>
    public void SetEquippedWeaponID(string id) { EquippedWeaponID = id ?? ""; }

    /// <summary>装備中の WeaponData を返す。未装備なら null。</summary>
    public WeaponData GetEquippedWeapon()
    {
        if (string.IsNullOrEmpty(EquippedWeaponID)) return null;

        // インベントリ内のアイテムから検索
        foreach (var kvp in Inventory.GetAllItems())
        {
            if (kvp.Key is WeaponData weapon && weapon.ItemID == EquippedWeaponID)
                return weapon;
        }
        return null;
    }

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

        // StaffManager を追加
        if (!TryGetComponent(out StaffManager staffMgr))
        {
            staffMgr = gameObject.AddComponent<StaffManager>();
        }
        Staff = staffMgr;

        // HousingManager を追加
        if (!TryGetComponent(out HousingManager housingMgr))
        {
            housingMgr = gameObject.AddComponent<HousingManager>();
        }
        Housing = housingMgr;

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
                // 拠点 → フィールド探索
                SetPhase(GamePhase.Noon);
                LoadSceneAsync(FIELD_SCENE);
                break;

            case GamePhase.Noon:
                // フィールド → 経営パート
                SetPhase(GamePhase.Evening);
                LoadSceneAsync(MANAGEMENT_SCENE);
                break;

            case GamePhase.Evening:
                // 経営 → 拠点（日数+1、給料支払い、臨時消去、自動セーブ）
                AdvanceDay();
                SetPhase(GamePhase.Morning);

                // 朝の給料支払い + 臨時スタッフ消去
                if (Staff != null)
                {
                    Staff.ProcessMorningPayroll();
                    Staff.ClearTemporaryStaff();
                }

                // 鮮度バフリセット（翌日は新しいバトル成績で再計算）
                DailyFreshnessBuff = 1f;

                if (SaveData != null)
                {
                    SaveData.Save();
                    Debug.Log("[GameManager] フェーズ遷移 (Evening → Morning) で自動セーブ実行。");
                }

                LoadSceneAsync(BASE_SCENE);
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
        Reputation   = 0;
        ChefLevel    = 1;
        CookingXP    = 0;
        DailyFreshnessBuff = 1f;
        EquippedWeaponID = "";
        Inventory.ClearAll();
        Staff?.ClearAll();
        Housing?.ClearAll();
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
