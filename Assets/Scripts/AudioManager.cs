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
    private Coroutine crossfadeRoutine;

    [Header("Audio Clips")]
    public List<NamedAudioClip> soundEffects = new List<NamedAudioClip>();
    public List<NamedAudioClip> musicTracks = new List<NamedAudioClip>();

    private Dictionary<string, AudioClip> sfxDict = new();
    private Dictionary<string, AudioClip> musicDict = new();

    [Header("Playlist Settings")]
    public List<string> playlistTrackNames = new();
    public float playlistCrossfadeTime = 2f;
    public bool avoidRepeats = true;

    private string lastPlayedTrack = "";
    private Coroutine playlistRoutine;

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
            sfxDict[clip.name] = clip.clip;
        foreach (var clip in musicTracks)
            musicDict[clip.name] = clip.clip;

        currentMusicSource = musicSourceA;
        nextMusicSource = musicSourceB;
    }

    // ----- SFX -----
    public void PlaySFX(string name, float volume = 1f)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"SFX '{name}' not found!");
        }
    }

    // ----- Music -----
    public void PlayMusic(string name, bool loop = true, float volume = 1f)
    {
        if (!musicDict.TryGetValue(name, out var clip))
        {
            Debug.LogWarning($"Music '{name}' not found!");
            return;
        }

        currentMusicSource.clip = clip;
        currentMusicSource.loop = loop;
        currentMusicSource.volume = volume;
        currentMusicSource.Play();
    }

    public void StopMusic()
    {
        currentMusicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        currentMusicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }

    public void CrossfadeMusic(string name, float duration = 2f, float targetVolume = 1f)
    {
        if (!musicDict.TryGetValue(name, out var newClip))
        {
            Debug.LogWarning($"Music '{name}' not found!");
            return;
        }

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        crossfadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, duration, targetVolume));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration, float targetVolume)
    {
        nextMusicSource.clip = newClip;
        nextMusicSource.volume = 0f;
        nextMusicSource.loop = true;
        nextMusicSource.Play();

        float time = 0f;
        float startVolume = currentMusicSource.volume;

        while (time < duration)
        {
            float t = time / duration;
            currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            nextMusicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            time += Time.deltaTime;
            yield return null;
        }

        currentMusicSource.Stop();
        currentMusicSource.volume = targetVolume;

        // Swap roles
        var temp = currentMusicSource;
        currentMusicSource = nextMusicSource;
        nextMusicSource = temp;

        crossfadeRoutine = null;
    }

    public void CrossfadeToRandomPlaylistTrack(float crossfadeDuration = 2f, float targetVolume = 1f)
    {
        if (playlistTrackNames.Count == 0)
        {
            Debug.LogWarning("Playlist is empty!");
            return;
        }

        List<string> availableTracks = new(playlistTrackNames);

        if (avoidRepeats && availableTracks.Count > 1)
            availableTracks.Remove(lastPlayedTrack);

        string selected = availableTracks[Random.Range(0, availableTracks.Count)];
        lastPlayedTrack = selected;

        CrossfadeMusic(selected, crossfadeDuration, targetVolume);
    }
}

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
}

