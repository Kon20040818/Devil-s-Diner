// ============================================================
// CharacterBattleController.cs
// 味方・敵共通のバトルキャラクター制御コンポーネント。
// HP管理、ダメージ計算、コマンド待機を担当する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// バトルに参加するキャラクター（味方・敵共通）の制御コンポーネント。
/// CharacterStats (ScriptableObject) からステータスを読み込み、
/// BattleManager からの指示でコマンドを実行する。
/// </summary>
public sealed class CharacterBattleController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 列挙型
    // ──────────────────────────────────────────────

    /// <summary>キャラクターの陣営。</summary>
    public enum Faction
    {
        Player,
        Enemy
    }

    /// <summary>バトル中の状態。</summary>
    public enum BattleState
    {
        /// <summary>行動順待ち</summary>
        WaitingTurn,
        /// <summary>コマンド選択中（プレイヤーのみ）</summary>
        SelectingAction,
        /// <summary>アクション実行中（アニメーション再生中）</summary>
        Executing,
        /// <summary>戦闘不能</summary>
        Down
    }

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("設定")]
    [SerializeField] private CharacterStats _stats;
    [SerializeField] private Faction _faction = Faction.Player;

    /// <summary>実行するアクションの種別。</summary>
    public enum ActionType
    {
        BasicAttack,
        Skill,
        Ultimate,
        Meal
    }

    /// <summary>ダメージ結果を格納する構造体。UI表示に使用。</summary>
    public struct DamageResult
    {
        public int FinalDamage;
        public CharacterStats.ElementType Element;
        public bool IsWeakness;
        public bool CausedBreak;
        public CharacterBattleController Target;
        public CharacterBattleController Attacker;
    }

    // ──────────────────────────────────────────────
    // ランタイムステート
    // ──────────────────────────────────────────────

    private int _currentHP;
    private int _currentEP;
    private int _currentToughness;
    private bool _isBroken;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    /// <summary>元データ（ScriptableObject）。</summary>
    public CharacterStats Stats => _stats;

    /// <summary>陣営。</summary>
    public Faction CharacterFaction => _faction;

    /// <summary>現在のバトル状態。</summary>
    public BattleState CurrentState { get; private set; } = BattleState.WaitingTurn;

    /// <summary>現在のHP。</summary>
    public int CurrentHP => _currentHP;

    /// <summary>最大HP。</summary>
    public int MaxHP => _stats != null ? _stats.MaxHP : 1;

    /// <summary>生存判定。</summary>
    public bool IsAlive => _currentHP > 0;

    /// <summary>現在のEP。</summary>
    public int CurrentEP => _currentEP;

    /// <summary>最大EP。</summary>
    public int MaxEP => _stats != null ? _stats.MaxEP : 1;

    /// <summary>必殺技が使用可能か。</summary>
    public bool IsUltimateReady => _currentEP >= MaxEP;

    /// <summary>表示名。</summary>
    public string DisplayName => _stats != null ? _stats.DisplayName : gameObject.name;

    /// <summary>現在の靭性値。</summary>
    public int CurrentToughness => _currentToughness;

    /// <summary>靭性最大値。</summary>
    public int MaxToughness => _stats != null ? _stats.MaxToughness : 0;

    /// <summary>靭性が破壊されているか。</summary>
    public bool IsBroken => _isBroken;

    /// <summary>靭性システムが有効か。</summary>
    public bool HasToughness => MaxToughness > 0;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>HPが変化したとき (currentHP, maxHP)。</summary>
    public event Action<int, int> OnHPChanged;

    /// <summary>戦闘不能になったとき。</summary>
    public event Action<CharacterBattleController> OnDeath;

    /// <summary>EPが変化したとき (currentEP, maxEP)。</summary>
    public event Action<int, int> OnEPChanged;

    /// <summary>バトル状態が変化したとき。</summary>
    public event Action<BattleState> OnStateChanged;

    /// <summary>靭性が変化したとき (currentToughness, maxToughness)。</summary>
    public event Action<int, int> OnToughnessChanged;

    /// <summary>靭性が0になったとき（弱点撃破）。</summary>
    public event Action<CharacterBattleController> OnToughnessBreak;

    /// <summary>ダメージ結果を通知するイベント。UIダメージ表示に使用。</summary>
    public event Action<DamageResult> OnDamageReceived;

    // ──────────────────────────────────────────────
    // 初期化
    // ──────────────────────────────────────────────

    /// <summary>バトル開始時に呼ぶ初期化。</summary>
    public void Initialize(CharacterStats stats, Faction faction)
    {
        _stats = stats;
        _faction = faction;
        _currentHP = stats.MaxHP;
        _currentEP = 0;
        _currentToughness = stats.MaxToughness;
        _isBroken = false;
        SetState(BattleState.WaitingTurn);
    }

    private void Awake()
    {
        if (_stats != null)
        {
            _currentHP = _stats.MaxHP;
            _currentEP = 0;
            _currentToughness = _stats.MaxToughness;
            _isBroken = false;
        }
    }

    // ──────────────────────────────────────────────
    // ダメージ / 回復
    // ──────────────────────────────────────────────

    /// <summary>
    /// ダメージを受ける。防御力と属性耐性を考慮して最終ダメージを算出する。
    /// </summary>
    /// <param name="rawDamage">攻撃者の攻撃力ベースの素ダメージ。</param>
    /// <param name="element">攻撃の属性。</param>
    /// <returns>実際に与えたダメージ量。</returns>
    public int TakeDamage(int rawDamage, CharacterStats.ElementType element = CharacterStats.ElementType.Physical,
        CharacterBattleController attacker = null)
    {
        if (!IsAlive) return 0;

        // ── ダメージ計算: (素ダメージ - 防御力) × (1 - 耐性) ──
        float resistance = _stats != null ? _stats.GetResistance(element) : 0f;
        int defense = _stats != null ? _stats.Defense : 0;

        // 靭性破壊中はダメージ増加 (+25%)
        float breakBonus = _isBroken ? 1.25f : 1.0f;

        int reducedDamage = Mathf.Max(rawDamage - defense, 1);
        int finalDamage = Mathf.Max(Mathf.RoundToInt(reducedDamage * (1f - resistance) * breakBonus), 0);

        _currentHP = Mathf.Max(_currentHP - finalDamage, 0);
        OnHPChanged?.Invoke(_currentHP, MaxHP);

        // ── 靭性削り ──
        bool causedBreak = false;
        bool isWeakness = _stats != null && _stats.IsWeakTo(element);
        if (HasToughness && !_isBroken && isWeakness)
        {
            int toughnessDamage = 30;
            _currentToughness = Mathf.Max(0, _currentToughness - toughnessDamage);
            OnToughnessChanged?.Invoke(_currentToughness, MaxToughness);

            if (_currentToughness <= 0)
            {
                _isBroken = true;
                causedBreak = true;
                OnToughnessBreak?.Invoke(this);
            }
        }

        // ── DamageResult通知 ──
        var result = new DamageResult
        {
            FinalDamage = finalDamage,
            Element = element,
            IsWeakness = isWeakness,
            CausedBreak = causedBreak,
            Target = this,
            Attacker = attacker
        };
        OnDamageReceived?.Invoke(result);

        Debug.Log($"[Battle] {DisplayName} が {finalDamage} ダメージ (属性:{element}, 弱点:{isWeakness}) → HP: {_currentHP}/{MaxHP}");

        if (_currentHP <= 0)
        {
            SetState(BattleState.Down);
            OnDeath?.Invoke(this);
            Debug.Log($"[Battle] {DisplayName} は戦闘不能！");
        }
        else
        {
            int epGain = _stats != null ? _stats.EPGainOnHit : 0;
            if (epGain > 0) AddEP(epGain);
        }

        return finalDamage;
    }

    /// <summary>HPを回復する。</summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        _currentHP = Mathf.Min(_currentHP + amount, MaxHP);
        OnHPChanged?.Invoke(_currentHP, MaxHP);
    }

    // ──────────────────────────────────────────────
    // EP管理
    // ──────────────────────────────────────────────

    /// <summary>EPを加算する。最大値でクランプ。</summary>
    public void AddEP(int amount)
    {
        _currentEP = Mathf.Clamp(_currentEP + amount, 0, MaxEP);
        OnEPChanged?.Invoke(_currentEP, MaxEP);
    }

    /// <summary>EPを全消費する（必殺技使用時）。</summary>
    public void ConsumeAllEP()
    {
        _currentEP = 0;
        OnEPChanged?.Invoke(_currentEP, MaxEP);
    }

    // ──────────────────────────────────────────────
    // 状態遷移
    // ──────────────────────────────────────────────

    /// <summary>バトル状態を変更する。</summary>
    public void SetState(BattleState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // ──────────────────────────────────────────────
    // AI（敵用） — 簡易ロジック
    // ──────────────────────────────────────────────

    /// <summary>
    /// 敵AIが攻撃対象をランダムに選択する。
    /// 将来的にはスキルやAI行動パターンで拡張。
    /// </summary>
    public CharacterBattleController ChooseTarget(CharacterBattleController[] opponents)
    {
        if (opponents == null || opponents.Length == 0) return null;

        // 生存中のランダムな対象を選ぶ
        var alive = System.Array.FindAll(opponents, o => o != null && o.IsAlive);
        if (alive.Length == 0) return null;

        return alive[UnityEngine.Random.Range(0, alive.Length)];
    }

    /// <summary>
    /// 基本攻撃のダメージ値を計算する。
    /// </summary>
    public int CalculateBasicAttackDamage()
    {
        int baseDmg = _stats != null ? _stats.Attack : 1;
        return Mathf.RoundToInt(baseDmg * SkillEffectApplier.AttackMultiplier);
    }

    /// <summary>スキル攻撃のダメージ値を計算する。</summary>
    public int CalculateSkillDamage()
    {
        int baseDmg = _stats != null ? _stats.Attack : 1;
        float mult = _stats != null ? _stats.SkillMultiplier : 1.5f;
        return Mathf.RoundToInt(baseDmg * mult * SkillEffectApplier.AttackMultiplier);
    }

    /// <summary>必殺技のダメージ値を計算する。</summary>
    public int CalculateUltimateDamage()
    {
        int baseDmg = _stats != null ? _stats.Attack : 1;
        float mult = _stats != null ? _stats.UltimateMultiplier : 3.0f;
        return Mathf.RoundToInt(baseDmg * mult * SkillEffectApplier.AttackMultiplier);
    }

    /// <summary>指定アクションのダメージ値を返す。</summary>
    public int CalculateDamage(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Skill:    return CalculateSkillDamage();
            case ActionType.Ultimate: return CalculateUltimateDamage();
            default:                  return CalculateBasicAttackDamage();
        }
    }

    /// <summary>指定アクション実行後のEP獲得量を返す。</summary>
    public int GetEPGain(ActionType actionType)
    {
        if (_stats == null) return 0;
        switch (actionType)
        {
            case ActionType.BasicAttack: return _stats.EPGainOnAttack;
            case ActionType.Skill:       return _stats.EPGainOnSkill;
            default:                     return 0; // Ultimateは消費のみ
        }
    }
}
