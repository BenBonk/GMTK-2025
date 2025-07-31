using System;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    private Player player;
    public ShopItem[] shopItems;
    //continue

    private void Start()
    {
        player = GameController.player;
        InitializeAllUpgrades();
    }

    public void InitializeAllUpgrades()
    {
        foreach (var shopItem in shopItems)
        {
            shopItem.canPurchase = true;
            shopItem.Initialize();
        }
    }
    
    
}