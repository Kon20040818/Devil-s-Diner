// ============================================================
// CookingMinigame.cs
// 夕方パートの調理ミニゲーム。ゲージを止めて調理ランクを判定する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 調理ミニゲーム。
/// ゲージが PingPong で往復し、プレイヤーがボタンを押してストップ。
/// 停止位置に応じて Perfect / Good / Miss を判定し、CookedDishData を生成する。
/// </summary>
public sealed class CookingMinigame : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定数
    // ──────────────────────────────────────────────
    private const float GAUGE_CENTER = 0.5f;

    // ──────────────────────────────────────────────
    // シリアライズフィールド
    // ──────────────────────────────────────────────

    [SerializeField] private CookingConfig _config;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>
    /// 調理が完了したとき。引数は生成された CookedDishData。
    /// </summary>
    public event Action<CookedDishData> OnCookingCompleted;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private bool _isActive;
    private float _gaugePosition;
    private float _currentSpeed;
    private RecipeData _currentRecipe;

    /// <summary>PingPong 計算用の開始時刻（unscaledTime）。</summary>
    private float _startTime;

    // ──────────────────────────────────────────────
    // 公開プロパティ
    // ──────────────────────────────────────────────

    /// <summary>ミニゲームが実行中かどうか。</summary>
    public bool IsActive => _isActive;

    /// <summary>現在のゲージ位置（0〜1）。</summary>
    public float GaugePosition => _gaugePosition;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Update()
    {
        if (!_isActive) return;

        // Time.unscaledTime ベースの PingPong でゲージを往復させる
        float elapsed = Time.unscaledTime - _startTime;
        _gaugePosition = Mathf.PingPong(elapsed * _currentSpeed, 1f);

        // ボタン入力でゲージストップ（Space キーまたは左クリック）
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            StopAndJudge();
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 調理を開始する。素材チェック＆消費を行い、ゲージを動かし始める。
    /// </summary>
    /// <param name="recipe">調理するレシピ。</param>
    /// <returns>素材が足りず開始できなかった場合は false。</returns>
    public bool StartCooking(RecipeData recipe)
    {
        if (_isActive)
        {
            Debug.LogWarning("[CookingMinigame] 既に調理中です。");
            return false;
        }

        if (recipe == null)
        {
            Debug.LogWarning("[CookingMinigame] レシピが null です。");
            return false;
        }

        if (_config == null)
        {
            Debug.LogError("[CookingMinigame] CookingConfig が設定されていません。");
            return false;
        }

        InventoryManager inventory = GameManager.Instance.Inventory;

        // 素材チェック＆消費
        if (!inventory.TryConsumeMaterialsForRecipe(recipe))
        {
            Debug.Log("[CookingMinigame] 素材が不足しています。");
            return false;
        }

        _currentRecipe = recipe;

        // 速度 = config.BaseSpeed * 最もレアリティが高い素材の GaugeSpeedMultiplier
        _currentSpeed = _config.BaseSpeed * GetHighestRaritySpeedMultiplier(recipe);

        // ゲージ開始
        _gaugePosition = 0f;
        _startTime = Time.unscaledTime;
        _isActive = true;

        return true;
    }

    // ──────────────────────────────────────────────
    // 内部メソッド
    // ──────────────────────────────────────────────

    /// <summary>
    /// レシピの必要素材のうち、最もレアリティが高い素材の GaugeSpeedMultiplier を返す。
    /// </summary>
    private float GetHighestRaritySpeedMultiplier(RecipeData recipe)
    {
        float multiplier = 1f;
        int highestRarity = 0;

        IReadOnlyList<RecipeData.RequiredMaterial> materials = recipe.RequiredMaterials;

        for (int i = 0; i < materials.Count; i++)
        {
            MaterialData mat = materials[i].Material;
            if (mat == null) continue;

            if (mat.Rarity > highestRarity)
            {
                highestRarity = mat.Rarity;
                multiplier = mat.GaugeSpeedMultiplier;
            }
        }

        return multiplier;
    }

    /// <summary>
    /// ゲージを停止し、判定を行って CookedDishData を生成する。
    /// </summary>
    private void StopAndJudge()
    {
        _isActive = false;

        float distance = Mathf.Abs(_gaugePosition - GAUGE_CENTER);
        float halfWidth = _config.BaseSuccessWidth / 2f;
        float perfectHalfWidth = _config.PerfectZoneRatio * halfWidth;

        CookingRank rank;
        int price;

        if (distance <= perfectHalfWidth)
        {
            // Perfect 判定
            rank = CookingRank.Perfect;
            price = Mathf.RoundToInt(_currentRecipe.BasePrice * _currentRecipe.PerfectMultiplier);
        }
        else if (distance <= halfWidth)
        {
            // Good 判定
            rank = CookingRank.Good;
            price = _currentRecipe.BasePrice;
        }
        else
        {
            // Miss 判定
            rank = CookingRank.Miss;
            price = _config.BurntMeatPrice;
        }

        CookedDishData dish = new CookedDishData(_currentRecipe, rank, price);

        // 調理完了SE
        if (AudioManager.Instance != null)
        {
            string seKey = rank == CookingRank.Perfect ? "cooking_perfect"
                         : rank == CookingRank.Good    ? "cooking_good"
                         :                               "cooking_miss";
            AudioManager.Instance.PlaySE(seKey);
        }

        // インベントリに追加
        GameManager.Instance.Inventory.AddCookedDish(dish);

        // イベント発火
        OnCookingCompleted?.Invoke(dish);

        _currentRecipe = null;
    }
}
