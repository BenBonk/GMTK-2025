using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    private Player player;
    private DescriptionManager descriptionManager;
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

    [HideInInspector]public Boon overridingBoon;
    public Image darkCover;
    public bool cantPurchaseItem;
    public bool isTut;
    public TMP_Text cashText;
    public LocalizedString cashLocalString;
    public TMP_Text roundText;
    public LocalizedString roundLocalString;
    public GameObject purchaseParticles;
    public BoonShopPanel[] boonShopPanels;

    private IEnumerator Start()
    {
        descriptionManager = GameController.descriptionManager;
        player = GameController.player;
        yield return new WaitForEndOfFrame();
        InitializeAllUpgrades();
    }

    public void InitializeAllUpgrades()
    {
        UpdateCashText();
        
        //needs to be called when entering shop
        List<int> boonIndexes = new List<int>();
        while (boonIndexes.Count<3)
        {
            int val = Random.Range(0, 24);
            if (!isTut)
            {
                if (boonIndexes.Contains(val) || player.boonsInDeck.Contains(shopItems[0].GetComponent<SynergyShopItem>().possibleSynergies[val]))
                {
                    continue;
                }   
            }
            boonIndexes.Add(val);
        }

        for (int i = 0; i < 3; i++)
        {
            shopItems[i].GetComponent<SynergyShopItem>().SetInt(boonIndexes[i]);
        }
        foreach (var shopItem in shopItems)
        {
            Instantiate(purchaseParticles, shopItem.rt.position, Quaternion.identity);
            shopItem.canPurchase = true;
            shopItem.Initialize();
            shopItem.transform.DOScale(Vector3.one, .3f).SetEase(Ease.OutBack);
        }
        UpdateDeck();
        UpdateSynergies();
    }

    public void UpdateCashText()
    {
        cashText.text = cashLocalString.GetLocalizedString() +" "+ LassoController.FormatNumber(player.playerCurrency);
        roundText.text = roundLocalString.GetLocalizedString() +" "+ (GameController.gameManager.roundNumber);
        Sequence pulse = DOTween.Sequence();
        pulse.Append(cashText.transform.DOScale(1.10f, 0.1f).SetEase(Ease.OutBack));
        pulse.Append(cashText.transform.DOShakeRotation(
            duration: 0.15f,
            strength: new Vector3(0f, 0f, 6f), 
            vibrato: 5,
            randomness: 90,
            fadeOut: true
        ));
        pulse.Append(cashText.transform.DOScale(1f, 0.15f).SetEase(Ease.OutExpo));
        pulse.Join(cashText.transform.DOLocalRotate(Vector3.zero, 0.15f, RotateMode.Fast));
    }

    public void UpdateSynergies()
    {
        foreach (DeckCard synergyCard in synergyCards)
        {
            synergyCard.gameObject.SetActive(false);
        }

        for (int i = 0; i < player.boonsInDeck.Count; i++)
        {
            synergyCards[i].gameObject.SetActive(true);
            synergyCards[i].Initialize( player.boonsInDeck[i].synergyName.GetLocalizedString(), player.boonsInDeck[i].desc.GetLocalizedString(), player.boonsInDeck[i].art, descriptionManager.GetBoonDescription(player.boonsInDeck[i]));
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
            string name = obj.animalName.GetLocalizedString();

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
            string desc = descriptionManager.GetAnimalDescription(reference);
            
            GameObject card = Instantiate(deckCardPrefab, deckParent);
            card.GetComponent<DeckCard>().Initialize("x" + count, desc, reference.deckIcon);
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
            deckPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            deckPanel.DOAnchorPosX(-415, .25f).SetEase(Ease.InOutQuad).OnComplete(()=>deckPanel.gameObject.SetActive(false));
        }
    }

    public void ToggleSynergies()
    {
        synergiesOpen = !synergiesOpen;
        if (synergiesOpen)
        {
            synergiesVisual.SetActive(true);
            synergiesPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            synergiesPanel.DOAnchorPosX(415, .25f).SetEase(Ease.InOutQuad).OnComplete(() => deckPanel.gameObject.SetActive(false));
        }
    }

    [System.Serializable]
    public class BoonShopPanel
    {
        public Sprite bgArt;
        public Sprite popupArt;
        public Color titleColor;
        public Color costColor;
        public Color popupColor;
    }
}