using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public string highlightSFX = "ui_hover";
    public string clickSFX = "ui_click";

    private bool hoveredThisFrame = false;

    private void Update()
    {
        // Reset hover flag each frame
        hoveredThisFrame = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hoveredThisFrame)
        {
            hoveredThisFrame = true;
            if (!string.IsNullOrEmpty(highlightSFX))
                AudioManager.Instance?.PlaySFX(highlightSFX);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(clickSFX))
            AudioManager.Instance?.PlaySFX(clickSFX);
    }
}
