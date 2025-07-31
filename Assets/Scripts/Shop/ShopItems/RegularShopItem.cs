using UnityEngine;

public class RegularShopItem : ShopItem
{
    public bool increaseLassos;
    public bool removeCard;
    private int timesPurchased;
    public override void Initialize()
    {
        price = 25 + 25 * timesPurchased;
    }
    public override void PurchaseUpgrade()
    {
        if (canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            canPurchase = false;
        }
        timesPurchased++;
        if (increaseLassos)
        {
            IncreaseLassos();
        }
        if (removeCard)
        {
            RemoveCard();
        }
    }

    public void IncreaseLassos()
    { 
        //Implement
    }

    public void RemoveCard()
    {
        //Implement
    }
}
