// ============================================================
// EnemyData.cs
// 敵データの ScriptableObject。
// ============================================================
using UnityEngine;

/// <summary>敵データ。HP、攻撃力、ドロップ情報を定義する。</summary>
[CreateAssetMenu(fileName = "ENM_New", menuName = "DevilsDiner/EnemyData")]
public sealed class EnemyData : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _enemyName;
    [SerializeField] private int _maxHP = 1000;
    [SerializeField] private int _baseAttack = 50;

    [Header("ドロップ設定")]
    [SerializeField] private ItemData _dropItemNormal;
    [SerializeField] private ItemData _dropItemJust;
    [SerializeField, Range(0f, 1f)] private float _dropRateNormal = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _dropRateJust = 1.0f;

    [Header("ゴールド報酬")]
    [SerializeField] private int _goldReward = 100;

    public string   Id              => _id;
    public string   EnemyName       => _enemyName;
    public int      MaxHP           => _maxHP;
    public int      BaseAttack      => _baseAttack;
    public ItemData DropItemNormal  => _dropItemNormal;
    public ItemData DropItemJust    => _dropItemJust;
    public float    DropRateNormal  => _dropRateNormal;
    public float    DropRateJust    => _dropRateJust;
    public int      GoldReward      => _goldReward;
}
