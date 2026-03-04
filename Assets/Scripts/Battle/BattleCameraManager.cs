// ============================================================
// BattleCameraManager.cs
// 崩壊スターレイル風バトルカメラシステム。
// 7つのカメラモード + EaseInOut遷移 + カメラシェイク + スローモーション。
// Cinemachine未導入時はフォールバック手動カメラで完全動作する。
// ============================================================
using UnityEngine;
using System.Collections;
#if USE_CINEMACHINE
using Unity.Cinemachine;
#endif

/// <summary>
/// スターレイル準拠のバトルカメラマネージャー。
/// 7つのカメラモードをイージング付きで滑らかに遷移する。
/// </summary>
public sealed class BattleCameraManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // カメラモード
    // ──────────────────────────────────────────────

    public enum CameraMode
    {
        Overview,
        TurnStart,
        BasicAttack,
        SkillExecution,
        UltimateCinematic,
        EnemyAction,
        Victory,
        Defeat
    }

    // ──────────────────────────────────────────────
    // 定数 — カメラ位置パラメータ
    // ──────────────────────────────────────────────

    // Overview: 3/4ビュー
    private static readonly Vector3 OVERVIEW_OFFSET = new Vector3(2.0f, 5.0f, -8.0f);
    private static readonly Vector3 OVERVIEW_LOOK_OFFSET = new Vector3(0f, 1.0f, 2.0f);
    private const float OVERVIEW_FOV = 42f;

    // TurnStart: 左肩越し斜め後方（スターレイル準拠）
    private const float TURNSTART_BACK = 4.0f;
    private const float TURNSTART_RIGHT = 2.5f;
    private const float TURNSTART_UP = 0.8f;
    private const float TURNSTART_LOOK_AHEAD = 6.0f;
    private const float TURNSTART_LOOK_UP = 1.4f;
    private const float TURNSTART_FOV = 50f;

    // BasicAttack
    private const float BASIC_ATTACK_SIDE = 2.5f;
    private const float BASIC_ATTACK_UP = 1.5f;
    private const float BASIC_ATTACK_BACK = 2.0f;
    private const float BASIC_ATTACK_FOV = 45f;

    // Skill
    private const float SKILL_SIDE = 3.5f;
    private const float SKILL_UP = 1.2f;
    private const float SKILL_BACK = 1.0f;
    private const float SKILL_DUTCH_ANGLE = 8f;
    private const float SKILL_FOV = 38f;

    // Ultimate
    private const float ULTIMATE_CLOSE_BACK = 1.5f;
    private const float ULTIMATE_CLOSE_UP = 1.8f;
    private const float ULTIMATE_CLOSE_RIGHT = 0.3f;
    private const float ULTIMATE_FOV_CLOSE = 32f;
    private const float ULTIMATE_DUTCH = 5f;

    // Enemy
    private static readonly Vector3 ENEMY_ACTION_OFFSET = new Vector3(0f, 6.0f, -10.0f);
    private static readonly Vector3 ENEMY_ACTION_LOOK = new Vector3(0f, 1.2f, 0f);
    private const float ENEMY_ACTION_FOV = 48f;

    // Victory
    private static readonly Vector3 VICTORY_OFFSET = new Vector3(0f, 2.5f, 6.0f);
    private const float VICTORY_FOV = 40f;
    private const float VICTORY_ORBIT_SPEED = 12f;

    // Defeat
    private static readonly Vector3 DEFEAT_OFFSET = new Vector3(0f, 8.0f, -5.0f);
    private const float DEFEAT_FOV = 50f;
    private const float DEFEAT_ZOOM_OUT_SPEED = 0.3f;

    // ──────────────────────────────────────────────
    // 定数 — 遷移タイミング
    // ──────────────────────────────────────────────

    private const float TRANSITION_OVERVIEW = 0.6f;
    private const float TRANSITION_TURN_START = 0.5f;
    private const float TRANSITION_BASIC_ATTACK = 0.25f;
    private const float TRANSITION_SKILL = 0.35f;
    private const float TRANSITION_ULTIMATE = 0.3f;
    private const float TRANSITION_ENEMY = 0.35f;
    private const float TRANSITION_VICTORY = 1.0f;
    private const float TRANSITION_DEFEAT = 1.5f;

    // ──────────────────────────────────────────────
    // シェイクプリセット (amplitude, frequency, duration)
    // ──────────────────────────────────────────────

    public static readonly Vector3 SHAKE_BASIC_HIT = new Vector3(0.15f, 25f, 0.12f);
    public static readonly Vector3 SHAKE_SKILL_HIT = new Vector3(0.35f, 20f, 0.2f);
    public static readonly Vector3 SHAKE_ULTIMATE_IMPACT = new Vector3(0.7f, 15f, 0.25f);
    public static readonly Vector3 SHAKE_ENEMY_HIT = new Vector3(0.08f, 25f, 0.1f);
    public static readonly Vector3 SHAKE_BREAK = new Vector3(0.5f, 14f, 0.25f);

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

#if USE_CINEMACHINE
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera _vcamOverview;
    [SerializeField] private CinemachineCamera _vcamActiveCharacter;
    [SerializeField] private CinemachineCamera _vcamActionTarget;
#endif

    [Header("フォールバック設定")]
    [SerializeField] private Transform _overviewPosition;
    [SerializeField] private Vector3 _overviewOffset = OVERVIEW_OFFSET;
    [SerializeField] private Vector3 _overviewLookOffset = OVERVIEW_LOOK_OFFSET;
    [SerializeField] private float _followSpeed = 5f;

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private CameraMode _currentMode = CameraMode.Overview;
    private Transform _focusTarget;
    private Transform _actionAttacker;
    private Transform _actionTarget;
    private Vector3 _fieldCenter = Vector3.zero;

    // 遷移
    private Vector3 _transStartPos;
    private Quaternion _transStartRot;
    private float _transStartFOV;
    private Vector3 _transEndPos;
    private Quaternion _transEndRot;
    private float _transEndFOV;
    private float _transDuration;
    private float _transElapsed;
    private bool _isTransitioning;

    // シェイク
    private Vector3 _shakeOffset;
    private Coroutine _shakeCoroutine;

    // 勝利軌道
    private float _victoryOrbitAngle;
    // 敗北ズーム
    private float _defeatZoomProgress;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    public CameraMode CurrentMode => _currentMode;

    // ──────────────────────────────────────────────
    // 公開 API — カメラ切替
    // ──────────────────────────────────────────────

    public void SetFieldCenter(Vector3 center) => _fieldCenter = center;

    /// <summary>Overview: 全体俯瞰。</summary>
    public void SwitchToOverview()
    {
        _currentMode = CameraMode.Overview;
        Vector3 pos = GetOverviewPos();
        Vector3 look = GetOverviewLook();
        BeginTransition(pos, Quaternion.LookRotation(look - pos), OVERVIEW_FOV, TRANSITION_OVERVIEW);
    }

    /// <summary>TurnStart: 肩越しフォーカス。</summary>
    public void FocusOnCharacter(Transform character)
    {
        if (character == null) return;
        _focusTarget = character;
        _currentMode = CameraMode.TurnStart;
        Vector3 pos = CalcTurnStartPos(character);
        Vector3 look = CalcTurnStartLook(character);
        BeginTransition(pos, Quaternion.LookRotation(look - pos), TURNSTART_FOV, TRANSITION_TURN_START);
    }

    /// <summary>BasicAttack: 通常攻撃カメラ。</summary>
    public void SwitchToActionCamera(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) return;
        _actionAttacker = attacker;
        _actionTarget = target;
        _currentMode = CameraMode.BasicAttack;
        Vector3 pos = CalcBasicAttackPos(attacker, target);
        Vector3 look = target.position + Vector3.up * 1.2f;
        BeginTransition(pos, Quaternion.LookRotation(look - pos), BASIC_ATTACK_FOV, TRANSITION_BASIC_ATTACK);
    }

    /// <summary>SkillExecution: スキル発動カメラ（ダッチアングル付き）。</summary>
    public void SwitchToSkillCamera(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) return;
        _actionAttacker = attacker;
        _actionTarget = target;
        _currentMode = CameraMode.SkillExecution;
        Vector3 pos = CalcSkillPos(attacker, target);
        Vector3 look = (attacker.position + target.position) * 0.5f + Vector3.up * 1.0f;
        Quaternion rot = Quaternion.LookRotation(look - pos);
        Vector3 e = rot.eulerAngles;
        e.z = SKILL_DUTCH_ANGLE;
        BeginTransition(pos, Quaternion.Euler(e), SKILL_FOV, TRANSITION_SKILL);
    }

    /// <summary>UltimateCinematic Phase1: クローズアップ。</summary>
    public void SwitchToUltimateCamera(Transform character, Transform target)
    {
        if (character == null) return;
        _actionAttacker = character;
        _actionTarget = target;
        _currentMode = CameraMode.UltimateCinematic;

        Vector3 back = -character.forward;
        Vector3 right = character.right;
        Vector3 pos = character.position + back * ULTIMATE_CLOSE_BACK + right * ULTIMATE_CLOSE_RIGHT + Vector3.up * ULTIMATE_CLOSE_UP;
        Vector3 look = character.position + Vector3.up * 1.5f;
        Quaternion rot = Quaternion.LookRotation(look - pos);
        Vector3 e = rot.eulerAngles;
        e.z = ULTIMATE_DUTCH;
        BeginTransition(pos, Quaternion.Euler(e), ULTIMATE_FOV_CLOSE, TRANSITION_ULTIMATE);
    }

    /// <summary>UltimateCinematic Phase2: アクションワイド。</summary>
    public void SwitchToUltimateActionCamera(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) return;
        _actionAttacker = attacker;
        _actionTarget = target;

        Vector3 mid = (attacker.position + target.position) * 0.5f;
        Vector3 dir = (target.position - attacker.position).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 pos = mid + side * 4.0f + Vector3.up * 2.5f - dir * 2.0f;
        Vector3 look = mid + Vector3.up * 1.0f;
        Quaternion rot = Quaternion.LookRotation(look - pos);
        Vector3 e = rot.eulerAngles;
        e.z = -ULTIMATE_DUTCH;
        BeginTransition(pos, Quaternion.Euler(e), 44f, 0.4f);
    }

    /// <summary>EnemyAction: 敵ターンワイドカメラ。</summary>
    public void SwitchToEnemyCamera(Transform enemy, Transform playerTarget)
    {
        if (enemy == null) return;
        _actionAttacker = enemy;
        _actionTarget = playerTarget;
        _currentMode = CameraMode.EnemyAction;
        Vector3 center = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
        Vector3 pos = center + ENEMY_ACTION_OFFSET;
        Vector3 look = center + ENEMY_ACTION_LOOK;
        BeginTransition(pos, Quaternion.LookRotation(look - pos), ENEMY_ACTION_FOV, TRANSITION_ENEMY);
    }

    /// <summary>Victory: 勝利カメラ（軌道回転付き）。</summary>
    public void SwitchToVictoryCamera()
    {
        _currentMode = CameraMode.Victory;
        _victoryOrbitAngle = 0f;
        Vector3 center = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
        Vector3 pos = center + VICTORY_OFFSET;
        Vector3 look = center + Vector3.up * 1.5f;
        BeginTransition(pos, Quaternion.LookRotation(look - pos), VICTORY_FOV, TRANSITION_VICTORY);
    }

    /// <summary>Defeat: 敗北カメラ（ズームアウト付き）。</summary>
    public void SwitchToDefeatCamera()
    {
        _currentMode = CameraMode.Defeat;
        _defeatZoomProgress = 0f;
        Vector3 center = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
        Vector3 pos = center + DEFEAT_OFFSET;
        Vector3 look = center + Vector3.up * 0.5f;
        BeginTransition(pos, Quaternion.LookRotation(look - pos), DEFEAT_FOV, TRANSITION_DEFEAT);
    }

    // ──────────────────────────────────────────────
    // 公開 API — シェイク
    // ──────────────────────────────────────────────

    /// <summary>プリセットベクター (amplitude, frequency, duration) でシェイク。</summary>
    public void ShakeCamera(Vector3 preset)
    {
        ShakeCamera(preset.x, preset.z, preset.y);
    }

    /// <summary>カメラシェイク。</summary>
    public void ShakeCamera(float intensity = 0.3f, float duration = 0.2f, float frequency = 20f)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration, frequency));
    }

    /// <summary>スローモーション。</summary>
    public void SlowMotion(float timeScale = 0.3f, float duration = 0.2f)
    {
        StartCoroutine(SlowMotionRoutine(timeScale, duration));
    }

    // ──────────────────────────────────────────────
    // 内部 — 遷移
    // ──────────────────────────────────────────────

    private void BeginTransition(Vector3 endPos, Quaternion endRot, float endFOV, float dur)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        _transStartPos = cam.transform.position - _shakeOffset;
        _transStartRot = cam.transform.rotation;
        _transStartFOV = cam.fieldOfView;
        _transEndPos = endPos;
        _transEndRot = endRot;
        _transEndFOV = endFOV;
        _transDuration = Mathf.Max(dur, 0.01f);
        _transElapsed = 0f;
        _isTransitioning = true;
    }

    // ──────────────────────────────────────────────
    // LateUpdate
    // ──────────────────────────────────────────────

    private void LateUpdate()
    {
#if USE_CINEMACHINE
        if (_shakeOffset != Vector3.zero)
        {
            Camera c = Camera.main;
            if (c != null) c.transform.position += _shakeOffset;
        }
        return;
#else
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        Transform ct = mainCam.transform;

        if (_isTransitioning)
        {
            _transElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_transElapsed / _transDuration);
            float e = EaseInOutCubic(t);

            ct.position = Vector3.Lerp(_transStartPos, _transEndPos, e);
            ct.rotation = Quaternion.Slerp(_transStartRot, _transEndRot, e);
            mainCam.fieldOfView = Mathf.Lerp(_transStartFOV, _transEndFOV, e);

            if (t >= 1f) _isTransitioning = false;
        }
        else
        {
            switch (_currentMode)
            {
                case CameraMode.Overview:
                    float breathe = Mathf.Sin(Time.time * 0.5f) * 0.02f;
                    ct.position += new Vector3(0f, breathe, 0f);
                    break;

                case CameraMode.TurnStart:
                    if (_focusTarget != null)
                    {
                        Vector3 ip = CalcTurnStartPos(_focusTarget);
                        Vector3 il = CalcTurnStartLook(_focusTarget);
                        ct.position = Vector3.Lerp(ct.position, ip, Time.deltaTime * _followSpeed * 0.5f);
                        ct.rotation = Quaternion.Slerp(ct.rotation,
                            Quaternion.LookRotation(il - ct.position), Time.deltaTime * _followSpeed * 0.5f);
                    }
                    break;

                case CameraMode.Victory:
                    _victoryOrbitAngle += VICTORY_ORBIT_SPEED * Time.deltaTime;
                    Vector3 vc = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
                    float vr = _victoryOrbitAngle * Mathf.Deg2Rad;
                    Vector3 vp = vc + new Vector3(Mathf.Sin(vr) * VICTORY_OFFSET.z, VICTORY_OFFSET.y, Mathf.Cos(vr) * VICTORY_OFFSET.z);
                    ct.position = Vector3.Lerp(ct.position, vp, Time.deltaTime * 2f);
                    ct.rotation = Quaternion.Slerp(ct.rotation,
                        Quaternion.LookRotation(vc + Vector3.up * 1.5f - ct.position), Time.deltaTime * 2f);
                    break;

                case CameraMode.Defeat:
                    _defeatZoomProgress += Time.deltaTime * DEFEAT_ZOOM_OUT_SPEED;
                    Vector3 dc = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
                    Vector3 dp = dc + DEFEAT_OFFSET + new Vector3(0f, _defeatZoomProgress * 2f, -_defeatZoomProgress * 1.5f);
                    ct.position = Vector3.Lerp(ct.position, dp, Time.deltaTime * 1.5f);
                    ct.rotation = Quaternion.Slerp(ct.rotation,
                        Quaternion.LookRotation(dc + Vector3.up * 0.5f - ct.position), Time.deltaTime * 1.5f);
                    break;
            }
        }

        ct.position += _shakeOffset;
#endif
    }

    // ──────────────────────────────────────────────
    // 位置計算
    // ──────────────────────────────────────────────

    private Vector3 GetOverviewPos()
    {
        Vector3 c = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
        return c + _overviewOffset;
    }

    private Vector3 GetOverviewLook()
    {
        Vector3 c = _overviewPosition != null ? _overviewPosition.position : _fieldCenter;
        return c + _overviewLookOffset;
    }

    private static Vector3 CalcTurnStartPos(Transform ch)
    {
        return ch.position - ch.forward * TURNSTART_BACK + ch.right * TURNSTART_RIGHT + Vector3.up * TURNSTART_UP;
    }

    private static Vector3 CalcTurnStartLook(Transform ch)
    {
        return ch.position + ch.forward * TURNSTART_LOOK_AHEAD + Vector3.up * TURNSTART_LOOK_UP;
    }

    private static Vector3 CalcBasicAttackPos(Transform atk, Transform tgt)
    {
        Vector3 d = (tgt.position - atk.position).normalized;
        Vector3 s = Vector3.Cross(Vector3.up, d).normalized;
        return atk.position + s * BASIC_ATTACK_SIDE + Vector3.up * BASIC_ATTACK_UP - d * BASIC_ATTACK_BACK;
    }

    private static Vector3 CalcSkillPos(Transform atk, Transform tgt)
    {
        Vector3 d = (tgt.position - atk.position).normalized;
        Vector3 s = Vector3.Cross(Vector3.up, d).normalized;
        return atk.position + s * SKILL_SIDE + Vector3.up * SKILL_UP - d * SKILL_BACK;
    }

    // ──────────────────────────────────────────────
    // コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator ShakeRoutine(float amp, float dur, float freq)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - elapsed / dur;
            float ca = amp * decay;
            float ph = elapsed * freq;
            float x = Mathf.Sin(ph * 1.1f) * ca + Random.Range(-1f, 1f) * ca * 0.3f;
            float y = Mathf.Cos(ph * 0.9f) * ca + Random.Range(-1f, 1f) * ca * 0.3f;
            _shakeOffset = new Vector3(x, y, 0f);
            yield return null;
        }
        _shakeOffset = Vector3.zero;
        _shakeCoroutine = null;
    }

    private IEnumerator SlowMotionRoutine(float targetScale, float dur)
    {
        float orig = Time.timeScale;
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = 0.02f * targetScale;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = orig;
        Time.fixedDeltaTime = 0.02f * orig;
    }

    // ──────────────────────────────────────────────
    // イージング
    // ──────────────────────────────────────────────

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
