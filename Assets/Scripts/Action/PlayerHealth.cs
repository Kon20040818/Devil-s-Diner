using System;
using UnityEngine;

/// <summary>
/// プレイヤーのHP管理とダメージ処理を担当するコンポーネント。
/// IDamageableを実装し、EnemyControllerからの攻撃を受け付ける。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    // ───────────────────────── Inspector Fields ─────────────────────────
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _deathGoldPenalty = 100;

    // ───────────────────────── Internal State ─────────────────────────
    private bool _isInvincible;

    // ───────────────────────── Properties ─────────────────────────
    public int MaxHP => _maxHP;
    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsInvincible => _isInvincible;

    // ───────────────────────── Events ─────────────────────────
    /// <summary>現在HPが変化したとき (currentHP, maxHP)</summary>
    public event Action<int, int> OnHPChanged;

    /// <summary>プレイヤーが戦闘不能になったとき</summary>
    public event Action OnPlayerDeath;

    // ───────────────────────── Lifecycle ─────────────────────────
    private void Awake()
    {
        CurrentHP = _maxHP;
    }

    // ───────────────────────── IDamageable ─────────────────────────
    /// <summary>
    /// ダメージを受ける。HPが0以下になると戦闘不能処理を実行する。
    /// </summary>
    public void TakeDamage(HitResult hitResult)
    {
        if (!IsAlive) return;
        if (_isInvincible) return;

        CurrentHP = Mathf.Max(CurrentHP - hitResult.FinalDamage, 0);
        OnHPChanged?.Invoke(CurrentHP, _maxHP);

        Debug.Log($"[PlayerHealth] ダメージ {hitResult.FinalDamage} (残HP: {CurrentHP}/{MaxHP})");

        if (CurrentHP <= 0)
        {
            OnDeath();
        }
    }

    // ───────────────────────── Public Methods ─────────────────────────
    /// <summary>
    /// HPを回復する。最大HPを超えないようにクランプされる。
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        CurrentHP = Mathf.Min(CurrentHP + amount, _maxHP);
        OnHPChanged?.Invoke(CurrentHP, _maxHP);
    }

    /// <summary>
    /// HPを最大値にリセットする。
    /// </summary>
    public void ResetHP()
    {
        CurrentHP = _maxHP;
        OnHPChanged?.Invoke(CurrentHP, _maxHP);
    }

    /// <summary>
    /// 無敵状態を設定する。回避中に呼ばれる。
    /// </summary>
    public void SetInvincible(bool value)
    {
        _isInvincible = value;
    }

    /// <summary>
    /// 最大HPを設定する。現在HPを比率で調整する。
    /// </summary>
    public void SetMaxHP(int value)
    {
        int oldMax = _maxHP;
        _maxHP = Mathf.Max(1, value);
        // Proportionally scale current HP
        if (oldMax > 0)
        {
            CurrentHP = Mathf.CeilToInt((float)CurrentHP / oldMax * _maxHP);
        }
        else
        {
            CurrentHP = _maxHP;
        }
        OnHPChanged?.Invoke(CurrentHP, _maxHP);
    }

    // ───────────────────────── Private Methods ─────────────────────────
    /// <summary>
    /// 戦闘不能時の処理。ゴールドペナルティを適用し、フェーズを進める。
    /// </summary>
    private void OnDeath()
    {
        OnPlayerDeath?.Invoke();

        Debug.Log($"[PlayerHealth] プレイヤー戦闘不能！ ペナルティ: -{_deathGoldPenalty}G");

        // ゴールドペナルティ適用
        GameManager.Instance.AddGold(-_deathGoldPenalty);

        // Noon → Evening へフェーズ強制遷移（ManagementSceneへ移行）
        GameManager.Instance.AdvancePhase();
    }
}
