using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    private Transform leaveParent;
    Vector3 leavePos;
    private void Awake()
    {
        bgSR = GetComponent<Image>();
        popupBg = hoverPopup.gameObject.GetComponent<Image>();
    }

    public override void Start()
    {
        shopManager = GameController.shopManager;
        leaveParent = shopManager.leaveShopButton.transform.parent;
        //leavePos = shopManager.leaveShopButton.transform.position;
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
        if (!shopManager.cantPurchaseItem && canPurchase && (GameController.player.playerCurrency >= price || (GameController.boonManager.ContainsBoon("BNPL") && GameController.player.playerCurrency - price >=-150)))
        {
            AudioManager.Instance.PlaySFX("ui_click");
            AudioManager.Instance.PlaySFX("coins");

            if (GameController.player.boonsInDeck.Count<FBPP.GetInt("boonDeckSize",5))
            {
                shopManager.UpdateCashText();
                canPurchase = false;
                FBPP.SetInt(chosenBoon.name, FBPP.GetInt(chosenBoon.name) + 1);
                FBPP.SetInt("totalBoonsPurchased", FBPP.GetInt("totalBoonsPurchased") + 1);
                upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.InOutQuad);
                Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
                foreach (var s in shopManager.shopItems)
                {
                    s.StopAllCoroutines();
                    DOTween.KillAll(s);
                }
                StartCoroutine(DeckPulse());
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

                //triggers save, so goes last
                GameController.player.playerCurrency -= price;
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
                shopManager.cancelOverride.gameObject.SetActive(true);
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
    IEnumerator DeckPulse()
    {
        shopManager.cantToggleSynergiesDeck = true;
        shopManager.leaveShopButton.transform.SetParent(shopManager.transform);
        yield return new WaitForSeconds(.25f);
        Sequence pulse = DOTween.Sequence();
        pulse.Append(shopManager.boonDeckButton.transform.DOScale(1.10f, 0.1f).SetEase(Ease.OutBack));
        pulse.Append(shopManager.boonDeckButton.transform.DOShakeRotation(
            duration: 0.15f,
            strength: new Vector3(0f, 0f, 6f), 
            vibrato: 5,
            randomness: 90,
            fadeOut: true
        ));
        pulse.Append(shopManager.boonDeckButton.transform.DOScale(1f, 0.15f).SetEase(Ease.OutExpo));
        pulse.Join(shopManager.boonDeckButton.transform.DOLocalRotate(Vector3.zero, 0.15f, RotateMode.Fast));
        yield return new WaitForSeconds(.5f);
        shopManager.leaveShopButton.transform.SetParent(leaveParent);
        //shopManager.leaveShopButton.transform.position = leavePos;
        shopManager.cantToggleSynergiesDeck = false;
    }
}
