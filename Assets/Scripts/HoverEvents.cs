using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public DeckCard card;

    public void OnPointerEnter(PointerEventData eventData)
    {
        card.HoverOver();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        card.HoverOverExit();
    }
}
