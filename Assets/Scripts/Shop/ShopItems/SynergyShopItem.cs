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
        if (GameController.shopManager.isTut)
        {
            value = 0;
        }
        chosenSynergy = possibleSynergies[value];
        titleText.text = chosenSynergy.synergyName;
        descriptionText.text = chosenSynergy.desc;
        priceText.text = chosenSynergy.price.ToString();
        price = chosenSynergy.price;
        upgradeArt.sprite = chosenSynergy.art;

        desc2.text = GameController.descriptionManager.GetSynergyDescription(chosenSynergy);
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
