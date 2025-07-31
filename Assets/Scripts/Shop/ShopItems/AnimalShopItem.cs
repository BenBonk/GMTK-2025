using UnityEngine;

public class AnimalShopItem : ShopItem
{
    public override void Initialize()
    {
        
    }
    public override void PurchaseUpgrade()
    {
        GameController.player.AddAnimalToDeck();
    }
}
