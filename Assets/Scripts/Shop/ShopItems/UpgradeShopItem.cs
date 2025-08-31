using DG.Tweening;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class UpgradeShopItem : ShopItem
{
    private AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    public UpgradeShopItem partnerUpgrade;
    public AnimalShopItem[] animalShopItems;
    
    public override void Initialize()
    {
        possibleAnimals = GameController.player.animalsInDeck.ToArray();
        AnimalLevelManager levelManager = GameController.animalLevelManager;
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        int animalLevel = levelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString());

        UpdateDescription();

        titleText.text = chosenAnimal.animalName.GetLocalizedString();
        price = Math.Round(chosenAnimal.upgradeCost * Math.Pow(1.5, animalLevel));
        priceText.text = LassoController.FormatNumber(price);
        upgradeArt.sprite = chosenAnimal.deckIcon;
        upgradeArt.transform.DOScale(Vector3.one, 0);
        upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.one, .25f);
    }

    void UpdateDescription()
    {
        descriptionText.text = GameController.descriptionManager.GetAnimalLevelDescription(chosenAnimal);
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            FBPP.SetInt("totalUpgradesPurchased", FBPP.GetInt("totalUpgradesPurchased")+1);
            GameController.animalLevelManager.SetLevel(chosenAnimal.animalName.GetLocalizedString(), GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString())+1);
            Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
            if (GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString()) > FBPP.GetInt("highestAnimalLevel"))
            {
                FBPP.SetInt("highestAnimalLevel", GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString()));
            }

            if (!shopManager.cantPurchaseItem)
            {
                partnerUpgrade.UpdateDescription();
            }

            foreach (var a in animalShopItems)
            {
                if (a.chosenAnimal.animalName==chosenAnimal.animalName)
                {
                    a.descriptionText.text = GameController.descriptionManager.GetAnimalDescription(a.chosenAnimal);
                }
            }

            foreach (var card in FindObjectsOfType<DeckCard>(true))
            {
                if (!card.bounch && card.icon.sprite == chosenAnimal.deckIcon)
                {
                    card.desc.text = GameController.descriptionManager.GetAnimalDescription(chosenAnimal);
                }
            }
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            
        }
    }
}
