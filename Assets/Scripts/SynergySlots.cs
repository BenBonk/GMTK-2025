using System;
using System.Collections;
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
            string desc = newBoon.desc.GetLocalizedString();
            if (newBoon.name=="Thief")
            {
                desc = newBoon.desc.GetLocalizedString() + " " + GameController.gameManager.foxThiefStolenStats.animalName.GetLocalizedString() + ".";
            }
            deckCards[index].Initialize(newBoon.synergyName.GetLocalizedString(), desc, newBoon.art, descriptionManager.GetBoonDescription(newBoon));
            if (newBoon is BasicBoon && !deckCards[index].subPopup.activeInHierarchy)
            {
                Debug.Log("call");
                deckCards[index].subPopup.SetActive(true);   
                deckCards[index].hoverPopup.DOLocalMoveY(deckCards[index].hoverPopup.position.y + 100,0);
            }
            GameController.boonManager.boonDict.Remove(player.boonsInDeck[index].name);
            player.boonsInDeck[index] = newBoon;
            GameController.boonManager.boonDict.Add(newBoon.name, newBoon);
            shopManager.overridingBoon = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
            shopManager.instructionsText.DOFade(0f, 0.5f);
            StartCoroutine(Wait());
            GameController.saveManager.SaveGameData();
        }
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(1);
        if (shopManager.overridingBoon==null)
        {
            shopManager.ToggleSynergies();   
        }
    }
}
