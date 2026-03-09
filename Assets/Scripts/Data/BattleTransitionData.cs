// ============================================================
// BattleTransitionData.cs
// フィールド → バトルシーン間で敵構成データを受け渡すための
// 純粋な C# データクラス（MonoBehaviour ではない）。
// ============================================================

/// <summary>
/// フィールドからバトルシーンへ遷移する際の敵構成データ。
/// GameManager.PendingBattleData に設定し、BattleSceneBootstrap が消費する。
/// </summary>
[System.Serializable]
public sealed class BattleTransitionData
{
    /// <summary>エンカウントした敵の EnemyData 配列。</summary>
    public EnemyData[] EnemyDataList;

    /// <summary>エンカウントした敵の CharacterStats 配列。</summary>
    public CharacterStats[] EnemyStatsList;

    /// <summary>バトル後に戻るシーン名。</summary>
    public string ReturnSceneName;
}
