using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TMP_Text))]
public class TMPAutoSizeClamp : MonoBehaviour
{
    public RectTransform parent;   // assign parent rect
    public float absoluteMax = 0f; // optional hard cap (0 = ignore)

    private TMP_Text tmp;
    private RectTransform rect;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        rect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        float preferred = tmp.preferredWidth;              // width required for text
        float parentLimit = parent ? parent.rect.width : Mathf.Infinity;
        if (absoluteMax > 0f) parentLimit = Mathf.Min(parentLimit, absoluteMax);

        float target = Mathf.Min(preferred, parentLimit);  // clamp to parent or max
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, target);
    }
}
