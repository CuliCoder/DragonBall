using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [System.Serializable]
    public class NamedAudioClip
    {
        public string Key;
        public AudioClip Clip;
    }

    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Default Audio")]
    [SerializeField] private AudioClip _defaultBackgroundMusic;
    [SerializeField] private bool _playDefaultBgmOnStart = true;

    [Header("Audio Library")]
    [SerializeField] private List<NamedAudioClip> _bgmClips = new();
    [SerializeField] private List<NamedAudioClip> _sfxClips = new();

    private readonly Dictionary<string, AudioClip> _bgmMap = new();
    private readonly Dictionary<string, AudioClip> _sfxMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        BuildLibrary();
    }

    private void Start()
    {
        if (_playDefaultBgmOnStart && _defaultBackgroundMusic != null)
        {
            PlayBackgroundMusic(_defaultBackgroundMusic, true);
        }
    }

    private void EnsureAudioSources()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }

        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;

        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
    }

    private void BuildLibrary()
    {
        _bgmMap.Clear();
        _sfxMap.Clear();

        foreach (var item in _bgmClips)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Key) || item.Clip == null)
            {
                continue;
            }

            _bgmMap[item.Key] = item.Clip;
        }

        foreach (var item in _sfxClips)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Key) || item.Clip == null)
            {
                continue;
            }

            _sfxMap[item.Key] = item.Clip;
        }
    }

    public void PlayBackgroundMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.Play();
    }

    public void PlayBackgroundMusic(string key, bool loop = true)
    {
        if (_bgmMap.TryGetValue(key, out var clip))
        {
            PlayBackgroundMusic(clip, loop);
            return;
        }

        Debug.LogWarning($"[SoundManager] BGM key not found: {key}");
    }

    public void StopBackgroundMusic()
    {
        _bgmSource.Stop();
    }

    public void PauseBackgroundMusic()
    {
        _bgmSource.Pause();
    }

    public void ResumeBackgroundMusic()
    {
        if (_bgmSource.clip == null)
        {
            return;
        }

        _bgmSource.UnPause();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (_sfxMap.TryGetValue(key, out var clip))
        {
            PlaySfx(clip, volumeScale);
            return;
        }

        Debug.LogWarning($"[SoundManager] SFX key not found: {key}");
    }

    public void StopAllSfx()
    {
        _sfxSource.Stop();
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void SetBgmVolume(float volume)
    {
        _bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        _sfxSource.volume = Mathf.Clamp01(volume);
    }
}
