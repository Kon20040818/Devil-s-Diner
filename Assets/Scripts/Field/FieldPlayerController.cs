// ============================================================
// FieldPlayerController.cs
// CharacterController ベースの TPS プレイヤー移動。
// カメラの forward/right を基準にした相対移動を行う。
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// フィールドシーンでのプレイヤー移動を制御するコンポーネント。
/// CharacterController + Input System で WASD/左スティック移動、
/// カメラの向きを基準に方向を算出する。
/// </summary>
public sealed class FieldPlayerController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("移動パラメータ")]
    [Tooltip("歩行速度（m/s）")]
    [SerializeField] private float _walkSpeed = 5f;

    [Tooltip("ダッシュ速度（m/s）")]
    [SerializeField] private float _sprintSpeed = 8f;

    [Tooltip("回転スムージング時間（秒）")]
    [SerializeField] private float _rotationSmoothTime = 0.12f;

    [Tooltip("重力加速度")]
    [SerializeField] private float _gravity = -15f;

    [Header("Input")]
    [Tooltip("InputActionAsset（InputSystem_Actions）")]
    [SerializeField] private InputActionAsset _inputActions;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private CharacterController _controller;
    private Transform _cameraTransform;

    private InputAction _moveAction;
    private InputAction _sprintAction;

    private float _verticalVelocity;
    private float _rotationVelocity;

    // ──────────────────────────────────────────────
    // 外部設定
    // ──────────────────────────────────────────────

    /// <summary>カメラ Transform を外部から設定する（Bootstrap 用）。</summary>
    public void SetCameraTransform(Transform cam) => _cameraTransform = cam;

    /// <summary>InputActionAsset を外部から設定する。</summary>
    public void SetInputActions(InputActionAsset asset)
    {
        _inputActions = asset;
        BindActions();
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            _controller = gameObject.AddComponent<CharacterController>();
        }

        if (_cameraTransform == null)
        {
            var mainCam = Camera.main;
            if (mainCam != null) _cameraTransform = mainCam.transform;
        }

        BindActions();
    }

    private void OnEnable()
    {
        _moveAction?.Enable();
        _sprintAction?.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.Disable();
        _sprintAction?.Disable();
    }

    private void Update()
    {
        HandleMovement();
    }

    // ──────────────────────────────────────────────
    // 入力バインド
    // ──────────────────────────────────────────────

    private void BindActions()
    {
        if (_inputActions == null) return;

        var playerMap = _inputActions.FindActionMap("Player");
        if (playerMap == null) return;

        _moveAction = playerMap.FindAction("Move");
        _sprintAction = playerMap.FindAction("Sprint");
    }

    // ──────────────────────────────────────────────
    // 移動処理
    // ──────────────────────────────────────────────

    private void HandleMovement()
    {
        // 入力取得
        Vector2 input = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool isSprinting = _sprintAction != null && _sprintAction.IsPressed();

        // 重力
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        _verticalVelocity += _gravity * Time.deltaTime;

        // カメラ相対方向の算出
        Vector3 moveDir = Vector3.zero;
        if (input.sqrMagnitude > 0.01f && _cameraTransform != null)
        {
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = camForward * input.y + camRight * input.x;
            moveDir.Normalize();

            // キャラクター回転
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle,
                ref _rotationVelocity, _rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        // 速度選択
        float speed = isSprinting ? _sprintSpeed : _walkSpeed;

        // 最終移動ベクトル
        Vector3 velocity = moveDir * speed;
        velocity.y = _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }
}
