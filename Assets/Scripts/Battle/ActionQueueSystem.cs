// ============================================================
// ActionQueueSystem.cs
// スターレイル風の速度ベース行動順システム。
// 各キャラクターの行動値 (Action Value) を管理し、
// 最も行動値が低いキャラから順に行動させる。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 速度ベースの行動順キューを管理するシステム。
/// 行動値 (AV) = 基準値 / Speed。AV が最小のキャラが次に行動する。
/// 行動後は自身の AV 分だけ再加算される（スターレイル方式）。
/// </summary>
public sealed class ActionQueueSystem
{
    // ──────────────────────────────────────────────
    // 内部データ
    // ──────────────────────────────────────────────

    /// <summary>キャラクターごとの現在の行動値。</summary>
    private readonly Dictionary<CharacterBattleController, float> _actionValues
        = new Dictionary<CharacterBattleController, float>();

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>行動順が更新されたとき。UIタイムライン表示に使用。</summary>
    public event Action OnQueueUpdated;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>キャラクターをキューに登録する。</summary>
    public void Register(CharacterBattleController character)
    {
        if (character == null) return;
        float initialAV = character.Stats.CalculateActionValue();
        _actionValues[character] = initialAV;
        OnQueueUpdated?.Invoke();
    }

    /// <summary>キャラクターをキューから除外する（死亡時等）。</summary>
    public void Unregister(CharacterBattleController character)
    {
        _actionValues.Remove(character);
        OnQueueUpdated?.Invoke();
    }

    /// <summary>
    /// 次に行動するキャラクターを決定する。
    /// 全キャラの AV から最小値を引き、AV=0 になったキャラが行動。
    /// </summary>
    /// <returns>次に行動するキャラクター。キューが空なら null。</returns>
    public CharacterBattleController AdvanceAndGetNext()
    {
        if (_actionValues.Count == 0) return null;

        // ── 最小 AV を見つける ──
        float minAV = float.MaxValue;
        CharacterBattleController next = null;

        foreach (var kvp in _actionValues)
        {
            if (!kvp.Key.IsAlive) continue;
            if (kvp.Value < minAV)
            {
                minAV = kvp.Value;
                next = kvp.Key;
            }
        }

        if (next == null) return null;

        // ── 全キャラの AV から最小値を引く（時間経過シミュレート） ──
        var keys = new List<CharacterBattleController>(_actionValues.Keys);
        foreach (var key in keys)
        {
            _actionValues[key] -= minAV;
        }

        // ── 行動キャラの AV を再計算して再加算 ──
        float nextAV = next.Stats.CalculateActionValue();
        _actionValues[next] = nextAV;

        OnQueueUpdated?.Invoke();
        return next;
    }

    /// <summary>
    /// 行動順プレビューを取得する（UIタイムライン表示用）。
    /// 上位 count 件を返す。
    /// </summary>
    public List<CharacterBattleController> GetOrderPreview(int count = 10)
    {
        var preview = new List<CharacterBattleController>();

        // ── 一時コピーでシミュレート ──
        var tempAV = new Dictionary<CharacterBattleController, float>();
        foreach (var kvp in _actionValues)
        {
            if (kvp.Key.IsAlive)
            {
                tempAV[kvp.Key] = kvp.Value;
            }
        }

        for (int i = 0; i < count && tempAV.Count > 0; i++)
        {
            float minAV = float.MaxValue;
            CharacterBattleController next = null;

            foreach (var kvp in tempAV)
            {
                if (kvp.Value < minAV)
                {
                    minAV = kvp.Value;
                    next = kvp.Key;
                }
            }

            if (next == null) break;

            preview.Add(next);

            // 全員から最小値を引く
            var keys = new List<CharacterBattleController>(tempAV.Keys);
            foreach (var key in keys)
            {
                tempAV[key] -= minAV;
            }

            // 行動者の AV を再加算
            tempAV[next] = next.Stats.CalculateActionValue();
        }

        return preview;
    }

    /// <summary>全データをクリアする。</summary>
    public void Clear()
    {
        _actionValues.Clear();
    }
}
