// ============================================================
// BattleManager.cs
// ターン制バトルの進行を管理するステートマシン。
// スターレイル風の速度ベース行動値システムでターンを回す。
// パーティ共有SP + キャラ個別EPのリソース管理を含む。
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// バトル全体の進行を制御するマネージャー。
/// バトルフェーズのステートマシンと ActionQueueSystem を統合し、
/// 味方・敵のターンを順に処理する。
/// </summary>
public sealed class BattleManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // バトルフェーズ
    // ──────────────────────────────────────────────

    /// <summary>バトル全体の進行状態。</summary>
    public enum BattlePhase
    {
        None,
        BattleStart,
        AdvancingQueue,
        PlayerCommand,
        EnemyAction,
        Executing,
        TurnEnd,
        Victory,
        Defeat
    }

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("バトル設定")]
    [Tooltip("アクション実行演出の基本秒数")]
    [SerializeField] private float _executeAnimDuration = 1.0f;

    [Header("SP設定")]
    [Tooltip("SP最大値")]
    [SerializeField] private int _maxSP = 5;
    [Tooltip("バトル開始時の初期SP")]
    [SerializeField] private int _initialSP = 3;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private readonly ActionQueueSystem _queue = new ActionQueueSystem();
    private CharacterBattleController _activeCharacter;
    private CharacterBattleController _selectedTarget;
    private CharacterBattleController.ActionType _selectedAction;

    private CharacterBattleController[] _playerParty;
    private CharacterBattleController[] _enemyParty;

    private int _currentSP;
    private bool _isAutoBattle;
    private bool _isDoubleSpeed;
    private bool _isUltimateInProgress;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.None;
    public CharacterBattleController ActiveCharacter => _activeCharacter;
    public ActionQueueSystem Queue => _queue;
    public IReadOnlyList<CharacterBattleController> PlayerParty => _playerParty;
    public IReadOnlyList<CharacterBattleController> EnemyParty => _enemyParty;

    /// <summary>パーティ共有SP（現在値）。</summary>
    public int CurrentSP => _currentSP;

    /// <summary>SP最大値。</summary>
    public int MaxSP => _maxSP;

    /// <summary>オートバトル中か。</summary>
    public bool IsAutoBattle => _isAutoBattle;

    /// <summary>倍速中か。</summary>
    public bool IsDoubleSpeed => _isDoubleSpeed;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<CharacterBattleController> OnActiveCharacterChanged;
    public event Action<bool> OnBattleEnd;

    /// <summary>SP が変化したとき (currentSP, maxSP)。</summary>
    public event Action<int, int> OnSPChanged;

    /// <summary>ダメージが確定したとき。ダメージ表示UIに使用。</summary>
    public event Action<CharacterBattleController.DamageResult> OnDamageDealt;

    /// <summary>必殺技の割り込み使用イベント。</summary>
    public event Action<CharacterBattleController> OnUltimateActivated;

    /// <summary>スキル発動イベント (キャラ, スキル名)。</summary>
    public event Action<CharacterBattleController, string> OnSkillExecuted;

    /// <summary>オートバトル切替イベント。</summary>
    public event Action<bool> OnAutoBattleChanged;

    /// <summary>倍速切替イベント。</summary>
    public event Action<bool> OnSpeedChanged;

    // ──────────────────────────────────────────────
    // カメラマネージャー参照
    // ──────────────────────────────────────────────

    private BattleCameraManager _cameraManager;

    public void SetCameraManager(BattleCameraManager cam)
    {
        _cameraManager = cam;
    }

    // ──────────────────────────────────────────────
    // AttackAction（ジャストアタック）参照
    // ──────────────────────────────────────────────

    private AttackAction _attackAction;

    public void SetAttackAction(AttackAction attackAction)
    {
        _attackAction = attackAction;
    }

    // ──────────────────────────────────────────────
    // EnemyAttackAction（ジャストガード）参照
    // ──────────────────────────────────────────────

    private EnemyAttackAction _enemyAttackAction;

    public void SetEnemyAttackAction(EnemyAttackAction enemyAttackAction)
    {
        _enemyAttackAction = enemyAttackAction;
    }

    // ──────────────────────────────────────────────
    // 公開 API — バトル開始
    // ──────────────────────────────────────────────

    public void StartBattle(CharacterBattleController[] playerParty, CharacterBattleController[] enemyParty)
    {
        _playerParty = playerParty;
        _enemyParty = enemyParty;

        // SP初期化
        _currentSP = _initialSP;
        OnSPChanged?.Invoke(_currentSP, _maxSP);

        _queue.Clear();

        foreach (var c in _playerParty)
        {
            if (c != null && c.IsAlive)
            {
                _queue.Register(c);
                c.OnDeath += HandleCharacterDeath;
            }
        }
        foreach (var c in _enemyParty)
        {
            if (c != null && c.IsAlive)
            {
                _queue.Register(c);
                c.OnDeath += HandleCharacterDeath;
            }
        }

        Debug.Log($"[BattleManager] バトル開始！ 味方:{_playerParty.Length}体 vs 敵:{_enemyParty.Length}体 SP:{_currentSP}/{_maxSP}");

        SetPhase(BattlePhase.BattleStart);
        StartCoroutine(BattleStartSequence());
    }

    // ──────────────────────────────────────────────
    // 公開 API — プレイヤーコマンド入力
    // ──────────────────────────────────────────────

    /// <summary>
    /// プレイヤーがアクションとターゲットを選択した際に呼ぶ。
    /// PlayerCommand フェーズ中のみ有効。
    /// </summary>
    public void ExecutePlayerAction(
        CharacterBattleController.ActionType actionType,
        CharacterBattleController target)
    {
        if (CurrentPhase != BattlePhase.PlayerCommand) return;
        if (target == null || !target.IsAlive) return;

        // SP/EP バリデーション
        switch (actionType)
        {
            case CharacterBattleController.ActionType.Skill:
                if (_currentSP < 1)
                {
                    Debug.LogWarning("[BattleManager] SP不足でスキルを使用できません。");
                    return;
                }
                break;
            case CharacterBattleController.ActionType.Ultimate:
                if (!_activeCharacter.IsUltimateReady)
                {
                    Debug.LogWarning("[BattleManager] EP不足で必殺技を使用できません。");
                    return;
                }
                break;
        }

        _selectedAction = actionType;
        _selectedTarget = target;
        StartCoroutine(ExecuteAction());
    }

    /// <summary>スキル使用に十分なSPがあるか。</summary>
    public bool CanUseSkill() => _currentSP >= 1;

    // ──────────────────────────────────────────────
    // SP操作
    // ──────────────────────────────────────────────

    private void AddSP(int amount)
    {
        _currentSP = Mathf.Clamp(_currentSP + amount, 0, _maxSP);
        OnSPChanged?.Invoke(_currentSP, _maxSP);
    }

    private void ConsumeSP(int amount)
    {
        _currentSP = Mathf.Max(0, _currentSP - amount);
        OnSPChanged?.Invoke(_currentSP, _maxSP);
    }

    // ──────────────────────────────────────────────
    // バトルループ
    // ──────────────────────────────────────────────

    private IEnumerator BattleStartSequence()
    {
        if (_cameraManager != null)
        {
            // フィールド中心を味方と敵の中間に設定
            Vector3 center = Vector3.zero;
            int count = 0;
            if (_playerParty != null)
                foreach (var c in _playerParty)
                    if (c != null) { center += c.transform.position; count++; }
            if (_enemyParty != null)
                foreach (var c in _enemyParty)
                    if (c != null) { center += c.transform.position; count++; }
            if (count > 0) center /= count;
            _cameraManager.SetFieldCenter(center);
            _cameraManager.SwitchToOverview();
        }
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(NextTurn());
    }

    private IEnumerator NextTurn()
    {
        if (CheckBattleEnd()) yield break;

        SetPhase(BattlePhase.AdvancingQueue);
        _activeCharacter = _queue.AdvanceAndGetNext();

        if (_activeCharacter == null)
        {
            Debug.LogError("[BattleManager] 行動可能なキャラクターがいません。");
            yield break;
        }

        OnActiveCharacterChanged?.Invoke(_activeCharacter);
        Debug.Log($"[BattleManager] 次の行動: {_activeCharacter.DisplayName} ({_activeCharacter.CharacterFaction})");

        if (_cameraManager != null) _cameraManager.FocusOnCharacter(_activeCharacter.transform);

        yield return new WaitForSeconds(0.3f);

        if (_activeCharacter.CharacterFaction == CharacterBattleController.Faction.Player)
        {
            _activeCharacter.SetState(CharacterBattleController.BattleState.SelectingAction);
            SetPhase(BattlePhase.PlayerCommand);

            if (_isAutoBattle)
            {
                yield return new WaitForSeconds(0.2f);
                AutoSelectAction();
            }
        }
        else
        {
            SetPhase(BattlePhase.EnemyAction);
            yield return StartCoroutine(EnemyTurn());
        }
    }

    private IEnumerator EnemyTurn()
    {
        _activeCharacter.SetState(CharacterBattleController.BattleState.Executing);

        // 敵ターンのワイドカメラ
        _selectedTarget = _activeCharacter.ChooseTarget(_playerParty);
        _selectedAction = CharacterBattleController.ActionType.BasicAttack;

        if (_selectedTarget == null)
        {
            yield return StartCoroutine(TurnEnd());
            yield break;
        }

        if (_cameraManager != null)
            _cameraManager.SwitchToEnemyCamera(_activeCharacter.transform, _selectedTarget.transform);

        yield return StartCoroutine(ExecuteAction());
    }

    private IEnumerator ExecuteAction()
    {
        SetPhase(BattlePhase.Executing);
        _activeCharacter.SetState(CharacterBattleController.BattleState.Executing);

        // アクション種別に応じたカメラ切替
        if (_cameraManager != null)
        {
            // 敵のターンの場合は、すでにSwitchToEnemyCameraで防衛視点になっているため
            // 基本的にはそのままの視点を維持する（敵の肩越しアクションカメラには絶対切り替えない）
            if (_activeCharacter.CharacterFaction == CharacterBattleController.Faction.Enemy)
            {
                if (_selectedAction == CharacterBattleController.ActionType.Skill)
                {
                    OnSkillExecuted?.Invoke(_activeCharacter, "スキル");
                }
            }
            else
            {
                // 味方のターンの場合は、アクションカメラに切り替える
                switch (_selectedAction)
                {
                    case CharacterBattleController.ActionType.Skill:
                        _cameraManager.SwitchToSkillCamera(
                            _activeCharacter.transform,
                            _selectedTarget.transform);
                        OnSkillExecuted?.Invoke(_activeCharacter, "スキル");
                        break;
                    default:
                        _cameraManager.SwitchToActionCamera(
                            _activeCharacter.transform,
                            _selectedTarget.transform);
                        break;
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // ── リソース消費 ──
        switch (_selectedAction)
        {
            case CharacterBattleController.ActionType.Skill:
                ConsumeSP(1);
                Debug.Log($"[BattleManager] SP消費 → 残SP: {_currentSP}/{_maxSP}");
                break;
            case CharacterBattleController.ActionType.Ultimate:
                _activeCharacter.ConsumeAllEP();
                Debug.Log($"[BattleManager] EP全消費 → {_activeCharacter.DisplayName} EP: 0/{_activeCharacter.MaxEP}");
                break;
        }

        // ── 味方通常攻撃 + AttackAction 存在時：ジャストアタック分岐 ──
        bool useJustAttack = _selectedAction == CharacterBattleController.ActionType.BasicAttack
            && _activeCharacter.CharacterFaction == CharacterBattleController.Faction.Player
            && _attackAction != null;

        // ── 敵通常攻撃 + EnemyAttackAction 存在時：ジャストガード分岐 ──
        bool useJustGuard = _selectedAction == CharacterBattleController.ActionType.BasicAttack
            && _activeCharacter.CharacterFaction == CharacterBattleController.Faction.Enemy
            && _enemyAttackAction != null;

        if (useJustAttack)
        {
            yield return StartCoroutine(ExecuteJustAttack());
        }
        else if (useJustGuard)
        {
            yield return StartCoroutine(ExecuteEnemyJustGuard());
        }
        else
        {
            yield return StartCoroutine(ExecuteNormalDamage());
        }

        yield return StartCoroutine(TurnEnd());
    }

    // ──────────────────────────────────────────────
    // 通常ダメージ処理（スキル・必殺技・敵攻撃・非ジャスト通常攻撃）
    // ──────────────────────────────────────────────

    private IEnumerator ExecuteNormalDamage()
    {
        int damage = _activeCharacter.CalculateDamage(_selectedAction);
        CharacterStats.ElementType element = _activeCharacter.Stats != null
            ? _activeCharacter.Stats.Element
            : CharacterStats.ElementType.Physical;

        int dealt = _selectedTarget.TakeDamage(damage, element, _activeCharacter);

        var damageResult = new CharacterBattleController.DamageResult
        {
            FinalDamage = dealt,
            Element = element,
            IsWeakness = _selectedTarget.Stats != null && _selectedTarget.Stats.IsWeakTo(element),
            CausedBreak = _selectedTarget.IsBroken && _selectedTarget.CurrentToughness <= 0,
            Target = _selectedTarget,
            Attacker = _activeCharacter
        };
        OnDamageDealt?.Invoke(damageResult);

        if (_cameraManager != null)
        {
            switch (_selectedAction)
            {
                case CharacterBattleController.ActionType.Skill:
                    _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_SKILL_HIT);
                    break;
                case CharacterBattleController.ActionType.Ultimate:
                    _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_ULTIMATE_IMPACT);
                    break;
                default:
                    if (_activeCharacter.CharacterFaction == CharacterBattleController.Faction.Player)
                        _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BASIC_HIT);
                    else
                        _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_ENEMY_HIT);
                    break;
            }

            if (damageResult.CausedBreak)
                _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BREAK);
        }

        string actionName = _selectedAction switch
        {
            CharacterBattleController.ActionType.Skill => "スキル",
            CharacterBattleController.ActionType.Ultimate => "必殺技",
            _ => "通常攻撃"
        };
        Debug.Log($"[BattleManager] {_activeCharacter.DisplayName} の{actionName} → {_selectedTarget.DisplayName} に {dealt} ダメージ！");

        if (_selectedAction == CharacterBattleController.ActionType.BasicAttack)
        {
            AddSP(1);
        }

        int epGain = _activeCharacter.GetEPGain(_selectedAction);
        if (epGain > 0) _activeCharacter.AddEP(epGain);

        yield return new WaitForSeconds(_executeAnimDuration);
    }

    // ──────────────────────────────────────────────
    // ジャストアタック処理（AttackAction 連携・複数ヒット）
    // ──────────────────────────────────────────────

    private IEnumerator ExecuteJustAttack()
    {
        int baseDamage = _activeCharacter.CalculateDamage(_selectedAction);
        CharacterStats.ElementType element = _activeCharacter.Stats != null
            ? _activeCharacter.Stats.Element
            : CharacterStats.ElementType.Physical;

        int totalDealt = 0;
        int hitCount = _attackAction.HitCount;
        // 各ヒットのダメージはヒット数で均等分割
        int perHitDamage = Mathf.Max(1, baseDamage / hitCount);

        yield return StartCoroutine(_attackAction.ExecuteAttackCoroutine((hitIndex, isJust) =>
        {
            if (_selectedTarget == null || !_selectedTarget.IsAlive) return;

            int hitDamage = isJust
                ? Mathf.RoundToInt(perHitDamage * _attackAction.JustMultiplier)
                : perHitDamage;

            int dealt = _selectedTarget.TakeDamage(hitDamage, element, _activeCharacter);
            totalDealt += dealt;

            var damageResult = new CharacterBattleController.DamageResult
            {
                FinalDamage = dealt,
                Element = element,
                IsWeakness = _selectedTarget.Stats != null && _selectedTarget.Stats.IsWeakTo(element),
                CausedBreak = _selectedTarget.IsBroken && _selectedTarget.CurrentToughness <= 0,
                Target = _selectedTarget,
                Attacker = _activeCharacter
            };
            OnDamageDealt?.Invoke(damageResult);

            if (_cameraManager != null)
            {
                _cameraManager.ShakeCamera(isJust
                    ? BattleCameraManager.SHAKE_SKILL_HIT
                    : BattleCameraManager.SHAKE_BASIC_HIT);

                if (damageResult.CausedBreak)
                    _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BREAK);
            }

            Debug.Log($"[BattleManager] {_activeCharacter.DisplayName} Hit {hitIndex + 1}{(isJust ? " (JUST!)" : "")} → {_selectedTarget.DisplayName} に {dealt} ダメージ！");
        }));

        Debug.Log($"[BattleManager] {_activeCharacter.DisplayName} ジャストアタック合計 → {totalDealt} ダメージ！");

        // 通常攻撃: SP+1, EP獲得
        AddSP(1);
        int epGain = _activeCharacter.GetEPGain(_selectedAction);
        if (epGain > 0) _activeCharacter.AddEP(epGain);
    }

    // ──────────────────────────────────────────────
    // ジャストガード処理（EnemyAttackAction 連携・敵攻撃）
    // ──────────────────────────────────────────────

    private IEnumerator ExecuteEnemyJustGuard()
    {
        int baseDamage = _activeCharacter.CalculateDamage(_selectedAction);
        CharacterStats.ElementType element = _activeCharacter.Stats != null
            ? _activeCharacter.Stats.Element
            : CharacterStats.ElementType.Physical;

        int totalDealt = 0;
        int hitCount = _enemyAttackAction.HitCount;
        int perHitDamage = Mathf.Max(1, baseDamage / hitCount);

        yield return StartCoroutine(_enemyAttackAction.ExecuteAttackCoroutine((hitIndex, guardResult) =>
        {
            if (_selectedTarget == null || !_selectedTarget.IsAlive) return;

            float multiplier = guardResult switch
            {
                EnemyAttackAction.GuardResult.JustGuard => _enemyAttackAction.JustGuardMultiplier,
                EnemyAttackAction.GuardResult.NormalGuard => _enemyAttackAction.NormalGuardMultiplier,
                _ => 1.0f
            };

            int hitDamage = Mathf.RoundToInt(perHitDamage * multiplier);
            int dealt = _selectedTarget.TakeDamage(hitDamage, element, _activeCharacter);
            totalDealt += dealt;

            var damageResult = new CharacterBattleController.DamageResult
            {
                FinalDamage = dealt,
                Element = element,
                IsWeakness = _selectedTarget.Stats != null && _selectedTarget.Stats.IsWeakTo(element),
                CausedBreak = _selectedTarget.IsBroken && _selectedTarget.CurrentToughness <= 0,
                Target = _selectedTarget,
                Attacker = _activeCharacter
            };
            OnDamageDealt?.Invoke(damageResult);

            if (_cameraManager != null)
            {
                // ジャストガード成功時は軽めのシェイク、失敗時は敵攻撃級
                switch (guardResult)
                {
                    case EnemyAttackAction.GuardResult.JustGuard:
                        _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BASIC_HIT);
                        break;
                    case EnemyAttackAction.GuardResult.NormalGuard:
                        _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BASIC_HIT);
                        break;
                    default:
                        _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_ENEMY_HIT);
                        break;
                }

                if (damageResult.CausedBreak)
                    _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BREAK);
            }

            string guardLabel = guardResult switch
            {
                EnemyAttackAction.GuardResult.JustGuard => "JUST GUARD!",
                EnemyAttackAction.GuardResult.NormalGuard => "Guard",
                _ => "NO GUARD"
            };
            Debug.Log($"[BattleManager] {_activeCharacter.DisplayName} Hit {hitIndex + 1} ({guardLabel}) → {_selectedTarget.DisplayName} に {dealt} ダメージ！");
        }));

        Debug.Log($"[BattleManager] {_activeCharacter.DisplayName} 敵攻撃合計 → {totalDealt} ダメージ（ジャストガード付き）");
    }

    private IEnumerator TurnEnd()
    {
        SetPhase(BattlePhase.TurnEnd);
        _activeCharacter.SetState(CharacterBattleController.BattleState.WaitingTurn);

        if (_cameraManager != null) _cameraManager.SwitchToOverview();

        yield return new WaitForSeconds(0.3f);

        StartCoroutine(NextTurn());
    }

    // ──────────────────────────────────────────────
    // 勝敗判定
    // ──────────────────────────────────────────────

    private bool CheckBattleEnd()
    {
        bool allEnemiesDead = true;
        foreach (var e in _enemyParty)
        {
            if (e != null && e.IsAlive) { allEnemiesDead = false; break; }
        }
        if (allEnemiesDead)
        {
            SetPhase(BattlePhase.Victory);
            if (_cameraManager != null) _cameraManager.SwitchToVictoryCamera();
            OnBattleEnd?.Invoke(true);
            Debug.Log("[BattleManager] 勝利！");
            return true;
        }

        bool allPlayersDead = true;
        foreach (var p in _playerParty)
        {
            if (p != null && p.IsAlive) { allPlayersDead = false; break; }
        }
        if (allPlayersDead)
        {
            SetPhase(BattlePhase.Defeat);
            if (_cameraManager != null) _cameraManager.SwitchToDefeatCamera();
            OnBattleEnd?.Invoke(false);
            Debug.Log("[BattleManager] 敗北…");
            return true;
        }

        return false;
    }

    // ──────────────────────────────────────────────
    // イベントハンドラ
    // ──────────────────────────────────────────────

    private void HandleCharacterDeath(CharacterBattleController character)
    {
        _queue.Unregister(character);
    }

    private void SetPhase(BattlePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    // ──────────────────────────────────────────────
    // 必殺技割り込み
    // ──────────────────────────────────────────────

    /// <summary>
    /// 必殺技を割り込みで使用する。PlayerCommandフェーズ以外でも発動可能。
    /// </summary>
    public void ExecuteUltimate(CharacterBattleController character, CharacterBattleController target)
    {
        if (character == null || target == null) return;
        if (!character.IsUltimateReady) return;
        if (!character.IsAlive || !target.IsAlive) return;
        if (_isUltimateInProgress) return;

        OnUltimateActivated?.Invoke(character);
        StartCoroutine(UltimateInterruptSequence(character, target));
    }

    private IEnumerator UltimateInterruptSequence(
        CharacterBattleController character,
        CharacterBattleController target)
    {
        _isUltimateInProgress = true;
        var previousPhase = CurrentPhase;
        var previousActive = _activeCharacter;

        SetPhase(BattlePhase.Executing);
        _activeCharacter = character;
        OnActiveCharacterChanged?.Invoke(character);

        character.ConsumeAllEP();

        // Phase 1: クローズアップ + スローモーション
        if (_cameraManager != null)
        {
            _cameraManager.SwitchToUltimateCamera(character.transform, target.transform);
            _cameraManager.SlowMotion(0.3f, 0.25f);
        }

        yield return new WaitForSeconds(0.5f);

        // Phase 2: アクションカメラに切替
        if (_cameraManager != null)
            _cameraManager.SwitchToUltimateActionCamera(character.transform, target.transform);

        yield return new WaitForSeconds(0.3f);

        int damage = character.CalculateUltimateDamage();
        CharacterStats.ElementType element = character.Stats != null
            ? character.Stats.Element : CharacterStats.ElementType.Physical;
        int dealt = target.TakeDamage(damage, element, character);

        var damageResult = new CharacterBattleController.DamageResult
        {
            FinalDamage = dealt,
            Element = element,
            IsWeakness = target.Stats != null && target.Stats.IsWeakTo(element),
            CausedBreak = target.IsBroken && target.CurrentToughness <= 0,
            Target = target,
            Attacker = character
        };
        OnDamageDealt?.Invoke(damageResult);

        // 必殺技インパクトシェイク
        if (_cameraManager != null)
        {
            _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_ULTIMATE_IMPACT);
            if (damageResult.CausedBreak)
                _cameraManager.ShakeCamera(BattleCameraManager.SHAKE_BREAK);
        }

        Debug.Log($"[BattleManager] {character.DisplayName} の必殺技(割込) → {target.DisplayName} に {dealt} ダメージ！");

        yield return new WaitForSeconds(_executeAnimDuration);

        _isUltimateInProgress = false;

        _activeCharacter = previousActive;
        if (previousActive != null)
            OnActiveCharacterChanged?.Invoke(previousActive);

        if (_cameraManager != null) _cameraManager.SwitchToOverview();

        if (!CheckBattleEnd())
        {
            SetPhase(previousPhase);
        }
    }

    // ──────────────────────────────────────────────
    // オートバトル / 倍速
    // ──────────────────────────────────────────────

    /// <summary>オートバトルを切り替える。</summary>
    public void ToggleAutoBattle()
    {
        _isAutoBattle = !_isAutoBattle;
        OnAutoBattleChanged?.Invoke(_isAutoBattle);

        // 現在PlayerCommandフェーズならすぐに自動行動
        if (_isAutoBattle && CurrentPhase == BattlePhase.PlayerCommand)
        {
            AutoSelectAction();
        }
    }

    /// <summary>倍速を切り替える。</summary>
    public void ToggleSpeed()
    {
        _isDoubleSpeed = !_isDoubleSpeed;
        Time.timeScale = _isDoubleSpeed ? 2f : 1f;
        OnSpeedChanged?.Invoke(_isDoubleSpeed);
    }

    private void AutoSelectAction()
    {
        if (_activeCharacter == null) return;
        var target = _activeCharacter.ChooseTarget(_enemyParty);
        if (target == null) return;

        if (CanUseSkill())
        {
            ExecutePlayerAction(CharacterBattleController.ActionType.Skill, target);
        }
        else
        {
            ExecutePlayerAction(CharacterBattleController.ActionType.BasicAttack, target);
        }
    }

    private void OnDestroy()
    {
        if (_playerParty != null)
            foreach (var c in _playerParty)
                if (c != null) c.OnDeath -= HandleCharacterDeath;
        if (_enemyParty != null)
            foreach (var c in _enemyParty)
                if (c != null) c.OnDeath -= HandleCharacterDeath;
    }
}
