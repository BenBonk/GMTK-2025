using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(DeckCard))] // ensure the card is on this object
public class ClickRemoveAnimal : MonoBehaviour, IPointerClickHandler
{
    [Header("Options")]
    public bool requireLeftButton = true;       // UI clicks: only left mouse

    // UI click (needs EventSystem + GraphicRaycaster on Canvas)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (requireLeftButton && eventData.button != PointerEventData.InputButton.Left) return;
        InvokeRemove();
    }

    void InvokeRemove()
    {
        var card = GetComponent<DeckCard>();
        if (!card)
        {
            Debug.LogWarning("ClickRemoveAnimal: DeckCard not found.");
            return;
        }

        AudioManager.Instance.PlaySFX(card.animalData.name);
        GameController.challengeRewardSelect.RemoveAnimal(card.animalData);
    }
}

