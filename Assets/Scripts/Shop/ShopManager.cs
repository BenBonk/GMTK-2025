using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    private Player player;
    public ShopItem[] shopItems;
    public DeckCard[] synergyCards;
    public GameObject deckCardPrefab;
    public RectTransform deckParent;

    public RectTransform deckPanel;
    public RectTransform synergiesPanel;
    public GameObject synergiesVisual;
    public TMP_Text instructionsText;
    private bool deckOpen;
    [HideInInspector]public bool synergiesOpen;

    [HideInInspector]public Synergy overridingSynergy;
    public Image darkCover;
    public bool cantPurchaseItem;
    public TMP_Text cashText;
    //continue

    private void Start()
    {
        player = GameController.player;
        player.playerCurrency = 10000;
        InitializeAllUpgrades();
    }

    public void InitializeAllUpgrades()
    {
        UpdateCashText();
        
        //needs to be called when entering shop
        foreach (var shopItem in shopItems)
        {
            shopItem.canPurchase = true;
            shopItem.Initialize();
            shopItem.transform.DOScale(Vector3.one, .3f).SetEase(Ease.OutBack);
        }
        UpdateDeck();
        UpdateSynergies();
    }

    public void UpdateCashText()
    {
        cashText.text = "CASH: " + player.playerCurrency; 
    }

    public void UpdateSynergies()
    {
        foreach (DeckCard synergyCard in synergyCards)
        {
            synergyCard.gameObject.SetActive(false);
        }

        for (int i = 0; i < player.synergiesInDeck.Count; i++)
        {
            synergyCards[i].gameObject.SetActive(true);
            synergyCards[i].Initialize( player.synergiesInDeck[i].name, player.synergiesInDeck[i].desc, player.synergiesInDeck[i].art);
        }
    }

    public void UpdateDeck()
    {
        foreach (Transform child in deckParent.transform)
        {
            Destroy(child.gameObject);
        }
        Dictionary<string, (int count, AnimalData reference)> uniqueObjects =
            new Dictionary<string, (int, AnimalData)>();
        
        
        foreach (AnimalData obj in player.animalsInDeck)
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
            int count = entry.Value.count;
            AnimalData reference = entry.Value.reference;
            GameObject card = Instantiate(deckCardPrefab, deckParent);
            card.GetComponent<DeckCard>().Initialize("x" + count, reference.description, reference.deckIcon);
        }
    }


    public void ToggleDeck()
    {
        if (cantPurchaseItem)
        {
            return;
        }
        deckOpen = !deckOpen;
        if (deckOpen)
        {
            deckPanel.gameObject.SetActive(true);
            deckPanel.DOAnchorPosX(-1059, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            deckPanel.DOAnchorPosX(-1469, .25f).SetEase(Ease.InOutQuad).OnComplete(()=>deckPanel.gameObject.SetActive(false));
        }
    }

    public void ToggleSynergies()
    {
        synergiesOpen = !synergiesOpen;
        if (synergiesOpen)
        {
            synergiesVisual.SetActive(true);
            synergiesPanel.DOAnchorPosX(1148, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            synergiesPanel.DOAnchorPosX(1577, .25f).SetEase(Ease.InOutQuad).OnComplete(()=>synergiesVisual.SetActive(false));
        }
    }
    
}