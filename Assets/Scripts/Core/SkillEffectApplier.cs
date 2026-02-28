// ============================================================
// SkillEffectApplier.cs
// ActionScene のプレイヤー生成時にスキル効果を実ゲームロジックへ反映する。
// ActionSceneBootstrap と同じ GameObject にアタッチして使用する。
// ============================================================
using UnityEngine;

/// <summary>
/// 解放済みスキルの効果を実際のゲームパラメータに反映するコンポーネント。
/// ActionScene 起動時に <see cref="SkillManager.GetTotalBonus"/> を読み取り、
/// PlayerHealth / JustInputAction / PlayerController / DropResolver / CookingMinigame
/// の各パラメータにパッチを当てる。
/// </summary>
public sealed class SkillEffectApplier : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────

    private const float FRAME_DURATION = 1f / 60f; // 1フレーム = 1/60秒

    // ──────────────────────────────────────────────
    // 攻撃力補正（静的フィールドで他システムから参照）
    // ──────────────────────────────────────────────

    /// <summary>攻撃力のスキル補正倍率。WeaponColliderHandler のダメージ計算時に参照する。</summary>
    public static float AttackMultiplier { get; private set; } = 1f;

    /// <summary>ドロップ率のスキル補正加算値。DropResolver で参照する。</summary>
    public static float DropRateBonus { get; private set; }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        ApplyAllSkillEffects();
    }

    // ──────────────────────────────────────────────
    // 公開 API — 再適用
    // ──────────────────────────────────────────────

    /// <summary>
    /// すべてのスキル効果を再度適用する。
    /// スキル解放直後のリフレッシュに使用可能。
    /// </summary>
    public void ApplyAllSkillEffects()
    {
        SkillManager skillManager = FindSkillManager();
        if (skillManager == null)
        {
            Debug.LogWarning("[SkillEffectApplier] SkillManager が見つかりません。スキル効果の適用をスキップします。");
            ResetStaticFields();
            return;
        }

        ApplyMaxHPUp(skillManager);
        ApplyJustFrameExtend(skillManager);
        ApplyAttackUp(skillManager);
        ApplyDropRateUp(skillManager);
        ApplyCookingSpeedUp(skillManager);
        ApplyDodgeDistanceUp(skillManager);
        ApplyDodgeInvincibleUp(skillManager);

        Debug.Log(
            $"[SkillEffectApplier] スキル効果を適用しました:\n" +
            $"  MaxHPUp: +{skillManager.GetTotalBonus(SkillData.SkillType.MaxHPUp)}\n" +
            $"  JustFrameExtend: +{skillManager.GetTotalBonus(SkillData.SkillType.JustFrameExtend)} フレーム\n" +
            $"  AttackUp: +{skillManager.GetTotalBonus(SkillData.SkillType.AttackUp)}% (倍率: {AttackMultiplier:F2})\n" +
            $"  DropRateUp: +{DropRateBonus:F2}\n" +
            $"  DodgeDistanceUp: +{skillManager.GetTotalBonus(SkillData.SkillType.DodgeDistanceUp)}\n" +
            $"  DodgeInvincibleUp: +{skillManager.GetTotalBonus(SkillData.SkillType.DodgeInvincibleUp)}");
    }

    // ──────────────────────────────────────────────
    // MaxHPUp → PlayerHealth.SetMaxHP
    // ──────────────────────────────────────────────

    private void ApplyMaxHPUp(SkillManager sm)
    {
        float bonus = sm.GetTotalBonus(SkillData.SkillType.MaxHPUp);
        if (bonus <= 0f) return;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        int newMaxHP = playerHealth.MaxHP + Mathf.RoundToInt(bonus);
        playerHealth.SetMaxHP(newMaxHP);
    }

    // ──────────────────────────────────────────────
    // JustFrameExtend → JustInputConfig.HitStopDuration
    // ──────────────────────────────────────────────

    private void ApplyJustFrameExtend(SkillManager sm)
    {
        float bonusFrames = sm.GetTotalBonus(SkillData.SkillType.JustFrameExtend);
        if (bonusFrames <= 0f) return;

        JustInputAction jia = FindFirstObjectByType<JustInputAction>();
        if (jia == null) return;

        // JustInputConfig の HitStopDuration にフレーム数分の時間を加算する
        // JustInputConfig は ScriptableObject なので直接変更するとアセットが変わってしまう
        // → リフレクションで JustInputAction の受付時間をランタイムで補正する
        // JustInputAction.StartHitStop() 内で _hitStopAcceptDuration が計算されるため、
        // WeaponData.JustInputFrameBonus と同様に、config の値を直接加算する
        var configField = typeof(JustInputAction).GetField("_config",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (configField == null) return;

        JustInputConfig config = configField.GetValue(jia) as JustInputConfig;
        if (config == null) return;

        // ランタイム用の一時コピーを作成してフレームボーナスを加算
        // （元のアセットを汚さないため）
        JustInputConfig runtimeConfig = Instantiate(config);
        float originalDuration = runtimeConfig.HitStopDuration;

        // HitStopDuration フィールドにリフレクションで加算
        var durationField = typeof(JustInputConfig).GetField("_hitStopDuration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (durationField != null)
        {
            float newDuration = originalDuration + bonusFrames * FRAME_DURATION;
            durationField.SetValue(runtimeConfig, newDuration);
        }

        // ランタイム用 config を JustInputAction にセット
        configField.SetValue(jia, runtimeConfig);
    }

    // ──────────────────────────────────────────────
    // AttackUp → 静的倍率（WeaponColliderHandler で参照）
    // ──────────────────────────────────────────────

    private void ApplyAttackUp(SkillManager sm)
    {
        float bonusPercent = sm.GetTotalBonus(SkillData.SkillType.AttackUp);
        AttackMultiplier = 1f + bonusPercent / 100f;
    }

    // ──────────────────────────────────────────────
    // DropRateUp → 静的加算値（DropResolver で参照）
    // ──────────────────────────────────────────────

    private void ApplyDropRateUp(SkillManager sm)
    {
        // Value はパーセンテージ（例: 10 = +10%）
        float bonusPercent = sm.GetTotalBonus(SkillData.SkillType.DropRateUp);
        DropRateBonus = bonusPercent / 100f;
    }

    // ──────────────────────────────────────────────
    // CookingSpeedUp → CookingMinigame のゲージ速度を低下
    // ──────────────────────────────────────────────

    private void ApplyCookingSpeedUp(SkillManager sm)
    {
        // CookingSpeedUp はゲージを遅くする（ = 止めやすくする）ので、
        // 実際にはゲージ速度の減算。Value が 10 なら速度 -10%。
        // CookingMinigame は ManagementScene 側なのでここでは静的フィールドに保存し、
        // ManagementScene 側の SkillEffectApplier（または CookingMinigame 自身）が参照する。
        // → ここでは static で保存するだけ
        float bonusPercent = sm.GetTotalBonus(SkillData.SkillType.CookingSpeedUp);
        CookingSpeedReduction = bonusPercent / 100f;
    }

    /// <summary>調理ゲージ速度の減少率（0〜1）。CookingMinigame が参照する。</summary>
    public static float CookingSpeedReduction { get; private set; }

    // ──────────────────────────────────────────────
    // DodgeDistanceUp → PlayerController.DodgeDistance
    // ──────────────────────────────────────────────

    private void ApplyDodgeDistanceUp(SkillManager sm)
    {
        float bonus = sm.GetTotalBonus(SkillData.SkillType.DodgeDistanceUp);
        if (bonus <= 0f) return;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        pc.DodgeDistance += bonus;
    }

    // ──────────────────────────────────────────────
    // DodgeInvincibleUp → PlayerController.DodgeInvincibleRatio
    // ──────────────────────────────────────────────

    private void ApplyDodgeInvincibleUp(SkillManager sm)
    {
        float bonus = sm.GetTotalBonus(SkillData.SkillType.DodgeInvincibleUp);
        if (bonus <= 0f) return;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        // bonus はパーセンテージポイント（例: 10 = +0.1 ratio）
        pc.DodgeInvincibleRatio = Mathf.Clamp01(pc.DodgeInvincibleRatio + bonus / 100f);
    }

    // ──────────────────────────────────────────────
    // ヘルパー
    // ──────────────────────────────────────────────

    private SkillManager FindSkillManager()
    {
        // GameManager に SkillManager がアタッチされている想定
        if (GameManager.Instance != null)
        {
            SkillManager sm = GameManager.Instance.GetComponent<SkillManager>();
            if (sm != null) return sm;
        }

        return FindFirstObjectByType<SkillManager>();
    }

    private void ResetStaticFields()
    {
        AttackMultiplier = 1f;
        DropRateBonus = 0f;
        CookingSpeedReduction = 0f;
    }
}
