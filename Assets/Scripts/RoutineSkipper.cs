using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

[DisallowMultipleComponent]
public class RoutineSkipper : MonoBehaviour
{
    [Header("Behavior")]
    public bool useUnscaledTime = false;
    public bool holdToSpeedUp = true;
    [Min(1f)] public float holdSpeedMultiplier = 8f;

    [Header("Inputs")]
    public bool mouseLeftSkips = true;
    public bool mouseRightSkips = false;
    public bool anyKeySkips = false;
    public KeyCode[] extraSkipKeys = { KeyCode.Space, KeyCode.Return };

    bool skipRequested;
    float speedMult = 1f;
    bool listening = true;

    public void SetListening(bool on) { listening = on; }
    public void RequestSkip() { skipRequested = true; }

    void Update()
    {
        if (!listening) return;

        if (mouseLeftSkips && Input.GetMouseButtonDown(0)) skipRequested = true;
        if (mouseRightSkips && Input.GetMouseButtonDown(1)) skipRequested = true;

        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).phase == TouchPhase.Began) { skipRequested = true; break; }

        if (anyKeySkips && Input.anyKeyDown) skipRequested = true;
        for (int i = 0; i < extraSkipKeys.Length; i++)
            if (Input.GetKeyDown(extraSkipKeys[i])) { skipRequested = true; break; }

        bool holding = Input.GetMouseButton(0) || Input.touchCount > 0;
        speedMult = (holdToSpeedUp && holding) ? Mathf.Max(1f, holdSpeedMultiplier) : 1f;
    }

    bool ConsumeSkip()
    {
        if (!skipRequested) return false;
        skipRequested = false;
        return true;
    }

    // Wait helpers (instance-scoped)

    public IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (ConsumeSkip()) yield break;
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt * Mathf.Max(1f, speedMult);
            yield return null;
        }
    }

    public IEnumerator AwaitTween(Tween t)
    {
        if (t == null || !t.active) yield break;
        float baseScale = t.timeScale <= 0f ? 1f : t.timeScale;
        while (t.active && t.IsPlaying())
        {
            t.timeScale = baseScale * Mathf.Max(1f, speedMult);
            if (ConsumeSkip()) { t.Goto(t.Duration(true), true); break; }
            yield return null;
        }
        if (t != null && t.active) t.timeScale = baseScale;
    }

    public IEnumerator AwaitTypewriter(TMPTypewriterSwap typer)
    {
        if (!typer) yield break;
        while (typer.IsRunningPublic)
        {
            if (ConsumeSkip()) typer.RequestComplete();
            yield return null;
        }
    }
}

