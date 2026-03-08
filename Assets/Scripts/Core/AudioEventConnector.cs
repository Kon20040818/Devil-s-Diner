// ============================================================
// AudioEventConnector.cs
// AudioManager とゲームイベントを接続するブリッジ。
// 各ブートストラップから WireBattle / WireScene を呼んで結線する。
// ============================================================
using UnityEngine;

/// <summary>
/// AudioManager の SE/BGM を各種ゲームイベントに接続する。
/// SE キー名は AudioManager の Inspector で登録する必要がある。
/// </summary>
public static class AudioEventConnector
{
    // ──────────────────────────────────────────────
    // SE キー定数
    // ──────────────────────────────────────────────

    public const string SE_ATTACK_HIT    = "AttackHit";
    public const string SE_JUST_HIT      = "JustHit";
    public const string SE_SKILL         = "Skill";
    public const string SE_ULTIMATE      = "Ultimate";
    public const string SE_GUARD         = "Guard";
    public const string SE_DAMAGE        = "Damage";
    public const string SE_DEFEAT        = "Defeat";
    public const string SE_VICTORY       = "Victory";
    public const string SE_MENU_SELECT   = "MenuSelect";
    public const string SE_MENU_CONFIRM  = "MenuConfirm";
    public const string SE_COOKING       = "Cooking";
    public const string SE_SAVE          = "Save";

    // ──────────────────────────────────────────────
    // バトルイベント結線
    // ──────────────────────────────────────────────

    /// <summary>
    /// BattleManager のイベントに SE を接続する。
    /// BattleSceneBootstrap から呼ばれる。
    /// </summary>
    public static void WireBattle(BattleManager battleManager)
    {
        if (battleManager == null || AudioManager.Instance == null) return;

        battleManager.OnDamageDealt += result =>
        {
            if (result.IsWeakness)
                AudioManager.Instance.PlaySE(SE_JUST_HIT);
            else
                AudioManager.Instance.PlaySE(SE_ATTACK_HIT);
        };

        battleManager.OnBattleEnd += isVictory =>
        {
            AudioManager.Instance.PlaySE(isVictory ? SE_VICTORY : SE_DEFEAT);
        };

        battleManager.OnSkillExecuted += (character, skillName) =>
        {
            AudioManager.Instance.PlaySE(SE_SKILL);
        };

        battleManager.OnUltimateActivated += character =>
        {
            AudioManager.Instance.PlaySE(SE_ULTIMATE);
        };
    }

    // ──────────────────────────────────────────────
    // シーン BGM 結線
    // ──────────────────────────────────────────────

    /// <summary>
    /// シーンロード時にシーン名に応じたBGMを再生する。
    /// GameManager.OnSceneLoaded に接続して使用する。
    /// </summary>
    public static void WireSceneBGM()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnSceneLoaded += sceneName =>
        {
            if (AudioManager.Instance == null) return;

            // シーン名に応じたBGM切替（AudioClip は AudioManager の defaultBGM を使用）
            // 将来的にシーン別BGMクリップを持つ場合はここで分岐する
            switch (sceneName)
            {
                case "BaseScene":
                case "ManagementScene":
                    AudioManager.Instance.PlayDefaultBGM();
                    break;
                case "BattleScene":
                    // バトル用BGMがあればここで再生
                    // AudioManager.Instance.PlayBGM(battleBGM);
                    break;
                case "FieldScene":
                    // フィールド用BGMがあればここで再生
                    break;
            }
        };
    }
}
