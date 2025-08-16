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
        int animalLevel = levelManager.GetLevel(chosenAnimal.animalName);
        descriptionText.text = "";
        if (chosenAnimal.pointsLevelUpIncrease!=0)
        {
            float initial = (animalLevel * chosenAnimal.pointsLevelUpIncrease + chosenAnimal.pointsToGive); 
            float after = initial + chosenAnimal.pointsLevelUpIncrease;
            descriptionText.text += ("Points: " + initial + " -> " + after +"\n");
        }
        if (chosenAnimal.pointsLevelUpMult!=0)
        {
            float initial = (animalLevel * chosenAnimal.pointsLevelUpMult + chosenAnimal.pointsMultToGive);
            float after = initial + chosenAnimal.pointsLevelUpMult;
            descriptionText.text += ("Points: x" + initial + "-> x" + after+"\n");
        }
        if (chosenAnimal.currencyLevelUpIncrease!=0)
        {
            float initial = (animalLevel * chosenAnimal.currencyLevelUpIncrease + chosenAnimal.currencyToGive);
            float after = initial + chosenAnimal.currencyLevelUpIncrease;
            descriptionText.text += ("Coins: " + initial + " -> " + after +"\n");
        }
        if (chosenAnimal.currencyLevelUpMult!=0)
        {
            float initial = (animalLevel * chosenAnimal.currencyLevelUpMult + chosenAnimal.currencyMultToGive);
            float after = initial + chosenAnimal.currencyLevelUpMult;
            descriptionText.text += ("Coins: x" + initial + " -> x" + after+"\n");
        }
        
        titleText.text = chosenAnimal.animalName;
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
            GameController.animalLevelManager.SetLevel(chosenAnimal.animalName, GameController.animalLevelManager.GetLevel(chosenAnimal.animalName)+1);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            
        }
    }
}
