// ============================================================
// PlayerController.cs
// プレイヤーの移動・攻撃・状態遷移を制御する。
// 入力は PlayerInputHandler 経由で取得する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// プレイヤーの移動・攻撃・状態管理を行うメインコントローラー。
/// CharacterController + Animator + PlayerInputHandler 必須。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInputHandler))]
public sealed class PlayerController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float GRAVITY = -9.81f;
    private const float GROUND_CHECK_DISTANCE = 0.1f;

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────
    [Header("移動")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 1.5f;
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("回避")]
    [SerializeField] private float _dodgeDistance = 5f;
    [SerializeField] private float _dodgeDuration = 0.35f;
    [SerializeField] private float _dodgeCooldown = 0.8f;
    [SerializeField] private float _dodgeInvincibleRatio = 0.8f; // ratio of dodge duration that's invincible

    [Header("攻撃")]
    [SerializeField] private WeaponData _equippedWeapon;
    [SerializeField] private WeaponColliderHandler _weaponColliderHandler;

    [Header("カメラ")]
    [SerializeField] private Transform _cameraTransform;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>攻撃フェーズが変化したとき。</summary>
    public event Action<AttackPhase> OnAttackPhaseChanged;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    /// <summary>現在の攻撃フェーズ。</summary>
    public AttackPhase CurrentAttackPhase { get; private set; } = AttackPhase.None;

    /// <summary>現在のプレイヤー状態。</summary>
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    /// <summary>装備中の武器データ。</summary>
    public WeaponData EquippedWeapon => _equippedWeapon;

    /// <summary>回避の移動距離。スキルで延長可能。</summary>
    public float DodgeDistance { get => _dodgeDistance; set => _dodgeDistance = value; }
    /// <summary>回避の持続時間（秒）。</summary>
    public float DodgeDuration { get => _dodgeDuration; set => _dodgeDuration = value; }
    /// <summary>回避中の無敵時間の割合。スキルで延長可能。</summary>
    public float DodgeInvincibleRatio { get => _dodgeInvincibleRatio; set => _dodgeInvincibleRatio = value; }

    // ──────────────────────────────────────────────
    // キャッシュ
    // ──────────────────────────────────────────────
    private CharacterController _characterController;
    private Animator _animator;
    private PlayerInputHandler _inputHandler;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isAttacking;

    // 回避内部状態
    private bool _isDodging;
    private float _dodgeTimer;
    private float _dodgeCooldownTimer;
    private Vector3 _dodgeDirection;
    private bool _dodgeInvincibilityEnded;

    // Animator パラメータハッシュ
    private static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
    private static readonly int ANIM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int ANIM_VERTICAL_VELOCITY = Animator.StringToHash("VerticalVelocity");
    private static readonly int ANIM_DODGE = Animator.StringToHash("Dodge");

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _inputHandler = GetComponent<PlayerInputHandler>();

        // カメラ参照が未設定ならメインカメラを使用
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnJumpTriggered += HandleJumpTriggered;
            _inputHandler.OnAttackTriggered += HandleAttackTriggered;
            _inputHandler.OnDodgeTriggered += HandleDodgeTriggered;
        }
    }

    private void OnDisable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnJumpTriggered -= HandleJumpTriggered;
            _inputHandler.OnAttackTriggered -= HandleAttackTriggered;
            _inputHandler.OnDodgeTriggered -= HandleDodgeTriggered;
        }
    }

    private void Update()
    {
        UpdateGroundCheck();
        UpdateDodge();
        UpdateMovement();
        UpdateAnimator();
        UpdateState();
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>攻撃フェーズを外部から強制設定する（ヒットストップ等）。</summary>
    public void ForceAttackPhase(AttackPhase phase)
    {
        SetAttackPhase(phase);
    }

    /// <summary>武器を装備する。</summary>
    public void EquipWeapon(WeaponData weapon)
    {
        _equippedWeapon = weapon;

        // AnimatorOverride があれば適用
        if (weapon != null && weapon.AnimatorOverride != null)
        {
            _animator.runtimeAnimatorController = weapon.AnimatorOverride;
        }
    }

    // ──────────────────────────────────────────────
    // Animation Event コールバック
    // ──────────────────────────────────────────────

    /// <summary>Animation Event: PreCast フェーズ開始。</summary>
    public void AnimEvent_PreCastStart()
    {
        SetAttackPhase(AttackPhase.PreCast);
    }

    /// <summary>Animation Event: Active フェーズ開始（攻撃判定開始）。</summary>
    public void AnimEvent_ActiveStart()
    {
        SetAttackPhase(AttackPhase.Active);

        // 武器ヒットボックスを有効化
        if (_weaponColliderHandler != null)
        {
            _weaponColliderHandler.EnableHitbox();
        }
    }

    /// <summary>Animation Event: Active フェーズ終了（攻撃判定終了）。</summary>
    public void AnimEvent_ActiveEnd()
    {
        SetAttackPhase(AttackPhase.Recovery);

        // 武器ヒットボックスを無効化
        if (_weaponColliderHandler != null)
        {
            _weaponColliderHandler.DisableHitbox();
        }
    }

    /// <summary>Animation Event: Recovery フェーズ終了（攻撃モーション完了）。</summary>
    public void AnimEvent_RecoveryEnd()
    {
        SetAttackPhase(AttackPhase.None);
        _isAttacking = false;
    }

    // ──────────────────────────────────────────────
    // 入力ハンドラー
    // ──────────────────────────────────────────────

    private void HandleJumpTriggered()
    {
        if (_isGrounded && !_isAttacking)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * GRAVITY);
        }
    }

    private void HandleAttackTriggered()
    {
        if (_isAttacking) return;
        if (_isDodging) return;
        if (!_isGrounded) return;

        _isAttacking = true;
        _animator.SetTrigger(ANIM_ATTACK);
        SetAttackPhase(AttackPhase.PreCast);
    }

    private void HandleDodgeTriggered()
    {
        if (_isDodging || _isAttacking || !_isGrounded) return;
        if (_dodgeCooldownTimer > 0f) return;

        _isDodging = true;
        _dodgeTimer = 0f;
        _dodgeInvincibilityEnded = false;

        // 移動入力があればカメラ相対の方向、なければ正面方向
        Vector2 moveInput = _inputHandler.MoveInput;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
            Vector3 right = _cameraTransform != null ? _cameraTransform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            _dodgeDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }
        else
        {
            _dodgeDirection = transform.forward;
        }

        _animator.SetTrigger(ANIM_DODGE);

        // 無敵開始
        if (TryGetComponent(out PlayerHealth health))
        {
            health.SetInvincible(true);
        }
    }

    // ──────────────────────────────────────────────
    // 更新処理
    // ──────────────────────────────────────────────

    private void UpdateGroundCheck()
    {
        _isGrounded = _characterController.isGrounded;

        if (_isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
        }
    }

    private void UpdateDodge()
    {
        if (!_isDodging)
        {
            // クールダウン減少
            if (_dodgeCooldownTimer > 0f)
            {
                _dodgeCooldownTimer -= Time.deltaTime;
            }
            return;
        }

        _dodgeTimer += Time.deltaTime;

        // 回避移動
        Vector3 dodgeMovement = _dodgeDirection * (_dodgeDistance / _dodgeDuration) * Time.deltaTime;

        // 重力も適用
        _velocity.y += GRAVITY * Time.deltaTime;
        dodgeMovement.y = _velocity.y * Time.deltaTime;

        _characterController.Move(dodgeMovement);

        // 無敵時間終了チェック
        if (!_dodgeInvincibilityEnded && _dodgeTimer >= _dodgeDuration * _dodgeInvincibleRatio)
        {
            _dodgeInvincibilityEnded = true;
            if (TryGetComponent(out PlayerHealth health))
            {
                health.SetInvincible(false);
            }
        }

        // 回避終了チェック
        if (_dodgeTimer >= _dodgeDuration)
        {
            _isDodging = false;
            _dodgeCooldownTimer = _dodgeCooldown;

            // 安全のため無敵を確実に解除
            if (!_dodgeInvincibilityEnded)
            {
                _dodgeInvincibilityEnded = true;
                if (TryGetComponent(out PlayerHealth health))
                {
                    health.SetInvincible(false);
                }
            }
        }
    }

    private void UpdateMovement()
    {
        // 回避中は UpdateDodge で移動を処理する
        if (_isDodging) return;

        Vector2 moveInput = _inputHandler.MoveInput;
        bool isSprinting = _inputHandler.IsSprinting;

        // 攻撃中は移動不可
        if (_isAttacking)
        {
            // 重力だけ適用
            _velocity.y += GRAVITY * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
            return;
        }

        // カメラ相対の移動方向を計算
        Vector3 moveDirection = Vector3.zero;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
            Vector3 right = _cameraTransform != null ? _cameraTransform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = forward * moveInput.y + right * moveInput.x;
            moveDirection.Normalize();

            // キャラクターの回転
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // 速度計算
        float speed = _moveSpeed * (isSprinting ? _sprintMultiplier : 1f);
        Vector3 horizontalMovement = moveDirection * speed;

        // 重力
        _velocity.y += GRAVITY * Time.deltaTime;

        // 最終移動
        Vector3 finalMovement = horizontalMovement * Time.deltaTime + _velocity * Time.deltaTime;
        _characterController.Move(finalMovement);
    }

    private void UpdateAnimator()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        bool isSprinting = _inputHandler.IsSprinting;

        float speed = _isAttacking ? 0f : new Vector2(moveInput.x, moveInput.y).magnitude;
        if (isSprinting && speed > 0.01f)
        {
            speed *= _sprintMultiplier;
        }

        _animator.SetFloat(ANIM_SPEED, speed, 0.1f, Time.deltaTime);
        _animator.SetBool(ANIM_IS_GROUNDED, _isGrounded);
        _animator.SetFloat(ANIM_VERTICAL_VELOCITY, _velocity.y);
    }

    private void UpdateState()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        bool isSprinting = _inputHandler.IsSprinting;

        if (_isDodging)
        {
            CurrentState = PlayerState.Dodge;
        }
        else if (_isAttacking)
        {
            CurrentState = PlayerState.Attack;
        }
        else if (!_isGrounded)
        {
            CurrentState = PlayerState.Jump;
        }
        else if (moveInput.sqrMagnitude > 0.01f)
        {
            CurrentState = isSprinting ? PlayerState.Sprint : PlayerState.Move;
        }
        else
        {
            CurrentState = PlayerState.Idle;
        }
    }

    // ──────────────────────────────────────────────
    // 内部ヘルパー
    // ──────────────────────────────────────────────

    private void SetAttackPhase(AttackPhase phase)
    {
        if (CurrentAttackPhase == phase) return;

        CurrentAttackPhase = phase;
        OnAttackPhaseChanged?.Invoke(phase);
    }
}
