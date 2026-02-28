// ============================================================
// PlayerState.cs
// プレイヤーの状態列挙型。
// ============================================================

/// <summary>プレイヤーの行動状態を表す列挙型。</summary>
public enum PlayerState
{
    Idle,
    Move,
    Sprint,
    Jump,
    Attack,
    Dodge,
    Stagger,
    Dead
}
