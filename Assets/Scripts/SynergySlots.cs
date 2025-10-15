using DG.Tweening;
using System;
using System.Collections;
using System.Diagnostics;
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


            canOverrideBoon = false;
            string desc = newBoon.desc.GetLocalizedString();
            if (newBoon.name=="Thief")
            {
                desc = newBoon.desc.GetLocalizedString() + " " + GameController.gameManager.foxThiefStolenStats.animalName.GetLocalizedString() + ".";
            }
            deckCards[index].Initialize(newBoon.synergyName.GetLocalizedString(), desc, newBoon.art, descriptionManager.GetBoonDescription(newBoon));
            if (newBoon is BasicBoon && !deckCards[index].subPopup.activeInHierarchy)
            {
                deckCards[index].subPopup.SetActive(true);   
                deckCards[index].hoverPopup.DOLocalMoveY(deckCards[index].hoverPopup.position.y + 100,0);
            }
            GameController.boonManager.boonDict.Remove(player.boonsInDeck[index].name);
            player.boonsInDeck[index] = newBoon;
            GameController.boonManager.boonDict.Add(newBoon.name, newBoon);
            shopManager.overridingBoonItem = null;
            shopManager.cantPurchaseItem = false;
            shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
            shopManager.instructionsText.DOFade(0f, 0.5f);
            shopManager.cancelOverride.DOFade(0, 0.5f).OnComplete(() => shopManager.cancelOverride.enabled = false);
            GameController.rerollManager.transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0.25f).SetEase(Ease.OutBack);
            shopManager.leaveShopButton.transform.DOScale(new Vector3(1.394933f, 1.394933f, 1), 0.25f).SetEase(Ease.OutBack);
            StartCoroutine(Wait());
            GameController.saveManager.SaveGameData();
        }
    }

    public void CancelOverride()
    {
        canOverrideBoon = false;
        shopManager.overridingBoonItem = null;
        shopManager.cantPurchaseItem = false;
        shopManager.darkCover.DOFade(0f, 0.5f).OnComplete(() => shopManager.darkCover.enabled = false);
        shopManager.instructionsText.DOFade(0f, 0.5f);
        shopManager.cancelOverride.DOFade(0, 0.5f).OnComplete(() => shopManager.cancelOverride.enabled = false);
        GameController.rerollManager.transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0.25f).SetEase(Ease.OutBack);
        shopManager.leaveShopButton.transform.DOScale(new Vector3(1.394933f, 1.394933f, 1), 0.25f).SetEase(Ease.OutBack);
        StartCoroutine(Wait());
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
