using System;
using DG.Tweening;
using UnityEngine;

public class SynergySlots : MonoBehaviour
{
    public bool canOverrideSynergy;
    private ShopManager shopManager;
    private DeckCard[] deckCards;
    private Player player;
    private void Start()
    {
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
            deckCards[index].Initialize(newSynergy.name, newSynergy.desc, newSynergy.art);
            player.synergiesInDeck[index] = newSynergy;
            shopManager.overridingSynergy = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f);
        }
    }
}
