using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for all game sound effects.
/// Loads audio clips from Resources/Audio/ and plays them on demand.
///
/// Usage: SoundManager.Instance.Play("play");
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // All sound effect clip names and their loaded AudioClips
    private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    // Dedicated AudioSource for SFX (separate from BGM)
    private AudioSource sfxSource;

    // SFX volume (0-1), persisted in PlayerPrefs
    private float sfxVolume = 0.5f;

    // All sound effect names expected in Resources/Audio/
    private static readonly string[] SFX_NAMES = {
        "sfx_play",     // Playing cards
        "sfx_pass",     // Passing / skipping turn
        "sfx_bomb",     // Bomb (four-of-a-kind)
        "sfx_rocket",   // Rocket (both jokers)
        "sfx_bid",      // Bidding for landlord
        "sfx_win",      // Victory
        "sfx_lose",     // Defeat
        "sfx_select",   // Selecting/deselecting a card
        "sfx_deal"      // Dealing cards
    };

    private void Awake()
    {
        // Singleton setup — persists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create dedicated SFX audio source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        // Load persisted volume setting
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        sfxSource.volume = sfxVolume;

        // Pre-load all sound effects from Resources
        LoadAllClips();
    }

    /// <summary>
    /// Loads all expected SFX clips from Resources/Audio/.
    /// Missing files are logged as warnings but don't cause errors.
    /// </summary>
    private void LoadAllClips()
    {
        foreach (string name in SFX_NAMES)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip != null)
            {
                clips[name] = clip;
            }
            else
            {
                Debug.LogWarning($"SFX not found: Audio/{name}");
            }
        }
    }

    /// <summary>
    /// Plays a sound effect by name (without the "sfx_" prefix is also accepted).
    /// Stops any currently playing SFX first to prevent overlap/trailing sounds.
    /// Example: Play("play") or Play("sfx_play") both work.
    /// </summary>
    public void Play(string name)
    {
        // Allow calling with or without "sfx_" prefix
        string key = name.StartsWith("sfx_") ? name : "sfx_" + name;

        if (clips.TryGetValue(key, out AudioClip clip))
        {
            // Stop any previous sound to prevent overlap and trailing audio
            sfxSource.Stop();
            sfxSource.clip = clip;
            sfxSource.volume = sfxVolume;
            sfxSource.Play();
        }
        else
        {
            Debug.LogWarning($"SFX not loaded: {key}");
        }
    }

    /// <summary>
    /// Sets the SFX volume and persists it.
    /// </summary>
    public void SetVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    /// <summary>
    /// Returns current SFX volume (0-1).
    /// </summary>
    public float GetVolume()
    {
        return sfxVolume;
    }
}
