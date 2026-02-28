// ============================================================
// CameraShakeHandler.cs
// カメラシェイク制御。unscaledDeltaTime ベースで動作し、
// ヒットストップ中（timeScale ≒ 0）でもシェイクが有効。
// ============================================================
using System.Collections;
using UnityEngine;

/// <summary>
/// カメラにアタッチしてシェイク演出を行う MonoBehaviour。
/// <c>Time.unscaledDeltaTime</c> ベースのコルーチンで制御するため、
/// ヒットストップ中でも正しく動作する。
/// </summary>
public sealed class CameraShakeHandler : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────
    private Vector3 _originalLocalPosition;
    private Coroutine _shakeCoroutine;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 指定した強度と持続時間でカメラをシェイクする。
    /// 既にシェイク中の場合は中断して再開始する。
    /// </summary>
    /// <param name="intensity">シェイクの振幅。</param>
    /// <param name="duration">持続時間（リアルタイム秒）。</param>
    public void Shake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;

        // 既存のシェイクを中断して位置を復元
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transform.localPosition = _originalLocalPosition;
        }

        _originalLocalPosition = transform.localPosition;
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    // ──────────────────────────────────────────────
    // コルーチン
    // ──────────────────────────────────────────────

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 残り時間に応じて減衰
            float remaining = 1f - (elapsed / duration);
            float currentIntensity = intensity * remaining;

            Vector3 offset = new Vector3(
                Random.Range(-currentIntensity, currentIntensity),
                Random.Range(-currentIntensity, currentIntensity),
                0f
            );

            transform.localPosition = _originalLocalPosition + offset;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 位置を元に戻す
        transform.localPosition = _originalLocalPosition;
        _shakeCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 安全策
    // ──────────────────────────────────────────────

    private void OnDisable()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transform.localPosition = _originalLocalPosition;
            _shakeCoroutine = null;
        }
    }
}
