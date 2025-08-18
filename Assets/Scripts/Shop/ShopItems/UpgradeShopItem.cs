using DG.Tweening;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class UpgradeShopItem : ShopItem
{
    private AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    public override void Initialize()
    {
        possibleAnimals = GameController.player.animalsInDeck.ToArray();
        AnimalLevelManager levelManager = GameController.animalLevelManager;
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        int animalLevel = levelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString());

        descriptionText.text = GameController.descriptionManager.GetAnimalLevelDescription(chosenAnimal);

        titleText.text = chosenAnimal.animalName.GetLocalizedString();
        price = chosenAnimal.upgradeCost * Math.Pow(1.5, animalLevel);
        priceText.text = LassoController.FormatNumber(price);
        upgradeArt.sprite = chosenAnimal.deckIcon;
        upgradeArt.transform.DOScale(Vector3.one, 0);
        upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.one, .25f);
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            GameController.animalLevelManager.SetLevel(chosenAnimal.animalName.GetLocalizedString(), GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString())+1);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            
        }
    }
}
