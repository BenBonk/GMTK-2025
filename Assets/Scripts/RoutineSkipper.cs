using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

[DisallowMultipleComponent]
public class RoutineSkipper : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Control")]
    public bool clickSkipsCurrentStep = true;   // one click = finish the current wait/tween/typewriter
    public bool holdToSpeedUp = true;
    [Min(1f)] public float holdSpeedMultiplier = 8f;
    public bool useUnscaledTime = false;

    float speedMult = 1f;
    bool skipRequested = false;

    // --- Input hooks ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSkipsCurrentStep) skipRequested = true;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (holdToSpeedUp) speedMult = holdSpeedMultiplier;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdToSpeedUp) speedMult = 1f;
    }

    // Consume one skip request
    public bool ConsumeSkip()
    {
        if (!skipRequested) return false;
        skipRequested = false;
        return true;
    }

    // ----- Helper waits you can use inside your coroutine -----

    // Wait for seconds, but allow click-to-skip and hold speed-up
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

    // Wait for a tween to finish, but allow click-to-skip and hold speed-up
    public IEnumerator AwaitTween(Tween t)
    {
        if (t == null || !t.active) yield break;
        float originalScale = t.timeScale;
        t.timeScale = originalScale * Mathf.Max(1f, speedMult);

        while (t.active && t.IsPlaying())
        {
            // adjust tween speed while holding
            t.timeScale = originalScale * Mathf.Max(1f, speedMult);

            if (ConsumeSkip())
            {
                // jump this tween to its end and finish this step
                t.Goto(t.Duration(true), true);
                break;
            }
            yield return null;
        }

        if (t != null && t.active) t.timeScale = originalScale;
    }

    // Wait for a TMPTypewriterSwap to finish, but allow click-to-skip this whole step
    public IEnumerator AwaitTypewriter(TMPTypewriterSwap typer)
    {
        if (!typer) yield break;

        // If we want a single click to finish the whole "type" step:
        while (typer.IsRunningPublic)
        {
            if (ConsumeSkip())
            {
                typer.RequestComplete(); // see step 1
            }
            yield return null;
        }
    }
}

