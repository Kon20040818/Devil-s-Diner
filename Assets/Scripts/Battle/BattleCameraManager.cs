using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public sealed class BattleCameraManager : MonoBehaviour
{
    public enum CameraMode { Overview, TurnStart, BasicAttack, SkillExecution, UltimateCinematic, EnemyAction, Victory, Defeat }

    public static readonly Vector3 SHAKE_BASIC_HIT       = new Vector3(0.15f, 25f, 0.12f);
    public static readonly Vector3 SHAKE_SKILL_HIT       = new Vector3(0.35f, 20f, 0.2f);
    public static readonly Vector3 SHAKE_ULTIMATE_IMPACT = new Vector3(0.7f, 15f, 0.25f);
    public static readonly Vector3 SHAKE_ENEMY_HIT       = new Vector3(0.08f, 25f, 0.1f);
    public static readonly Vector3 SHAKE_BREAK           = new Vector3(0.5f, 14f, 0.25f);

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera _vcamOverview;
    [SerializeField] private CinemachineCamera _vcamTurnFocus;
    [SerializeField] private CinemachineCamera _vcamBasicAttack;
    [SerializeField] private CinemachineCamera _vcamSkill;
    [SerializeField] private CinemachineCamera _vcamEnemyWide;
    [SerializeField] private CinemachineCamera _vcamUltimateClose;
    [SerializeField] private CinemachineCamera _vcamUltimateWide;
    [SerializeField] private CinemachineCamera _vcamImpact;
    [SerializeField] private CinemachineCamera _vcamVictory;
    [SerializeField] private CinemachineCamera _vcamDefeat;

    [Header("Core Groups")]
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private Transform _overviewPosition;

    private CameraMode _currentMode = CameraMode.Overview;
    private Transform _currentTarget;
    private Coroutine _impactCoroutine;
    private Vector3 _fieldCenter;
    private Transform _fieldCenterAnchor;

    public CameraMode CurrentMode => _currentMode;

    private void Awake()
    {
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
    }

    public void SetFieldCenter(Vector3 center)
    {
        _fieldCenter = center;
        if (!_fieldCenterAnchor) _fieldCenterAnchor = new GameObject("FieldCenterAnchor").transform;
        _fieldCenterAnchor.position = _fieldCenter;
    }

    public void SwitchToOverview()
    {
        _currentMode = CameraMode.Overview;
        var t = _overviewPosition ? _overviewPosition : _fieldCenterAnchor;
        if (t) { _vcamOverview.Follow = t; _vcamOverview.LookAt = t; }
        ActivateVCam(_vcamOverview);
    }

    public void FocusOnCharacter(Transform character)
    {
        if (!character) return;
        _currentMode = CameraMode.TurnStart;
        _vcamTurnFocus.Follow = character;
        _vcamTurnFocus.LookAt = character;
        ActivateVCam(_vcamTurnFocus);
    }

    public void SwitchToActionCamera(Transform attacker, Transform target)
    {
        if (!attacker || !target) return;
        _currentMode = CameraMode.BasicAttack;
        _currentTarget = target;
        _vcamBasicAttack.Follow = attacker;
        _vcamBasicAttack.LookAt = target;
        ActivateVCam(_vcamBasicAttack);
    }

    public void SwitchToSkillCamera(Transform attacker, Transform target)
    {
        if (!attacker || !target) return;
        _currentMode = CameraMode.SkillExecution;
        _currentTarget = target;
        _vcamSkill.Follow = attacker;
        _vcamSkill.LookAt = target;
        ActivateVCam(_vcamSkill);
    }

    public void SwitchToEnemyCamera(Transform enemy, Transform playerTarget)
    {
        if (!enemy) return;
        _currentMode = CameraMode.EnemyAction;
        _currentTarget = playerTarget;
        
        // 敵のターンは、敵を映すのではなく「狙われるプレイヤー越し（斜め受け）」から敵を捉える
        _vcamEnemyWide.Follow = playerTarget;
        _vcamEnemyWide.LookAt = enemy;
        
        ActivateVCam(_vcamEnemyWide);
    }

    public void SwitchToUltimateCamera(Transform character, Transform target)
    {
        if (!character) return;
        _currentMode = CameraMode.UltimateCinematic;
        _currentTarget = target;
        _vcamUltimateClose.Follow = character;
        _vcamUltimateClose.LookAt = character;
        ActivateVCam(_vcamUltimateClose);
    }

    public void SwitchToUltimateActionCamera(Transform attacker, Transform target)
    {
        if (!attacker || !target) return;
        SetupTargetGroup(attacker, target, 1f, 1f);
        ActivateVCam(_vcamUltimateWide);
    }

    public void SwitchToVictoryCamera()
    {
        _currentMode = CameraMode.Victory;
        ActivateVCam(_vcamVictory);
    }

    public void SwitchToDefeatCamera()
    {
        _currentMode = CameraMode.Defeat;
        ActivateVCam(_vcamDefeat);
    }

    public void ShakeCamera(float intensity = 0.3f, float duration = 0.2f, float frequency = 20f)
    {
        if (!_impulseSource) return;
        _impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        _impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), 0f).normalized;
        _impulseSource.GenerateImpulseWithForce(intensity);

        if (intensity >= 0.5f && _currentTarget != null)
        {
            if (_impactCoroutine != null) StopCoroutine(_impactCoroutine);
            _impactCoroutine = StartCoroutine(ImpactFlash(duration));
        }
    }

    public void ShakeCamera(Vector3 preset) => ShakeCamera(preset.x, preset.z, preset.y);

    public void SlowMotion(float timeScale = 0.3f, float duration = 0.2f)
    {
        StartCoroutine(SlowMotionRoutine(timeScale, duration));
    }

    private void ActivateVCam(CinemachineCamera activeCam)
    {
        CinemachineCamera[] allCams = { _vcamOverview, _vcamTurnFocus, _vcamBasicAttack, _vcamSkill, _vcamEnemyWide, _vcamUltimateClose, _vcamUltimateWide, _vcamImpact, _vcamVictory, _vcamDefeat };
        foreach (var cam in allCams) if (cam && cam != activeCam) cam.Priority = 0;
        if (activeCam) { activeCam.PreviousStateIsValid = false; activeCam.Priority = 20; }
    }

    private void SetupTargetGroup(Transform a, Transform b, float weightA, float weightB)
    {
        if (!_targetGroup) return;
        var targets = new List<CinemachineTargetGroup.Target>();
        if (a) targets.Add(new CinemachineTargetGroup.Target { Object = a, Weight = weightA, Radius = 1f });
        if (b) targets.Add(new CinemachineTargetGroup.Target { Object = b, Weight = weightB, Radius = 1f });
        _targetGroup.Targets = targets;
    }

    private IEnumerator ImpactFlash(float duration)
    {
        if (!_vcamImpact || !_currentTarget) yield break;
        _vcamImpact.Follow = _currentTarget;
        _vcamImpact.LookAt = _currentTarget;
        _vcamImpact.PreviousStateIsValid = false;
        _vcamImpact.Priority = 40; 
        yield return new WaitForSecondsRealtime(duration);
        _vcamImpact.Priority = 0;
        _impactCoroutine = null;
    }

    private IEnumerator SlowMotionRoutine(float targetScale, float dur)
    {
        float orig = Time.timeScale;
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = 0.02f * targetScale;
        yield return new WaitForSecondsRealtime(dur);
        Time.timeScale = orig;
        Time.fixedDeltaTime = 0.02f * orig;
    }
}
