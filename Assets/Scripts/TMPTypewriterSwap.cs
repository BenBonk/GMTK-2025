using System.Collections;
using UnityEngine;
using TMPro;

public class TMPTypewriterSwap : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] TMP_Text label; // TextMeshProUGUI or TMP_Text
    [SerializeField] PaletteLetterColorizerRichText richColorizer; // optional
    //[SerializeField] PaletteLetterColorizerTMP meshColorizer;       // optional

    [Header("Timing")]
    [SerializeField, Min(0f)] float eraseDelay = 0.03f;
    [SerializeField, Min(0f)] float typeDelay = 0.03f;
    [SerializeField] bool useUnscaledTime = false;

    [Header("Behavior")]
    [SerializeField] bool stripRichTagsFromInitial = true; // if the existing label has tags
    [SerializeField] bool applyPaletteEveryStep = true;    // recolor on each step

    [Header("SFX")]
    [SerializeField] string sfxName = "ui_hover";
    [SerializeField] bool sfxOnErase = true;
    [SerializeField] bool sfxOnType = true;
    [SerializeField] bool sfxSkipOnWhitespace = true;
    [SerializeField, Min(0f)] float sfxMinInterval = 0f;   // throttle rapid ticks

    Coroutine running;
    string currentPlain = "";
    string lastStepText = "";
    float lastSfxTime = -999f;

    void Reset()
    {
        label = GetComponent<TMP_Text>();
        if (!richColorizer) richColorizer = GetComponent<PaletteLetterColorizerRichText>();
        //if (!meshColorizer) meshColorizer = GetComponent<PaletteLetterColorizerTMP>();
    }

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        currentPlain = label ? label.text : "";
        if (stripRichTagsFromInitial) currentPlain = StripTagsSimple(currentPlain);
        InstantSet(currentPlain);
        lastStepText = currentPlain;
    }

    void OnDisable()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    // Public API: animate swap
    public void ChangeTextAnimated(string newText)
    {
        if (!label) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoChangeTextAnimated(newText ?? ""));
    }

    // Optional: instantly set without animation (keeps palette applied)
    public void InstantSet(string text)
    {
        currentPlain = text ?? "";
        if (richColorizer)
        {
            richColorizer.SetTextAndApply(currentPlain);
        }
        else
        {
            label.text = currentPlain;
            //if (meshColorizer) meshColorizer.Apply();
        }
        lastStepText = currentPlain;
    }

    IEnumerator CoChangeTextAnimated(string newPlain)
    {
        // ERASE phase
        for (int len = Mathf.Max(0, currentPlain.Length); len >= 0; len--)
        {
            string next = currentPlain.Substring(0, len);
            MaybePlaySfx(lastStepText, next, isErase: true);
            SetStep(next);
            lastStepText = next;

            if (len > 0 && eraseDelay > 0f)
                yield return (useUnscaledTime ? new WaitForSecondsRealtime(eraseDelay) : new WaitForSeconds(eraseDelay));
        }

        // TYPE phase
        for (int len = 1; len <= newPlain.Length; len++)
        {
            string next = newPlain.Substring(0, len);
            MaybePlaySfx(lastStepText, next, isErase: false);
            SetStep(next);
            lastStepText = next;

            if (len < newPlain.Length && typeDelay > 0f)
                yield return (useUnscaledTime ? new WaitForSecondsRealtime(typeDelay) : new WaitForSeconds(typeDelay));
        }

        currentPlain = newPlain;
        running = null;
    }

    void SetStep(string partial)
    {
        if (richColorizer)
        {
            richColorizer.SetTextAndApply(partial);
        }
        else
        {
            label.text = partial;
            //if (applyPaletteEveryStep && meshColorizer) meshColorizer.Apply();
        }
    }

    void MaybePlaySfx(string prevText, string nextText, bool isErase)
    {
        if (!AudioManager.Instance || string.IsNullOrEmpty(sfxName)) return;
        if (isErase && !sfxOnErase) return;
        if (!isErase && !sfxOnType) return;

        // Throttle
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (sfxMinInterval > 0f && now - lastSfxTime < sfxMinInterval) return;

        // Determine the changed char to optionally skip whitespace sounds
        char changedChar = '\0';
        if (isErase)
        {
            // Removed last char from prevText
            if (!string.IsNullOrEmpty(prevText))
                changedChar = prevText[prevText.Length - 1];
        }
        else
        {
            // Added last char to nextText
            if (!string.IsNullOrEmpty(nextText))
                changedChar = nextText[nextText.Length - 1];
        }

        if (sfxSkipOnWhitespace && changedChar != '\0' && char.IsWhiteSpace(changedChar))
            return;

        AudioManager.Instance.PlaySFX(sfxName, 0.5f);
        lastSfxTime = now;
    }

    // Simple angle-bracket tag stripper
    static string StripTagsSimple(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        bool inTag = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '<') { inTag = true; continue; }
            if (inTag)
            {
                if (c == '>') inTag = false;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}


