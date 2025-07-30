using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public bool game;
    void Awake()
    {

        foreach (Sound s in sounds)     
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    private void Start()
    {
        if (game)
        {
            StartCoroutine(Game());
        }
        else
        {
            StartCoroutine(StartScreen());
        }
    }

    IEnumerator StartScreen()
    {
        Play("startintro");
        Sound s = Array.Find(sounds, sound => sound.name == "startintro");
        yield return new WaitForSeconds(s.clip.length-0.5f);
        Play("startloop");
    }

    IEnumerator Game()
    {
        yield return new WaitForSeconds(1);
        Play("gameintro");
        Sound s = Array.Find(sounds, sound => sound.name == "gameintro");
        yield return new WaitForSeconds(s.clip.length);
        Play("gameloop");
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
            return;
        s.source.Play();
    }
}
