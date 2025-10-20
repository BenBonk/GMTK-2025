using System.Collections;
using UnityEngine;
using TMPro;

public class TMPTypewriterSwap : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] TMP_Text label; // TextMeshProUGUI or TMP_Text
    [SerializeField] PaletteLetterColorizerRichText richColorizer; // optional

    [Header("Timing (defaults)")]
    [SerializeField, Min(0f)] float eraseDelay = 0.03f;
    [SerializeField, Min(0f)] float typeDelay = 0.03f;
    [SerializeField] bool useUnscaledTime = false;

    [Header("Behavior")]
    [SerializeField] bool stripRichTagsFromInitial = true; // if the existing label has tags
    [SerializeField] bool applyPaletteEveryStep = true;    // placeholder for parity

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

    // New: cache the in-flight target so skipper can complete instantly
    string targetPlain = "";

    // Skipper expects this
    public bool IsRunningPublic => running != null;

    void Reset()
    {
        label = GetComponent<TMP_Text>();
        if (!richColorizer) richColorizer = GetComponent<PaletteLetterColorizerRichText>();
    }

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        currentPlain = label ? label.text : "";
        if (stripRichTagsFromInitial) currentPlain = StripTagsSimple(currentPlain);
        InstantSet(currentPlain);
        lastStepText = currentPlain;
        targetPlain = currentPlain;
    }

    void OnDisable()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    // Fire-and-forget, with optional per-call delay overrides
    public void ChangeTextAnimated(string newText, float? overrideEraseDelay = null, float? overrideTypeDelay = null)
    {
        if (!label) return;
        if (running != null) StopCoroutine(running);

        targetPlain = newText ?? "";

        float localErase = Mathf.Max(0f, overrideEraseDelay ?? eraseDelay);
        float localType = Mathf.Max(0f, overrideTypeDelay ?? typeDelay);

        running = StartCoroutine(CoChangeTextAnimated(targetPlain, localErase, localType));
    }

    // Start and wait; supports optional extra flat wait and per-call delay overrides
    public IEnumerator PlayAndWait(string newText, float extraWait = 0f, float? overrideEraseDelay = null, float? overrideTypeDelay = null)
    {
        ChangeTextAnimated(newText, overrideEraseDelay, overrideTypeDelay);
        while (IsRunningPublic) yield return null;

        if (extraWait > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(extraWait);
            else yield return new WaitForSeconds(extraWait);
        }
    }

    // Duration helper (for DOTween AppendInterval), supports overrides and extra wait
    public float ComputeSwapDuration(string newText, float extraWait = 0f, float? overrideEraseDelay = null, float? overrideTypeDelay = null)
    {
        if (newText == null) newText = "";
        float e = Mathf.Max(0f, overrideEraseDelay ?? eraseDelay);
        float t = Mathf.Max(0f, overrideTypeDelay ?? typeDelay);

        int eraseSteps = Mathf.Max(0, currentPlain.Length); // each removed char yields e
        int typeSteps = Mathf.Max(0, newText.Length - 1);  // last added char does not yield t
        return eraseSteps * e + typeSteps * t + Mathf.Max(0f, extraWait);
    }

    // Instant set (no animation)
    public void InstantSet(string text)
    {
        currentPlain = text ?? "";
        if (richColorizer != null && richColorizer.isActiveAndEnabled)
            richColorizer.SetTextAndApply(currentPlain);
        else
            label.text = currentPlain;

        lastStepText = currentPlain;
    }

    // Skipper calls this to instantly finish the current animation
    public void RequestComplete()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
        // Jump to final target of the current swap
        InstantSet(targetPlain);
        currentPlain = targetPlain;
    }

    IEnumerator CoChangeTextAnimated(string newPlain, float localErase, float localType)
    {
        // ERASE phase
        for (int len = Mathf.Max(0, currentPlain.Length); len >= 0; len--)
        {
            string next = currentPlain.Substring(0, len);
            MaybePlaySfx(lastStepText, next, isErase: true);
            SetStep(next);
            lastStepText = next;

            if (len > 0 && localErase > 0f)
                yield return (useUnscaledTime ? new WaitForSecondsRealtime(localErase) : new WaitForSeconds(localErase));
        }

        // TYPE phase
        for (int len = 1; len <= newPlain.Length; len++)
        {
            string next = newPlain.Substring(0, len);
            MaybePlaySfx(lastStepText, next, isErase: false);
            SetStep(next);
            lastStepText = next;

            if (len < newPlain.Length && localType > 0f)
                yield return (useUnscaledTime ? new WaitForSecondsRealtime(localType) : new WaitForSeconds(localType));
        }

        currentPlain = newPlain;
        running = null;
    }

    void SetStep(string partial)
    {
        if (richColorizer != null && richColorizer.isActiveAndEnabled)
            richColorizer.SetTextAndApply(partial);
        else
            label.text = partial;
    }

    public void SetLabel(TMP_Text newLabel) { label = newLabel; }

    public void SetRichColorizer(PaletteLetterColorizerRichText newRich)
    {
        richColorizer = newRich;
    }

    void MaybePlaySfx(string prevText, string nextText, bool isErase)
    {
        if (!AudioManager.Instance || string.IsNullOrEmpty(sfxName)) return;
        if (isErase && !sfxOnErase) return;
        if (!isErase && !sfxOnType) return;

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (sfxMinInterval > 0f && now - lastSfxTime < sfxMinInterval) return;

        char changedChar = '\0';
        if (isErase)
        {
            if (!string.IsNullOrEmpty(prevText))
                changedChar = prevText[prevText.Length - 1];
        }
        else
        {
            if (!string.IsNullOrEmpty(nextText))
                changedChar = nextText[nextText.Length - 1];
        }

        if (sfxSkipOnWhitespace && changedChar != '\0' && char.IsWhiteSpace(changedChar))
            return;

        AudioManager.Instance.PlaySFX(sfxName, 0.5f);
        lastSfxTime = now;
    }

    public static string StripTagsSimple(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        bool inTag = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '<') { inTag = true; continue; }
            if (inTag) { if (c == '>') inTag = false; continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }
}

