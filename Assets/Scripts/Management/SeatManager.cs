// ============================================================
// SeatManager.cs
// ManagementScene 内の席を一元管理する。
// ============================================================
using UnityEngine;

/// <summary>
/// 店内の全席（SeatNode）を管理し、空席の検索と予約/解放を行う。
/// </summary>
public sealed class SeatManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private SeatNode[] _seats;

    // ──────────────────────────────────────────────
    // GC対策: 共有バッファ（シャッフル用）
    // ──────────────────────────────────────────────
    private static readonly System.Collections.Generic.List<SeatNode> _shuffleBuffer
        = new System.Collections.Generic.List<SeatNode>();

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>空き席の数。</summary>
    public int AvailableSeatCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] != null && !_seats[i].IsOccupied) count++;
            }
            return count;
        }
    }

    /// <summary>全席数。</summary>
    public int TotalSeatCount => _seats != null ? _seats.Length : 0;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 空席を1つ予約して返す。満席の場合は null。
    /// シャッフルして返すことでランダム性を持たせる。
    /// </summary>
    public SeatNode TryReserveSeat(CustomerAI customer)
    {
        if (customer == null) return null;

        // 共有バッファに空席を収集
        _shuffleBuffer.Clear();
        for (int i = 0; i < _seats.Length; i++)
        {
            if (_seats[i] != null && !_seats[i].IsOccupied)
            {
                _shuffleBuffer.Add(_seats[i]);
            }
        }

        if (_shuffleBuffer.Count == 0) return null;

        // Fisher-Yates シャッフル
        for (int i = _shuffleBuffer.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            SeatNode temp = _shuffleBuffer[i];
            _shuffleBuffer[i] = _shuffleBuffer[j];
            _shuffleBuffer[j] = temp;
        }

        SeatNode seat = _shuffleBuffer[0];
        seat.Occupy(customer);
        return seat;
    }

    /// <summary>席を解放する。</summary>
    public void ReleaseSeat(SeatNode seat)
    {
        if (seat != null)
        {
            seat.Release();
        }
    }
}
