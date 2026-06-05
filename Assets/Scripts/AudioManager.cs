using UnityEngine;

/// <summary>
/// 音效管理器（Singleton）。
/// 負責背景音樂播放、發牌音效、答對音效、答錯音效。
/// 
/// 使用方式：
///   1. 在場景中建立空 GameObject，命名為 "AudioManager"
///   2. 掛上此腳本
///   3. 在 Inspector 中把 Assets/Audio 裡的音檔拖進對應欄位
///   4. GameManager 會透過 AudioManager.Instance 呼叫播放方法
/// </summary>
[DefaultExecutionOrder(-100)]   // 確保比 GameManager(-99 以下) 更早 Awake
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── Inspector：音源 ──────────────────────────────────
    [Header("Audio Sources")]
    [Tooltip("播放背景音樂的 AudioSource（Loop = true）")]
    public AudioSource bgmSource;

    [Tooltip("播放音效的 AudioSource（一次性）")]
    public AudioSource sfxSource;

    // ── Inspector：音檔 ──────────────────────────────────
    [Header("BGM")]
    public AudioClip bgmClip;            // 背景音樂.mp3

    [Header("SFX - 發牌")]
    public AudioClip[] dealCardClips;     // 發牌音效 1~3（隨機播放其中一個）

    [Header("SFX - 答對")]
    public AudioClip correctClip;        // 答對音效.mp3

    [Header("SFX - 答錯")]
    public AudioClip wrongClip;          // 可選：答錯音效（若沒有就不播）

    [Header("SFX - 按鈕")]
    public AudioClip buttonClickClip;    // 可選：按鈕點擊音效

    [Header("SFX - 放牌")]
    public AudioClip placeCardClip;      // 可選：牌放進 Slot 音效

    // ── Inspector：音量 ──────────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    // ══════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ══════════════════════════════════════════════════════

    void Awake()
    {
        // Singleton 確保唯一
        if (Instance == null)
        {
            Instance = this;
            // 若希望跨場景保留：DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 若沒手動指定 AudioSource，就自動建立
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        bgmSource.spatialBlend = 0f;   // 強制 2D 播放，避免 3D 音效聽不到

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        sfxSource.spatialBlend = 0f;
    }

    void Start()
    {
        PlayBGM();
        Debug.Log($"🎵 AudioManager ready — BGM clip: {(bgmClip != null ? bgmClip.name : "null")}");
    }

    // ══════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════

    /// <summary>播放背景音樂（循環）</summary>
    public void PlayBGM()
    {
        if (bgmSource == null) return;
        if (bgmClip == null)
        {
            Debug.LogWarning("⚠ AudioManager：bgmClip 未指定，無法播放背景音樂");
            return;
        }

        bgmSource.clip        = bgmClip;
        bgmSource.volume      = bgmVolume;
        bgmSource.loop        = true;
        bgmSource.spatialBlend = 0f;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    /// <summary>暫停背景音樂</summary>
    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();
    }

    /// <summary>恢復背景音樂</summary>
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    /// <summary>停止背景音樂</summary>
    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    /// <summary>隨機播放發牌音效（從 dealCardClips 中隨機選一個）</summary>
    public void PlayDealCardSFX()
    {
        if (dealCardClips == null || dealCardClips.Length == 0) return;

        AudioClip clip = dealCardClips[Random.Range(0, dealCardClips.Length)];
        PlaySFX(clip);
    }

    /// <summary>播放答對音效</summary>
    public void PlayCorrectSFX()
    {
        PlaySFX(correctClip);
    }

    /// <summary>播放答錯音效</summary>
    public void PlayWrongSFX()
    {
        PlaySFX(wrongClip);
    }

    /// <summary>播放按鈕點擊音效</summary>
    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickClip);
    }

    /// <summary>播放放牌音效</summary>
    public void PlayPlaceCardSFX()
    {
        PlaySFX(placeCardClip);
    }

    /// <summary>設定背景音樂音量（0~1）</summary>
    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    /// <summary>設定音效音量（0~1）</summary>
    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
    }

    // ══════════════════════════════════════════════════════
    //  Internal
    // ══════════════════════════════════════════════════════

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
