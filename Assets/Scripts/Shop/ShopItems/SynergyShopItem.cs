using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SynergyShopItem : ShopItem
{
    [HideInInspector] public Boon chosenBoon;
    public SynergySlots synergySlots;
    public TMP_Text desc2;
    [HideInInspector] public Animal chosenToSteal;
    [HideInInspector] public int chosenToStealIndex;
    private Image bgSR;
    private Image popupBg;
    public Sprite[] boonBgs;
    public GameObject subPopup;
    private void Awake()
    {
        bgSR = GetComponent<Image>();
        popupBg = hoverPopup.gameObject.GetComponent<Image>();
    }

    public void SetBoon(Boon boon)
    {
        chosenBoon = boon;
    }
    public override void Initialize()
    {
        titleText.text = chosenBoon.synergyName.GetLocalizedString();
        descriptionText.text = chosenBoon.desc.GetLocalizedString();
        priceText.text = chosenBoon.price.ToString();
        price = chosenBoon.price;
        upgradeArt.sprite = chosenBoon.art;
        int index = 0;
        if (chosenBoon is BasicBoon basicBoon)
        {
            desc2.text = GameController.descriptionManager.GetBoonDescription(chosenBoon);
            subPopup.SetActive(true);
        }
        else if (chosenBoon is SpecialtyBoon specialtyBoon)
        {
            subPopup.SetActive(false);
            index = 1;
        }
        else if (chosenBoon is LegendaryBoon legendaryBoon)
        {
            subPopup.SetActive(false);
            if (chosenBoon.name=="Thief")
            {
                chosenToStealIndex = Random.Range(0, GameController.gameManager.animalShopItem.possibleAnimals.Length);
                chosenToSteal = GameController.gameManager.animalShopItem.possibleAnimals[chosenToStealIndex];
                descriptionText.text = chosenBoon.desc.GetLocalizedString() + " " + chosenToSteal.name + ".";
            }
            index = 2;
        }

        bgSR.sprite = shopManager.boonShopPanels[index].bgArt;
        popupBg.sprite = shopManager.boonShopPanels[index].popupArt;
        titleText.color = shopManager.boonShopPanels[index].titleColor;
        priceText.color = shopManager.boonShopPanels[index].costColor;
        descriptionText.color = shopManager.boonShopPanels[index].popupColor;
        bgSR.SetNativeSize();
        popupBg.SetNativeSize();
        
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && (GameController.player.playerCurrency >= price || (GameController.boonManager.ContainsBoon("BNPL") && GameController.player.playerCurrency - price >=-200)))
        {
            AudioManager.Instance.PlaySFX("ui_click");
            AudioManager.Instance.PlaySFX("coins");

            if (GameController.player.boonsInDeck.Count<GameController.gameManager.maxSynergies)
            {
                GameController.player.playerCurrency -= price;
                shopManager.UpdateCashText();
                canPurchase = false;
                FBPP.SetInt(chosenBoon.name, FBPP.GetInt(chosenBoon.name) + 1);
                FBPP.SetInt("totalBoonsPurchased", FBPP.GetInt("totalBoonsPurchased") + 1);
                upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.InOutQuad);
                Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);

                if (chosenBoon.name == "Thief")
                {
                    GameController.gameManager.foxThiefStolenStats = chosenToSteal.animalData;
                    FBPP.SetInt("chosenToStealIndex", chosenToStealIndex);
                }
                if (chosenBoon.name == "FreshStock" || chosenBoon.name == "Freeroll")
                {
                    GameController.rerollManager.Reset();
                }
                GameController.player.AddBoonToDeck(chosenBoon);
                shopManager.UpdateSynergies(shopManager.boonDeckParent);
            }
            else
            {
                shopManager.synergiesOpen = false;
                shopManager.ToggleSynergies();  
                shopManager.darkCover.enabled = true;
                shopManager.darkCover.DOFade(.75f, 0.5f);
                shopManager.instructionsText.DOFade(1, 0.5f);
                shopManager.cantPurchaseItem = true;
                shopManager.overridingBoonItem = this;
                shopManager.cancelOverride.enabled = true;
                shopManager.cancelOverride.DOFade(1, 0.5f);
                GameController.rerollManager.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
                shopManager.leaveShopButton.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
                shopManager.canOverrideBoon = true;
            }

        }
        else
        {
            AudioManager.Instance.PlaySFX("no_point_mult");
        }
    }
}
