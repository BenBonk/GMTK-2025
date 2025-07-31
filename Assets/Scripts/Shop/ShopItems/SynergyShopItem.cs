using UnityEngine;

public class SynergyShopItem : ShopItem
{
    public Synergy[] possibleSynergies;
    private Synergy chosenSynergy;

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
        if (canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            canPurchase = false;
            if (GameController.player.synergiesInDeck.Count<3)
            {
                GameController.player.AddSynergyToDeck(chosenSynergy);
            }
        }
    }
}
