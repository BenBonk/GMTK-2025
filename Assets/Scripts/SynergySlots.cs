using System;
using DG.Tweening;
using UnityEngine;

public class SynergySlots : MonoBehaviour
{
    public bool canOverrideBoon;
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
        if (canOverrideBoon)
        {
            Boon newBoon = shopManager.overridingBoon;
            canOverrideBoon = false;
            deckCards[index].Initialize(newBoon.synergyName.GetLocalizedString(), newBoon.desc.GetLocalizedString(), newBoon.art, descriptionManager.GetBoonDescription(newBoon));
            GameController.boonManager.RemoveBoon(player.boonsInDeck[index]);
            player.boonsInDeck[index] = newBoon;
            GameController.boonManager.AddBoon(player.boonsInDeck[index]);
            shopManager.overridingBoon = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
            shopManager.instructionsText.DOFade(0f, 0.5f);
            shopManager.Invoke("ToggleSynergies", 1);
            GameController.saveManager.SaveGameData();
        }
    }
}
