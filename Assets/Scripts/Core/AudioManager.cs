// ============================================================
// AudioManager.cs
// ゲーム全体のBGMとSEを管理するシングルトン。
// GameManager と同一 GameObject に配置し DontDestroyOnLoad で永続化。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM再生とSE再生を一元管理するオーディオマネージャー。
/// GameManager と同じ GameObject に配置される。
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // シングルトン
    // ──────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    // Inspector — SE エントリ
    // ──────────────────────────────────────────────

    /// <summary>文字列キーと AudioClip の対応。</summary>
    [Serializable]
    public class SEEntry
    {
        public string Key;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
    }

    [Header("SE 設定")]
    [SerializeField] private SEEntry[] _seEntries;

    [Header("BGM 設定")]
    [SerializeField] private AudioClip _defaultBGM;
    [Range(0f, 1f)]
    [SerializeField] private float _bgmVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _seVolume = 1f;

    // ──────────────────────────────────────────────
    // 内部状態
    // ──────────────────────────────────────────────

    private AudioSource _bgmSource;
    private AudioSource _seSource;
    private Dictionary<string, SEEntry> _seLookup;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // AudioSource 初期化 — BGM 用
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = _bgmVolume;

        // AudioSource 初期化 — SE 用
        _seSource = gameObject.AddComponent<AudioSource>();
        _seSource.loop = false;
        _seSource.playOnAwake = false;
        _seSource.volume = _seVolume;

        // SE ルックアップ構築
        _seLookup = new Dictionary<string, SEEntry>();
        if (_seEntries != null)
        {
            foreach (SEEntry entry in _seEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    _seLookup[entry.Key] = entry;
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // 公開 API — SE
    // ──────────────────────────────────────────────

    /// <summary>
    /// 文字列キーで SE を再生する。
    /// Inspector で登録されていないキーは警告を出してスキップ。
    /// </summary>
    public void PlaySE(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (_seLookup != null && _seLookup.TryGetValue(key, out SEEntry entry))
        {
            if (entry.Clip != null)
            {
                _seSource.PlayOneShot(entry.Clip, entry.Volume * _seVolume);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SE キー '{key}' は未登録です。Inspector で追加してください。");
        }
    }

    /// <summary>
    /// AudioClip を直接指定して SE を再生する。
    /// 既存の AudioSource.PlayOneShot を置き換える用途。
    /// </summary>
    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _seSource.PlayOneShot(clip, volumeScale * _seVolume);
    }

    // ──────────────────────────────────────────────
    // 公開 API — BGM
    // ──────────────────────────────────────────────

    /// <summary>BGM を再生する。同じクリップが再生中なら何もしない。</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.volume = _bgmVolume;
        _bgmSource.Play();
    }

    /// <summary>デフォルト BGM を再生する。</summary>
    public void PlayDefaultBGM()
    {
        if (_defaultBGM != null)
        {
            PlayBGM(_defaultBGM);
        }
    }

    /// <summary>BGM を停止する。</summary>
    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    // ──────────────────────────────────────────────
    // 公開 API — ボリューム制御
    // ──────────────────────────────────────────────

    /// <summary>BGM ボリュームを設定する (0-1)。</summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = _bgmVolume;
    }

    /// <summary>SE ボリュームを設定する (0-1)。</summary>
    public void SetSEVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
    }

    /// <summary>現在の BGM ボリューム。</summary>
    public float BGMVolume => _bgmVolume;

    /// <summary>現在の SE ボリューム。</summary>
    public float SEVolume => _seVolume;
}
