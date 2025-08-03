using DG.Tweening;
using TMPro;
using UnityEngine;

public class SynergyShopItem : ShopItem
{
    public Synergy[] possibleSynergies;
    private Synergy chosenSynergy;
    public SynergySlots synergySlots;
    public TMP_Text desc2;
    private int value;
    public void SetInt(int val)
    {
        value = val;
    }
    public override void Initialize()
    {
        chosenSynergy = possibleSynergies[value];
        titleText.text = chosenSynergy.name;
        descriptionText.text = chosenSynergy.desc;
        priceText.text = chosenSynergy.price.ToString();
        price = chosenSynergy.price;
        upgradeArt.sprite = chosenSynergy.art;
        desc2.text = "";
        if (chosenSynergy.pointsBonus!=0)
        {
            if (chosenSynergy.pointsBonus<0)
            {
                desc2.text += ("Points loss: " + chosenSynergy.pointsBonus + "\n");
            }
            else
            {
                desc2.text += ("Points bonus: +" + chosenSynergy.pointsBonus + "\n");   
            }
        }
        if (chosenSynergy.pointsMult!=1)
        {
            desc2.text += ("Points mult: x" + chosenSynergy.pointsMult + "\n");
        }
        if (chosenSynergy.currencyBonus!=0)
        {
            if (chosenSynergy.currencyBonus<0)
            {
                desc2.text += ("Cash loss: " + chosenSynergy.currencyBonus + "\n");
            }
            else
            {
                desc2.text += ("Cash bonus: +" + chosenSynergy.currencyBonus + "\n");   
            }
        }
        if (chosenSynergy.currencyMult!=1)
        {
            desc2.text += ("Cash mult: x" + chosenSynergy.currencyMult + "\n");
        }
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.InOutQuad);
            if (GameController.player.synergiesInDeck.Count<3)
            {
                GameController.player.AddSynergyToDeck(chosenSynergy);
                shopManager.UpdateSynergies();
            }
            else
            {
                shopManager.synergiesOpen = false;
                shopManager.ToggleSynergies();  
                shopManager.darkCover.DOFade(.75f, 0.5f);
                shopManager.instructionsText.DOFade(1, 0.5f);
                shopManager.cantPurchaseItem = true;
                shopManager.overridingSynergy = chosenSynergy;
                synergySlots.canOverrideSynergy = true;
            }

        }
    }
}
