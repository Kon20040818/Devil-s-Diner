// ============================================================
// CustomerSpawner.cs
// 客NPCのスポーン管理。居心地度ベースのスポーン確率計算を行う。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 客NPCを一定間隔でスポーンする。
/// DinerManager の ComfortScore に基づいてスポーン確率を計算する。
/// </summary>
public sealed class CustomerSpawner : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private int _maxCustomers = 30;
    private const float COMFORT_RATE_BONUS = 0.005f;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>最大客数を外部から設定する（店舗拡張用）。</summary>
    public void SetMaxCustomers(int max) { _maxCustomers = max; }

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private GameObject _customerPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _exitPoint;
    [SerializeField] private SeatManager _seatManager;
    [SerializeField] private DinerManager _dinerManager;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField, Range(0f, 1f)] private float _baseSpawnRate = 0.5f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private float _spawnTimer;
    private readonly List<CustomerAI> _activeCustomers = new List<CustomerAI>();
    private bool _isActive;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (_dinerManager != null)
        {
            _dinerManager.OnBusinessStarted += HandleBusinessStarted;
        }
    }

    private void OnDisable()
    {
        if (_dinerManager != null)
        {
            _dinerManager.OnBusinessStarted -= HandleBusinessStarted;
        }
        _isActive = false;
    }

    private void Update()
    {
        if (!_isActive) return;

        _spawnTimer += Time.deltaTime;

        // HousingManager の SpawnIntervalMultiplier で実効間隔を短縮
        float effectiveInterval = _spawnInterval;
        if (_dinerManager != null && _dinerManager.Housing != null)
        {
            effectiveInterval *= _dinerManager.Housing.SpawnIntervalMultiplier;
        }

        if (_spawnTimer >= effectiveInterval)
        {
            _spawnTimer = 0f;
            TrySpawnCustomer();
        }

        // 破棄済みの参照をクリーンアップ
        _activeCustomers.RemoveAll(c => c == null);
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    private void HandleBusinessStarted()
    {
        _isActive = true;
        _spawnTimer = 0f;
    }

    private void TrySpawnCustomer()
    {
        if (_activeCustomers.Count >= _maxCustomers) return;
        if (_seatManager != null && _seatManager.AvailableSeatCount <= 0) return;

        // スポーン確率 = baseRate + comfortBonus * COMFORT_RATE_BONUS
        float comfortScore = _dinerManager != null ? _dinerManager.ComfortScore : 0f;
        float spawnRate = Mathf.Clamp01(_baseSpawnRate + comfortScore * COMFORT_RATE_BONUS);

        if (Random.value > spawnRate) return;

        if (_customerPrefab == null || _spawnPoint == null) return;

        GameObject obj = Instantiate(_customerPrefab, _spawnPoint.position, _spawnPoint.rotation);

        if (obj.TryGetComponent(out CustomerAI customer))
        {
            customer.Initialize(_seatManager, _exitPoint);
            _activeCustomers.Add(customer);

            if (_dinerManager != null)
            {
                _dinerManager.RegisterCustomer(customer);
            }
        }
    }
}
