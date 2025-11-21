using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class SynergySlots : MonoBehaviour
{
    private ShopManager shopManager;
    private List<DeckCard> deckCards = new List<DeckCard>();
    private Player player;
    private DescriptionManager descriptionManager;
    private void Start()
    {
        descriptionManager = GameController.descriptionManager;
        shopManager = GameController.shopManager;
        player = GameController.player;
    }

    public void OverrideSynergy(DeckCard card)
    {
        if (shopManager.canOverrideBoon)
        {
            int index = player.boonsInDeck.IndexOf(card.boonData);
            Boon newBoon = shopManager.overridingBoonItem.chosenBoon;
            GameController.player.playerCurrency -= newBoon.price;
            shopManager.UpdateCashText();
            shopManager.overridingBoonItem.canPurchase = false;
            FBPP.SetInt(newBoon.name, FBPP.GetInt(newBoon.name) + 1);
            FBPP.SetInt("totalBoonsPurchased", FBPP.GetInt("totalBoonsPurchased") + 1);
            shopManager.overridingBoonItem.upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.InOutQuad);
            Instantiate(shopManager.purchaseParticles, shopManager.overridingBoonItem.rt.position, Quaternion.identity);

            if (shopManager.overridingBoonItem.chosenBoon.name == "Thief")
            {
                GameController.gameManager.foxThiefStolenStats = shopManager.overridingBoonItem.chosenToSteal.animalData;
                FBPP.SetInt("chosenToStealIndex", shopManager.overridingBoonItem.chosenToStealIndex);
            }
            if (shopManager.overridingBoonItem.chosenBoon.name == "FreshStock" || shopManager.overridingBoonItem.chosenBoon.name == "Freeroll")
            {
                GameController.rerollManager.Reset();
            }

            foreach (Transform child in shopManager.boonDeckParent)
            {
                deckCards.Add(child.GetComponent<DeckCard>());
            }


            shopManager.canOverrideBoon = false;
            string desc = newBoon.desc.GetLocalizedString();
            if (newBoon.name=="Thief")
            {
                desc = newBoon.desc.GetLocalizedString() + " " + GameController.gameManager.foxThiefStolenStats.animalName.GetLocalizedString() + ".";
            }
            card.Initialize(newBoon.synergyName.GetLocalizedString(), desc, newBoon, descriptionManager.GetBoonDescription(newBoon));
            if (newBoon is BasicBoon && !card.subPopup.activeInHierarchy)
            {
                card.subPopup.SetActive(true);   
                card.hoverPopup.DOLocalMoveY(card.hoverPopup.position.y + 100,0);
            }
            
            Debug.Log(index);
            Debug.Log(card.boonData);
            GameController.boonManager.boonDict.Remove(player.boonsInDeck[index].name);
            player.boonsInDeck[index] = newBoon;
            GameController.boonManager.boonDict.Add(newBoon.name, newBoon);
            shopManager.overridingBoonItem = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
            shopManager.instructionsText.DOFade(0f, 0.5f);
            shopManager.cancelOverride.DOFade(0, 0.5f).OnComplete(() => CancelOverride2());
            if (GameController.rerollManager.rerollsPerShop>GameController.rerollManager.rerollsThisShop)
            {
                GameController.rerollManager.transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0.25f).SetEase(Ease.OutBack);       
            }
            shopManager.leaveShopButton.transform.DOScale(new Vector3(1.394933f, 1.394933f, 1), 0.25f).SetEase(Ease.OutBack);
            StartCoroutine(Wait());
            GameController.saveManager.SaveGameData();
        }
    }

    public void CancelOverride()
    {
        shopManager.canOverrideBoon = false;
        shopManager.overridingBoonItem = null;
        shopManager.cantPurchaseItem = false;
        shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
        shopManager.instructionsText.DOFade(0f, 0.5f);
        shopManager.cancelOverride.DOFade(0, 0.5f).OnComplete(() => CancelOverride2());
        if (GameController.rerollManager.rerollsPerShop>GameController.rerollManager.rerollsThisShop)
        {
            GameController.rerollManager.transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0.25f).SetEase(Ease.OutBack);       
        }
        shopManager.leaveShopButton.transform.DOScale(new Vector3(1.394933f, 1.394933f, 1), 0.25f).SetEase(Ease.OutBack);
        StartCoroutine(Wait());
    }

    void CancelOverride2()
    {
        shopManager.cancelOverride.enabled = false;
        shopManager.cancelOverride.gameObject.SetActive(true);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(1);
        if (shopManager.overridingBoonItem==null)
        {
            shopManager.ToggleSynergies();   
        }
    }
}
