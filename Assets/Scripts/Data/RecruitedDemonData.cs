// ============================================================
// RecruitedDemonData.cs
// バトル → 経営フェーズへ渡すスカウト結果のデータ。
// バトル終了時にランダムバフを確定して生成する。
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// バトルでスカウト成功した悪魔のデータ。
/// バトル終了時にランダムバフが抽選・確定され、
/// <see cref="StaffManager"/> が受け取って <see cref="StaffInstance"/> に変換する。
/// </summary>
[Serializable]
public sealed class RecruitedDemonData
{
    /// <summary>敵の表示名。</summary>
    public string EnemyName;

    /// <summary>元のステータス参照。</summary>
    public CharacterStats Stats;

    /// <summary>種族マスター。</summary>
    public StaffRaceData Race;

    /// <summary>バトル終了時に確定したランダムバフ。</summary>
    public StaffBuffData[] RolledBuffs;
}
