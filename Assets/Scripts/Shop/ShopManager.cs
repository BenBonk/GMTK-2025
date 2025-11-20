using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject deckCardPrefab;
    public RectTransform deckParent;

    public RectTransform deckPanel;
    public RectTransform synergiesPanel;
    public GameObject synergiesVisual;
    public TMP_Text instructionsText;
    private bool deckOpen;
    [HideInInspector]public bool synergiesOpen;
    [HideInInspector]public SynergyShopItem overridingBoonItem;
    public Image cancelOverride;
    public GameObject leaveShopButton;
    public Image darkCover;
    public bool cantPurchaseItem;
    public bool isTut;
    public TMP_Text cashText;
    public LocalizedString cashLocalString;
    public TMP_Text roundText;
    public LocalizedString roundLocalString;
    public GameObject purchaseParticles;
    public BoonShopPanel[] boonShopPanels;
    public BoonGroup[] boonGroups;
    private Tween animalDeckTween;
    private Tween boonDeckTween;
    public UpgradeShopItem upgradeShopItem;
    public GameObject boonDeckCard;
    public Transform boonDeckParent;
    [HideInInspector] public bool canOverrideBoon;
    public RectTransform boonDeckButton;
    public RectTransform animalDeckButton;
    [HideInInspector] public bool cantToggleSynergiesDeck;

    private Queue<Boon> recentBoons;
    private SteamIntegration steamIntegration;
    int recentBoonCapacity = 3;

    private IEnumerator Start()
    {
        steamIntegration = GameController.steamIntegration;
        descriptionManager = GameController.descriptionManager;
        player = GameController.player;
        recentBoons = new Queue<Boon>(recentBoonCapacity);
        yield return new WaitForEndOfFrame();
        InitializeAllUpgrades();
    }

    public void InitializeAllUpgrades()
    {
        foreach (var shopItem in shopItems)
        {
            if (shopItem is SynergyShopItem || !shopItem.gameObject.activeInHierarchy)
            {
                continue;
            }
            Instantiate(purchaseParticles, shopItem.rt.position, Quaternion.identity);
            shopItem.canPurchase = true;
            shopItem.Initialize();
            shopItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }//a
        UpdateCashText();
        List<Boon> chosenBoons = new List<Boon>();
        while (chosenBoons.Count < 3)
        {
            Boon boon = GetRandomSynergy();
            if (!isTut)
            {
                if (chosenBoons.Contains(boon) || player.boonsInDeck.Contains(boon) || recentBoons.Contains(boon))
                    continue;
            }
            chosenBoons.Add(boon);
        }
        for (int i = 0; i < 3; i++)
        {
            shopItems[i].GetComponent<SynergyShopItem>().SetBoon(chosenBoons[i]);
            recentBoons.Enqueue(chosenBoons[i]); 
            if (recentBoons.Count > recentBoonCapacity) recentBoons.Dequeue();
        }
        foreach (var shopItem in shopItems)
        {
            if (shopItem is not SynergyShopItem)
            {
                continue;
            }
            Instantiate(purchaseParticles, shopItem.rt.position, Quaternion.identity);
            shopItem.canPurchase = true;
            shopItem.Initialize();
            shopItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        UpdateDeck(deckParent);
        UpdateSynergies(boonDeckParent);
    }
    private Boon GetRandomSynergy()
    {
        if (isTut)
        {
            return boonGroups[0].boons[Random.Range(0,10)];
        }
        int weightIndex = 0;
        if (GameController.gameManager.roundNumber>7)
        {
            weightIndex = 1;
        }
        float totalWeight = 0f;
        foreach (var group in boonGroups)
            totalWeight += group.weights[weightIndex];
        
        float roll = Random.Range(0f, totalWeight);
        foreach (var group in boonGroups)
        {
            if (roll < group.weights[weightIndex])
            {
                List<Boon> validBoons = group.boons;
                if (GameController.gameManager.roundNumber <= 2)
                {
                    validBoons = group.boons
                        .Where(b => b.price < 350)
                        .ToList();
                    if (validBoons.Count == 0)
                        validBoons = group.boons;
                } ;
                if (group.groupName == "Legendary")
                {
                    List<string> allowedLegendaryBoons = new List<string>();
                    foreach (var a in GameController.player.animalsInDeck)
                    {
                        allowedLegendaryBoons.Add(a.legendaryBoon.name);
                    }
                    foreach (var u in upgradeShopItem.animalShopItems)
                    {
                        allowedLegendaryBoons.Add(u.chosenAnimal.legendaryBoon.name);
                    }

                    validBoons = group.boons
                        .Where(b => allowedLegendaryBoons.Contains(b.name))
                        .ToList();

                    // fallback if all were filtered out
                    if (validBoons.Count == 0)
                        validBoons = group.boons;
                }
                if (group.groupName == "Basic")
                {
                    List<string> animalsInDeckShop = new List<string>();
                    foreach (var a in GameController.player.animalsInDeck)
                    {
                        animalsInDeckShop.Add(a.name);
                    }
                    foreach (var u in upgradeShopItem.animalShopItems)
                    {
                        animalsInDeckShop.Add(u.chosenAnimal.name);
                    }
                    
                    List<BasicBoon> validBasicBoons = group.boons
                        .OfType<BasicBoon>()
                        .Where(b => b.animalsNeeded.Any(animal => animalsInDeckShop.Contains(animal)))
                        .ToList();
                    validBoons = validBasicBoons.OfType<Boon>().ToList();
                    
                    // fallback if all were filtered out
                    if (validBoons.Count == 0)
                        validBoons = group.boons;
                }
                
                int idx = Random.Range(0, validBoons.Count);
                return validBoons[idx];
            }
            roll -= group.weights[weightIndex];
        }
        return boonGroups[0].boons[0];
    }

    public void UpdateCashText()
    {
        Debug.Log("update cash text");
        cashText.text = cashLocalString.GetLocalizedString() +" "+ LassoController.FormatNumber(player.playerCurrency);
        if (!GameController.gameManager)
        {
            roundText.text = "";
        }
        else
        {
            roundText.text = roundLocalString.GetLocalizedString() +" "+ (GameController.gameManager.roundNumber);
        }
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

    public void UpdateSynergies(Transform parent)
    {
        foreach (Transform child in parent.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < player.boonsInDeck.Count; i++)
        {
            GameObject boonCard = Instantiate(boonDeckCard, Vector3.zero, Quaternion.identity, parent);
            string desc = player.boonsInDeck[i].desc.GetLocalizedString();
            if (player.boonsInDeck[i].name == "Thief")
            {
                desc = player.boonsInDeck[i].desc.GetLocalizedString() + " " + GameController.gameManager.foxThiefStolenStats.animalName.GetLocalizedString() + ".";
            }
            boonCard.GetComponent<DeckCard>().Initialize( player.boonsInDeck[i].synergyName.GetLocalizedString(), desc, player.boonsInDeck[i], descriptionManager.GetBoonDescription(player.boonsInDeck[i]));

        }
    }

    public void UpdateDeck(RectTransform deckParentt)
    {
        foreach (Transform child in deckParentt.transform)
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
        
        if (uniqueObjects.Any(animal => animal.Value.count >= 10) && !steamIntegration.IsThisAchievementUnlocked("Monoculture"))
        {
            steamIntegration.UnlockAchievement("Monoculture");
        }

        if (player.animalsInDeck.Count==50 && !GameController.steamIntegration.IsThisAchievementUnlocked("The Whole Farm"))
        { 
            steamIntegration.UnlockAchievement("The Whole Farm");
        }
        

        // Step 2: Create a card for each unique GameObject
        foreach (var entry in uniqueObjects)
        {
            int count = entry.Value.count;
            AnimalData reference = entry.Value.reference;
            string desc = descriptionManager.GetAnimalDescription(reference);
            
            GameObject card = Instantiate(deckCardPrefab, deckParentt);
            card.GetComponent<DeckCard>().Initialize("x" + count, desc, reference);
        }
    }


    public void ToggleDeck()
    {
        if (cantPurchaseItem)
        {
            return;
        }
        if (animalDeckTween!=null)
        {
            animalDeckTween.Kill();
        }
        deckOpen = !deckOpen;
        if (deckOpen)
        {
            deckPanel.gameObject.SetActive(true);
            animalDeckTween = deckPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            animalDeckTween = deckPanel.DOAnchorPosX(-415, .25f).SetEase(Ease.InOutQuad).OnComplete(()=>deckPanel.gameObject.SetActive(false));
        }
    }

    public void CloseDeck()
    {
        if (deckOpen)
        {
            animalDeckTween = deckPanel.DOAnchorPosX(-415, .25f).SetEase(Ease.InOutQuad).OnComplete(() => deckPanel.gameObject.SetActive(false));
            deckOpen = false;
        }
    }

    public void ToggleSynergies()
    {
        if (cantToggleSynergiesDeck)
        {
            return;
        }
        if (boonDeckTween!=null)
        {
            boonDeckTween.Kill();
        }
        synergiesOpen = !synergiesOpen;
        if (synergiesOpen)
        {
            synergiesVisual.SetActive(true);
            boonDeckTween = synergiesPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack);
        }
        else
        {
            boonDeckTween = synergiesPanel.DOAnchorPosX(415, .25f).SetEase(Ease.InOutQuad).OnComplete(() => synergiesVisual.SetActive(false));
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
    [System.Serializable]
    public class BoonGroup
    {
        public string groupName;
        public float[] weights; // higher = more likely to be chosen
        public List<Boon> boons;
    }
}