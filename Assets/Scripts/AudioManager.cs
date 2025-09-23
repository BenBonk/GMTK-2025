using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music Sources")]
    public AudioSource musicSourceA;
    public AudioSource musicSourceB;

    [Header("SFX Source")]
    public AudioSource sfxSource;

    private AudioSource currentMusicSource;
    private AudioSource nextMusicSource;
    private Coroutine fadeOutRoutine;

    [Header("Audio Clips")]
    public List<NamedAudioClip> soundEffects = new();
    public List<NamedAudioClip> musicTracks = new();

    private readonly Dictionary<string, AudioClip> sfxDict = new();
    private readonly Dictionary<string, float> sfxVolumeDict = new();

    private readonly Dictionary<string, AudioClip> musicDict = new();
    private readonly Dictionary<string, float> musicVolumeDict = new();

    [Header("Playlist Settings")]
    public List<string> playlistTrackNames = new();
    public float playlistFadeOutTime = 2f;

    [Header("Fallback Settings")]
    public string fallbackTrackName = "idle_loop";
    private bool fallbackPending = false;

    private int currentPlaylistIndex = -1;
    private string lastPlayedTrack = "";

    private bool isAppFocused = true;

    // Master volumes (0..1) loaded from FBPP written by SettingsMenu
    private float masterMusic01 = 0.5f;
    private float masterSfx01 = 0.5f;

    // Base volume of current music track (from musicVolumeDict)
    private float currentMusicBaseVol = 1f;

    // FBPP keys used by SettingsMenu
    private const string KeyMusic = "musicValue";
    private const string KeySfx = "sfxValue";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build dictionaries
        foreach (var clip in soundEffects)
        {
            if (!string.IsNullOrEmpty(clip.name) && clip.clip)
            {
                sfxDict[clip.name] = clip.clip;
                sfxVolumeDict[clip.name] = clip.volume;
            }
        }
        foreach (var clip in musicTracks)
        {
            if (!string.IsNullOrEmpty(clip.name) && clip.clip)
            {
                musicDict[clip.name] = clip.clip;
                musicVolumeDict[clip.name] = clip.volume;
            }
        }

        currentMusicSource = musicSourceA;
        nextMusicSource = musicSourceB;

        // Load saved user volumes (defaults 0.5)
        masterMusic01 = FBPP.GetFloat(KeyMusic, 0.5f);
        masterSfx01 = FBPP.GetFloat(KeySfx, 0.5f);

        ApplyMasterVolumes();
    }

    private void Update()
    {
        if (!Application.isFocused || AudioListener.pause) return;

        if (fallbackPending &&
            !currentMusicSource.isPlaying &&
            currentMusicSource.clip != null &&
            !currentMusicSource.loop &&
            isAppFocused)
        {
            fallbackPending = false;
            PlayMusicWithFadeOutOld(fallbackTrackName, playlistFadeOutTime, loop: true);
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        isAppFocused = focus;

        if (focus && fallbackPending && currentMusicSource.clip != null && !currentMusicSource.isPlaying)
        {
            currentMusicSource.Play();
        }
    }

    // Perceptual curves (tweak exponent if desired: 2.0..3.0)
    private float MusicMasterGain() => Mathf.Pow(Mathf.Clamp01(masterMusic01), 2f);
    private float SfxMasterGain() => Mathf.Pow(Mathf.Clamp01(masterSfx01), 2f);

    private void ApplyMasterVolumes()
    {
        float mg = MusicMasterGain();
        musicSourceA.volume = currentMusicBaseVol * mg;
        musicSourceB.volume = currentMusicBaseVol * mg;
        sfxSource.volume = SfxMasterGain();
    }

    // --------------- SFX ----------------

    public void PlaySFX(string name, float volumeMultiplier = 1f)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            float baseVol = sfxVolumeDict.TryGetValue(name, out var v) ? v : 1f;
            float final = baseVol * volumeMultiplier * SfxMasterGain();
            sfxSource.PlayOneShot(clip, final);
        }
        else
        {
            Debug.LogWarning($"SFX '{name}' not found!");
        }
    }

    public void SetSFXVolume(float slider01)
    {
        masterSfx01 = Mathf.Clamp01(slider01);
        ApplyMasterVolumes();
    }

    // Re-added: play two SFX in sequence, scaled by SFX master
    public void PlaySequentialSFX(string firstClipName, string secondClipName, float volumeMultiplier = 1f, float delayBetween = 0f)
    {
        StartCoroutine(PlaySequentialRoutine(firstClipName, secondClipName, volumeMultiplier, delayBetween));
    }

    private IEnumerator PlaySequentialRoutine(string firstClipName, string secondClipName, float volumeMultiplier, float delayBetween)
    {
        if (!sfxDict.TryGetValue(firstClipName, out var firstClip))
        {
            Debug.LogWarning($"First SFX '{firstClipName}' not found!");
            yield break;
        }

        float v1 = sfxVolumeDict.TryGetValue(firstClipName, out var bv1) ? bv1 : 1f;
        sfxSource.PlayOneShot(firstClip, v1 * volumeMultiplier * SfxMasterGain());

        yield return new WaitForSeconds(firstClip.length + Mathf.Max(0f, delayBetween));

        if (sfxDict.TryGetValue(secondClipName, out var secondClip))
        {
            float v2 = sfxVolumeDict.TryGetValue(secondClipName, out var bv2) ? bv2 : 1f;
            sfxSource.PlayOneShot(secondClip, v2 * volumeMultiplier * SfxMasterGain());
        }
    }

    // --------------- Music ----------------

    public void PlayMusicWithFadeOutOld(string newTrackName, float fadeOutDuration = 2f, bool loop = false)
    {
        if (!musicDict.TryGetValue(newTrackName, out var newClip))
        {
            Debug.LogWarning($"Music track '{newTrackName}' not found!");
            return;
        }

        if (fadeOutRoutine != null)
            StopCoroutine(fadeOutRoutine);
        fadeOutRoutine = StartCoroutine(FadeOutOldTrack(currentMusicSource, fadeOutDuration));

        // Cache per-track base volume and multiply by master
        currentMusicBaseVol = musicVolumeDict.TryGetValue(newTrackName, out float baseVol) ? baseVol : 1f;

        nextMusicSource.clip = newClip;
        nextMusicSource.loop = loop;
        nextMusicSource.volume = currentMusicBaseVol * MusicMasterGain();
        nextMusicSource.Play();

        var temp = currentMusicSource;
        currentMusicSource = nextMusicSource;
        nextMusicSource = temp;
    }

    private IEnumerator FadeOutOldTrack(AudioSource sourceToFade, float duration)
    {
        float startVolume = sourceToFade.volume;
        float t = 0f;

        while (t < duration)
        {
            sourceToFade.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        sourceToFade.Stop();
        sourceToFade.volume = startVolume;
        fadeOutRoutine = null;
    }

    public void StopMusicImmediately()
    {
        currentMusicSource.Stop();
        nextMusicSource.Stop();
        fallbackPending = false;
    }

    // Called by SettingsMenu sliders (SettingsMenu saves FBPP)
    public void SetMusicVolume(float slider01)
    {
        masterMusic01 = Mathf.Clamp01(slider01);
        ApplyMasterVolumes();
    }

    // --------------- Playlist helpers ----------------

    public void PlayNextPlaylistTrack(float fadeOutDuration = -1f)
    {
        if (playlistTrackNames.Count == 0) return;

        currentPlaylistIndex = (currentPlaylistIndex + 1) % playlistTrackNames.Count;
        string nextTrack = playlistTrackNames[currentPlaylistIndex];

        float duration = fadeOutDuration > 0 ? fadeOutDuration : playlistFadeOutTime;

        fallbackPending = true;
        PlayMusicWithFadeOutOld(nextTrack, duration, loop: false);
    }

    public void PlayPlaylistTrack(int index, float fadeOutDuration = -1f)
    {
        if (playlistTrackNames.Count == 0 || index < 0 || index >= playlistTrackNames.Count)
        {
            Debug.LogWarning("Invalid playlist index.");
            return;
        }

        currentPlaylistIndex = index;
        float duration = fadeOutDuration > 0 ? fadeOutDuration : playlistFadeOutTime;

        fallbackPending = true;
        PlayMusicWithFadeOutOld(playlistTrackNames[index], duration, loop: false);
    }

    public void PlayRandomPlaylistTrack(float fadeOutDuration = -1f)
    {
        if (playlistTrackNames.Count == 0)
        {
            Debug.LogWarning("Playlist is empty!");
            return;
        }

        string selected = playlistTrackNames[Random.Range(0, playlistTrackNames.Count)];
        lastPlayedTrack = selected;

        float duration = fadeOutDuration > 0 ? fadeOutDuration : playlistFadeOutTime;

        fallbackPending = true;
        PlayMusicWithFadeOutOld(selected, duration, loop: false);
    }
}

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
    [Range(0f, 2f)] public float volume = 1f;
}




