using DG.Tweening;
using UnityEngine;

public class SynergyShopItem : ShopItem
{
    public Synergy[] possibleSynergies;
    private Synergy chosenSynergy;
    public SynergySlots synergySlots;

    public override void Initialize()
    {
        chosenSynergy = possibleSynergies[Random.Range(0, possibleSynergies.Length)];
        titleText.text = chosenSynergy.name;
        descriptionText.text = chosenSynergy.desc;
        priceText.text = chosenSynergy.price.ToString();
        price = chosenSynergy.price;
        upgradeArt.sprite = chosenSynergy.art;
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            canPurchase = false;
            upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
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
