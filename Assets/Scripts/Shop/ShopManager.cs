using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    private Player player;
    public ShopItem[] shopItems;
    public GameObject deckCardPrefab;
    public RectTransform deckParent;
    //continue

    private void Start()
    {
        player = GameController.player;
        InitializeAllUpgrades();
    }

    public void InitializeAllUpgrades()
    {
        //needs to be called when entering shop
        foreach (var shopItem in shopItems)
        {
            shopItem.canPurchase = true;
            shopItem.Initialize();
        }

        foreach (Transform child in deckParent.transform)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, (int count, GameObject reference)> uniqueObjects =
            new Dictionary<string, (int, GameObject)>();
        
        foreach (GameObject obj in player.animalsInDeck)
        {
            string name = obj.name;

            if (uniqueObjects.ContainsKey(name))
            {
                uniqueObjects[name] = (uniqueObjects[name].count + 1, uniqueObjects[name].reference);
            }
            else
            {
                uniqueObjects[name] = (1, obj);
            }
        }

        // Step 2: Create a card for each unique GameObject
        foreach (var entry in uniqueObjects)
        {
            string name = entry.Key;
            int count = entry.Value.count;
            GameObject reference = entry.Value.reference;
            GameObject card = Instantiate(deckCardPrefab, deckParent);
            card.GetComponent<DeckCard>().Initialize(name + " - x" + count);
        }

    }
    
    
}