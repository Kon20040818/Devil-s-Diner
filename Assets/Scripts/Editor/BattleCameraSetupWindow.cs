#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using System.Collections.Generic;

public static class BattleCameraSetupWindow
{
    [MenuItem("DevilsDiner/Setup Battle Camera")]
    public static void Setup()
    {
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            if (!mainCam.TryGetComponent(out CinemachineBrain brain))
                brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.4f);

            var blends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
            var blendList = new List<CinemachineBlenderSettings.CustomBlend>
            {
                new CinemachineBlenderSettings.CustomBlend { From = "ANY", To = "VCam_Impact", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) },
                new CinemachineBlenderSettings.CustomBlend { From = "ANY", To = "VCam_BasicAttack", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) },
                new CinemachineBlenderSettings.CustomBlend { From = "ANY", To = "VCam_Skill", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) },
                new CinemachineBlenderSettings.CustomBlend { From = "ANY", To = "VCam_UltimateClose", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) },
                new CinemachineBlenderSettings.CustomBlend { From = "VCam_Impact", To = "ANY", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.2f) }
            };

            blends.CustomBlends = blendList.ToArray();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            AssetDatabase.CreateAsset(blends, "Assets/Settings/PersonaCameraCustomBlends.asset");
            AssetDatabase.SaveAssets();
            brain.CustomBlends = blends;
        }

        // 過去のセットアップによる「空のマネージャー」がBattleSystemに残存していると起動時にエラーになるため完全抹消する
        foreach (var oldMgr in Object.FindObjectsByType<BattleCameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(oldMgr);
        }

        var oldRig = GameObject.Find("BattleCameraRig");
        if (oldRig) Object.DestroyImmediate(oldRig);

        var root = new GameObject("BattleCameraRig");
        Undo.RegisterCreatedObjectUndo(root, "Setup Battle Camera");

        var targetGroup = new GameObject("TargetGroup").AddComponent<CinemachineTargetGroup>();
        targetGroup.transform.SetParent(root.transform);
        
        // エディタ上でのプレビュー用にダミーのターゲットを取得
        Transform dummyPlayer = GameObject.Find("Player_Hero")?.transform ?? GameObject.Find("Player")?.transform;
        Transform dummyEnemy = GameObject.Find("Enemy_Slime")?.transform ?? GameObject.Find("Slime")?.transform;
        
        if (dummyPlayer == null || dummyEnemy == null)
        {
            var chars = UnityEngine.Object.FindObjectsOfType<CharacterBattleController>();
            foreach (var c in chars)
            {
                // X座標がマイナスならプレイヤー、プラスなら敵として強制判別する
                if (c.transform.position.x < 0) dummyPlayer = c.transform;
                if (c.transform.position.x > 0) dummyEnemy = c.transform;
            }
        }

        var impulse = new GameObject("ImpulseSource").AddComponent<CinemachineImpulseSource>();
        impulse.transform.SetParent(root.transform);
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulse.ImpulseDefinition.ImpulseDuration = 0.2f;

        var vOverview = CreateFollowVCam(root.transform, "VCam_Overview", 55f, 0f, new Vector3(3f, 8f, -9f), BindingMode.WorldSpace);
        var vTurnFocus = CreateFollowVCam(root.transform, "VCam_TurnFocus", 40f, 3f, new Vector3(2.0f, 0.5f, -2.5f), BindingMode.LockToTargetWithWorldUp);
        var vBasicAttack = CreateFollowVCam(root.transform, "VCam_BasicAttack", 32f, 7f, new Vector3(2.5f, -0.5f, -1.8f), BindingMode.LockToTargetWithWorldUp);
        var vSkill = CreateFollowVCam(root.transform, "VCam_Skill", 28f, -12f, new Vector3(-2.8f, -0.3f, -1.2f), BindingMode.LockToTargetWithWorldUp);
        
        // 敵のターンは「標的となるプレイヤーの肩口ギリギリ（斜め受け）」の超近接望遠視点
        var vEnemyWide = CreateFollowVCam(root.transform, "VCam_EnemyWide", 35f, -8f, new Vector3(-1.2f, 1.4f, -1.8f), BindingMode.LockToTargetWithWorldUp);
        
        // エディタ画面での確認（プレビュー）用に一旦仮組みする（プレイ時はManagerが動的に上書きする）
        if (dummyPlayer && dummyEnemy)
        {
            vTurnFocus.Follow = dummyPlayer; vTurnFocus.LookAt = dummyPlayer;
            vTurnFocus.transform.position = dummyPlayer.position + new Vector3(2.0f, 0.5f, -2.5f);
            vTurnFocus.transform.LookAt(dummyPlayer);

            vBasicAttack.Follow = dummyPlayer; vBasicAttack.LookAt = dummyEnemy;
            vBasicAttack.transform.position = dummyPlayer.position + new Vector3(2.5f, -0.5f, -1.8f);
            vBasicAttack.transform.LookAt(dummyEnemy);

            vSkill.Follow = dummyPlayer; vSkill.LookAt = dummyEnemy;
            vSkill.transform.position = dummyPlayer.position + new Vector3(-2.8f, -0.3f, -1.2f);
            vSkill.transform.LookAt(dummyEnemy);
            
            // 敵ターンの防衛視点（プレイヤーの背後から敵を見る）
            vEnemyWide.Follow = dummyPlayer; vEnemyWide.LookAt = dummyEnemy;
            vEnemyWide.transform.position = dummyPlayer.position + new Vector3(-1.2f, 1.4f, -1.8f);
            vEnemyWide.transform.LookAt(dummyEnemy);
        }
        
        var vUltimateClose = CreateFollowVCam(root.transform, "VCam_UltimateClose", 22f, 15f, new Vector3(-0.8f, -1.0f, -0.8f), BindingMode.LockToTargetWithWorldUp);
        var vUltimateWide = CreateFollowVCam(root.transform, "VCam_UltimateWide", 40f, 0f, new Vector3(0f, 4f, -12f), BindingMode.WorldSpace);
        vUltimateWide.Follow = targetGroup.transform; vUltimateWide.LookAt = targetGroup.transform;
        
        var vImpact = CreateFollowVCam(root.transform, "VCam_Impact", 20f, -18f, new Vector3(1.5f, -0.8f, -0.5f), BindingMode.LockToTargetWithWorldUp);
        var vVictory = CreateFollowVCam(root.transform, "VCam_Victory", 45f, 0f, new Vector3(0,5,-6), BindingMode.WorldSpace);
        var vDefeat = CreateFollowVCam(root.transform, "VCam_Defeat", 45f, 0f, new Vector3(0,8,-8), BindingMode.WorldSpace);

        var mgr = root.AddComponent<BattleCameraManager>();
        var so = new SerializedObject(mgr);
        so.FindProperty("_vcamOverview").objectReferenceValue = vOverview;
        so.FindProperty("_vcamTurnFocus").objectReferenceValue = vTurnFocus;
        so.FindProperty("_vcamBasicAttack").objectReferenceValue = vBasicAttack;
        so.FindProperty("_vcamSkill").objectReferenceValue = vSkill;
        so.FindProperty("_vcamEnemyWide").objectReferenceValue = vEnemyWide;
        so.FindProperty("_vcamUltimateClose").objectReferenceValue = vUltimateClose;
        so.FindProperty("_vcamUltimateWide").objectReferenceValue = vUltimateWide;
        so.FindProperty("_vcamImpact").objectReferenceValue = vImpact;
        so.FindProperty("_vcamVictory").objectReferenceValue = vVictory;
        so.FindProperty("_vcamDefeat").objectReferenceValue = vDefeat;
        so.FindProperty("_targetGroup").objectReferenceValue = targetGroup;
        so.FindProperty("_impulseSource").objectReferenceValue = impulse;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        Debug.Log("BattleCameraRig Setup Complete!");
    }

    private static CinemachineCamera CreateFollowVCam(Transform root, string name, float fov, float dutch, Vector3 offset, BindingMode mode)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        var vcam = go.AddComponent<CinemachineCamera>();
        var lens = vcam.Lens; lens.FieldOfView = fov; lens.Dutch = dutch; vcam.Lens = lens;
        vcam.Priority = 0;

        var follow = go.AddComponent<CinemachineFollow>();
        follow.TrackerSettings.BindingMode = mode;
        follow.FollowOffset = offset;
        follow.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);

        var rot = go.AddComponent<CinemachineRotationComposer>();
        rot.Damping = new Vector2(0.5f, 0.5f);

        var listener = go.AddComponent<CinemachineImpulseListener>();
        listener.Use2DDistance = false;
        
        return vcam;
    }
}
#endif
