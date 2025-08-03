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

    private Dictionary<string, AudioClip> sfxDict = new();
    private Dictionary<string, float> sfxVolumeDict = new();

    private Dictionary<string, AudioClip> musicDict = new();
    private Dictionary<string, float> musicVolumeDict = new();

    [Header("Playlist Settings")]
    public List<string> playlistTrackNames = new();
    public float playlistFadeOutTime = 2f;

    [Header("Fallback Settings")]
    public string fallbackTrackName = "idle_loop";
    private bool fallbackPending = false;

    private int currentPlaylistIndex = -1;
    private string lastPlayedTrack = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var clip in soundEffects)
        {
            sfxDict[clip.name] = clip.clip;
            sfxVolumeDict[clip.name] = clip.volume;
        }

        foreach (var clip in musicTracks)
        {
            musicDict[clip.name] = clip.clip;
            musicVolumeDict[clip.name] = clip.volume;
        }

        currentMusicSource = musicSourceA;
        nextMusicSource = musicSourceB;
    }

    private void Update()
    {
        if (
            fallbackPending &&
            !currentMusicSource.isPlaying &&
            currentMusicSource.clip != null &&
            !currentMusicSource.loop
        )
        {
            //Debug.Log($"Fallback triggered. Playing: '{fallbackTrackName}'");
            fallbackPending = false;
            PlayMusicWithFadeOutOld(fallbackTrackName, playlistFadeOutTime, loop: true);
        }
    }

    // ──────────────────────────────────────────────
    // SFX
    // ──────────────────────────────────────────────

    public void PlaySFX(string name, float volumeMultiplier = 1f)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            float baseVolume = sfxVolumeDict.TryGetValue(name, out var v) ? v : 1f;
            float finalVolume = baseVolume * volumeMultiplier;
            sfxSource.PlayOneShot(clip, finalVolume);
            //Debug.Log($" Playing SFX: '{name}' | Volume: {finalVolume:F2}");
        }
        else
        {
            Debug.LogWarning($" SFX '{name}' not found!");
        }
    }

    public void SetSFXVolume(float volume) => sfxSource.volume = volume;

    // ──────────────────────────────────────────────
    // Music (Fade-out Previous, Play New)
    // ──────────────────────────────────────────────

    public void PlayMusicWithFadeOutOld(string newTrackName, float fadeOutDuration = 2f, bool loop = false)
    {
        if (!musicDict.TryGetValue(newTrackName, out var newClip))
        {
            Debug.LogWarning($" Music track '{newTrackName}' not found!");
            return;
        }

        if (fadeOutRoutine != null)
            StopCoroutine(fadeOutRoutine);

        fadeOutRoutine = StartCoroutine(FadeOutOldTrack(currentMusicSource, fadeOutDuration));

        float volume = musicVolumeDict.TryGetValue(newTrackName, out float trackVolume) ? trackVolume : 1f;
        nextMusicSource.clip = newClip;
        nextMusicSource.volume = volume;
        nextMusicSource.loop = loop;
        nextMusicSource.Play();

        //Debug.Log($" Playing music: '{newTrackName}' | Volume: {volume:F2} | Loop: {loop}");

        var temp = currentMusicSource;
        currentMusicSource = nextMusicSource;
        nextMusicSource = temp;
    }

    private IEnumerator FadeOutOldTrack(AudioSource sourceToFade, float duration)
    {
        float startVolume = sourceToFade.volume;
        float time = 0f;

        while (time < duration)
        {
            sourceToFade.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            time += Time.deltaTime;
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

    public void SetMusicVolume(float volume)
    {
        currentMusicSource.volume = volume;

    }
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
            Debug.LogWarning(" Invalid playlist index.");
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
            Debug.LogWarning(" Playlist is empty!");
            return;
        }

        string selected = playlistTrackNames[Random.Range(0, playlistTrackNames.Count)];
        lastPlayedTrack = selected;

        float duration = fadeOutDuration > 0 ? fadeOutDuration : playlistFadeOutTime;

        fallbackPending = true;
        PlayMusicWithFadeOutOld(selected, duration, loop: false);
    }

    public void PlaySequentialSFX(string firstClipName, string secondClipName, float volumeMultiplier = 1f, float delayBetween = 0f)
    {
        StartCoroutine(PlaySequentialRoutine(firstClipName, secondClipName, volumeMultiplier, delayBetween));
    }

    private IEnumerator PlaySequentialRoutine(string firstClipName, string secondClipName, float volumeMultiplier, float delayBetween)
    {
        if (!sfxDict.TryGetValue(firstClipName, out var firstClip))
        {
            Debug.LogWarning($" First SFX '{firstClipName}' not found!");
            yield break;
        }

        float firstVolume = sfxVolumeDict.TryGetValue(firstClipName, out var v1) ? v1 * volumeMultiplier : 1f * volumeMultiplier;
        sfxSource.PlayOneShot(firstClip, firstVolume);

        yield return new WaitForSeconds(firstClip.length + delayBetween);

        if (sfxDict.TryGetValue(secondClipName, out var secondClip))
        {
            float secondVolume = sfxVolumeDict.TryGetValue(secondClipName, out var v2) ? v2 * volumeMultiplier : 1f * volumeMultiplier;
            sfxSource.PlayOneShot(secondClip, secondVolume);
        }
    }
}

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
    [Range(0f, 2f)] public float volume = 1f;
}



