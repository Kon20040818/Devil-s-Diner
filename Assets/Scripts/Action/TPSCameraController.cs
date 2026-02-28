// ============================================================
// TPSCameraController.cs
// シンプルなTPSカメラ。ターゲットの周囲をマウス操作で回転する。
// 障害物判定は行わないプロトタイプ実装。
// ============================================================

using UnityEngine;

/// <summary>
/// Third-person camera that orbits around a target using mouse input.
/// Attach to the Main Camera and assign the player transform as the target.
/// </summary>
public sealed class TPSCameraController : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────

    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Orbit Settings")]
    [SerializeField] private float _distance = 5f;
    [SerializeField] private float _heightOffset = 1.5f;

    [Header("Mouse Input")]
    [SerializeField] private float _mouseSensitivity = 3f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 60f;

    [Header("Smoothing")]
    [SerializeField] private float _smoothSpeed = 10f;

    // ── Private State ─────────────────────────────────────────

    private float _yaw;
    private float _pitch;

    // ── Unity Callbacks ───────────────────────────────────────

    private void Awake()
    {
        if (_target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }

        // Initialise yaw/pitch from the current camera orientation so the
        // view doesn't snap on start.
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;

        // Normalise pitch into the -180..180 range for correct clamping.
        if (_pitch > 180f)
        {
            _pitch -= 360f;
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        // --- Read mouse input ---
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * _mouseSensitivity;
        _pitch -= mouseY * _mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        // --- Calculate desired position ---
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_distance);

        Vector3 targetPoint = _target.position + Vector3.up * _heightOffset;
        Vector3 desiredPosition = targetPoint + offset;

        // --- Smooth follow ---
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            _smoothSpeed * Time.deltaTime
        );

        // --- Always look at the target (with height offset) ---
        transform.LookAt(targetPoint);
    }
}
