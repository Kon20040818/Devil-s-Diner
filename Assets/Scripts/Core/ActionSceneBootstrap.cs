// ============================================================
// ActionSceneBootstrap.cs
// ActionScene のブートストラップ。シーンロード時にスクリプト間の
// 参照を自動で結線し、Inspector での手動アタッチを削減する。
// ============================================================
using System.Reflection;
using UnityEngine;

/// <summary>
/// ActionScene 起動時に各コンポーネント間の参照を自動結線するブートストラップ。
/// シーン内の GameObject にアタッチして使用する。
/// </summary>
public sealed class ActionSceneBootstrap : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        WireAll();
    }

    // ──────────────────────────────────────────────
    // 結線メイン
    // ──────────────────────────────────────────────

    private void WireAll()
    {
        // コンポーネント検索
        ActionHUD hud = FindFirstObjectByType<ActionHUD>();
        JustInputAction justInputAction = FindFirstObjectByType<JustInputAction>();
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        WeaponColliderHandler weaponColliderHandler = FindFirstObjectByType<WeaponColliderHandler>();

        // ── 1. ActionHUD → JustInputAction ──
        if (hud != null && justInputAction != null)
        {
            TryWireField(hud, "_justInputAction", justInputAction);
        }
        else
        {
            if (hud == null)
                Debug.LogWarning("[ActionSceneBootstrap] ActionHUD が見つかりません。");
            if (justInputAction == null)
                Debug.LogWarning("[ActionSceneBootstrap] JustInputAction が見つかりません。");
        }

        // ── 2. PlayerController 確認 ──
        if (playerController != null)
        {
            Debug.Log("[ActionSceneBootstrap] PlayerController を検出しました。" +
                      " (PlayerInputHandler は RequireComponent で自動アタッチ済み)");
        }
        else
        {
            Debug.LogWarning("[ActionSceneBootstrap] PlayerController が見つかりません。");
        }

        // ── 3. WeaponColliderHandler → PlayerController, JustInputAction ──
        if (weaponColliderHandler != null)
        {
            if (playerController != null)
            {
                TryWireField(weaponColliderHandler, "_playerController", playerController);
            }

            if (justInputAction != null)
            {
                TryWireField(weaponColliderHandler, "_justInputAction", justInputAction);
            }
        }
        else
        {
            Debug.LogWarning("[ActionSceneBootstrap] WeaponColliderHandler が見つかりません。");
        }

        // ── 4. PlayerController → Camera.main.transform ──
        if (playerController != null)
        {
            if (Camera.main != null)
            {
                TryWireField(playerController, "_cameraTransform", Camera.main.transform);
            }
            else
            {
                Debug.LogWarning("[ActionSceneBootstrap] Camera.main が見つかりません。" +
                                 " PlayerController のカメラ参照を結線できません。");
            }
        }

        // ── 5. SkillEffectApplier（スキル効果の実適用） ──
        if (FindFirstObjectByType<SkillEffectApplier>() == null)
        {
            gameObject.AddComponent<SkillEffectApplier>();
        }

        // ── 5b. ComboManager 結線 ──
        ComboManager comboManager = FindFirstObjectByType<ComboManager>();
        if (comboManager != null)
        {
            if (justInputAction != null)
            {
                TryWireField(comboManager, "_justInputAction", justInputAction);
            }

            PlayerHealth ph = playerController != null
                ? playerController.GetComponent<PlayerHealth>()
                : null;
            if (ph != null)
            {
                TryWireField(comboManager, "_playerHealth", ph);
            }
        }

        // ── 5c. ActionHUD → ComboManager 結線 ──
        if (hud != null && comboManager != null)
        {
            TryWireField(hud, "_comboManager", comboManager);
        }

        // ── 6. WeaponShopUI の装備復元 ──
        if (playerController != null && !string.IsNullOrEmpty(WeaponShopUI.LastEquippedWeaponId))
        {
            WeaponData[] allWeapons = Resources.LoadAll<WeaponData>("");
            foreach (WeaponData w in allWeapons)
            {
                if (w != null && w.Id == WeaponShopUI.LastEquippedWeaponId)
                {
                    playerController.EquipWeapon(w);
                    break;
                }
            }
        }

        Debug.Log("[ActionSceneBootstrap] Auto-wired: ActionHUD, WeaponColliderHandler, PlayerController camera, SkillEffectApplier, ComboManager");
    }

    // ──────────────────────────────────────────────
    // リフレクション結線ヘルパー
    // ──────────────────────────────────────────────

    /// <summary>
    /// リフレクションで対象オブジェクトのプライベートフィールドに値を設定する。
    /// 既に値が設定済み（非 null）の場合は上書きしない。
    /// </summary>
    /// <typeparam name="T">設定する値の型。</typeparam>
    /// <param name="target">フィールドを持つオブジェクト。</param>
    /// <param name="fieldName">プライベートフィールド名。</param>
    /// <param name="value">設定する値。</param>
    /// <returns>値を設定した場合は true、スキップまたは失敗した場合は false。</returns>
    private static bool TryWireField<T>(object target, string fieldName, T value) where T : class
    {
        if (target == null || value == null) return false;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogWarning($"[ActionSceneBootstrap] {target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            return false;
        }

        if (field.GetValue(target) != null) return false;

        field.SetValue(target, value);
        return true;
    }
}
