using System.Text;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class PaletteLetterColorizerRichText : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] Color[] palette = { Color.red, Color.green, Color.blue };
    [SerializeField] bool includeSpaces = false;
    [SerializeField] bool applyOnEnable = true;

    // If set, use this as the source text. If empty, use label.text.
    [TextArea, SerializeField] string sourceOverride = "";

    void Reset() { label = GetComponent<TMP_Text>(); }
    void OnValidate() { if (label == null) label = GetComponent<TMP_Text>(); }
    void OnEnable() { if (applyOnEnable) Apply(); }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        if (!label || palette == null || palette.Length == 0) return;

        string raw = string.IsNullOrEmpty(sourceOverride) ? label.text : sourceOverride;
        if (string.IsNullOrEmpty(raw)) return;

        var sb = new StringBuilder(raw.Length * 16);
        bool inTag = false;
        int colorIndex = 0;

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            // Pass TMP/HTML tags through unchanged without advancing the palette.
            if (c == '<')
            {
                inTag = true;
                sb.Append(c);
                continue;
            }
            if (inTag)
            {
                sb.Append(c);
                if (c == '>') inTag = false;
                continue;
            }

            bool isWhitespace = char.IsWhiteSpace(c);
            if (isWhitespace && !includeSpaces)
            {
                sb.Append(c);
                continue;
            }

            string hex = ColorUtility.ToHtmlStringRGBA(palette[colorIndex % palette.Length]);
            sb.Append("<color=#").Append(hex).Append(">").Append(c).Append("</color>");
            colorIndex++;
        }

        label.text = sb.ToString();
    }

    // Call this if you change the text at runtime and want to reapply.
    public void SetTextAndApply(string newText)
    {
        sourceOverride = newText;
        Apply();
    }
}