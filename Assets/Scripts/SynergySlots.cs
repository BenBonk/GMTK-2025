using System;
using DG.Tweening;
using UnityEngine;

public class SynergySlots : MonoBehaviour
{
    public bool canOverrideSynergy;
    private ShopManager shopManager;
    private DeckCard[] deckCards;
    private Player player;
    private DescriptionManager descriptionManager;
    private void Start()
    {
        descriptionManager = GameController.descriptionManager;
        shopManager = GameController.shopManager;
        player = GameController.player;
        deckCards = shopManager.synergyCards;
    }

    public void OverrideSynergy(int index)
    {
        if (canOverrideSynergy)
        {
            Synergy newSynergy = shopManager.overridingSynergy;
            canOverrideSynergy = false;
            deckCards[index].Initialize(newSynergy.synergyName, newSynergy.desc, newSynergy.art, descriptionManager.GetSynergyDescription(newSynergy));
            player.synergiesInDeck[index] = newSynergy;
            shopManager.overridingSynergy = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f);
            shopManager.instructionsText.DOFade(0f, 0.5f);
            shopManager.Invoke("ToggleSynergies", 1);
        }
    }
}
