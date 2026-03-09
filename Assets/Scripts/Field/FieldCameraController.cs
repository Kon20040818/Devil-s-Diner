// ============================================================
// FieldCameraController.cs
// Cinemachine 3.x ベースの TPS 肩越しカメラ制御。
// マウス / 右スティックの Look 入力で回転リグを操作する。
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// フィールドシーンの TPS カメラを制御するコンポーネント。
/// CinemachineCamera の Follow/LookAt ターゲットとなる
/// 回転リグ（空 Transform）を Look 入力で回転させる方式。
/// </summary>
public sealed class FieldCameraController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("Cinemachine")]
    [Tooltip("フィールド用の CinemachineCamera")]
    [SerializeField] private CinemachineCamera _vcam;

    [Header("Look 感度")]
    [Tooltip("マウス / 右スティックの感度")]
    [SerializeField] private float _lookSensitivity = 0.15f;

    [Tooltip("ピッチ上限角度")]
    [SerializeField] private float _maxPitchAngle = 70f;

    [Tooltip("ピッチ下限角度")]
    [SerializeField] private float _minPitchAngle = -30f;

    [Header("Input")]
    [Tooltip("InputActionAsset（InputSystem_Actions）")]
    [SerializeField] private InputActionAsset _inputActions;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private Transform _followTarget;
    private InputAction _lookAction;
    private float _yaw;
    private float _pitch;

    // ──────────────────────────────────────────────
    // 外部設定
    // ──────────────────────────────────────────────

    /// <summary>
    /// プレイヤーを追従対象に設定する。
    /// 回転リグをプレイヤーの子として生成し、CinemachineCamera に結線する。
    /// </summary>
    public void SetTarget(Transform player)
    {
        if (player == null) return;

        // 回転リグ生成（プレイヤーの子）
        var rigGO = new GameObject("CameraFollowTarget");
        rigGO.transform.SetParent(player);
        rigGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        rigGO.transform.localRotation = Quaternion.identity;
        _followTarget = rigGO.transform;

        // Cinemachine 結線
        if (_vcam != null)
        {
            _vcam.Follow = _followTarget;
            _vcam.LookAt = _followTarget;
        }

        // 初期ヨーをプレイヤーの向きに合わせる
        _yaw = player.eulerAngles.y;
    }

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
        BindActions();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        _lookAction?.Enable();
    }

    private void OnDisable()
    {
        _lookAction?.Disable();
    }

    private void LateUpdate()
    {
        HandleLook();
    }

    // ──────────────────────────────────────────────
    // 入力バインド
    // ──────────────────────────────────────────────

    private void BindActions()
    {
        if (_inputActions == null) return;

        var playerMap = _inputActions.FindActionMap("Player");
        if (playerMap == null) return;

        _lookAction = playerMap.FindAction("Look");
    }

    // ──────────────────────────────────────────────
    // Look 処理
    // ──────────────────────────────────────────────

    private void HandleLook()
    {
        if (_followTarget == null) return;

        Vector2 lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

        _yaw += lookInput.x * _lookSensitivity;
        _pitch -= lookInput.y * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, _minPitchAngle, _maxPitchAngle);

        _followTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
