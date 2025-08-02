using DG.Tweening;
using UnityEngine;

public class UpgradeShopItem : ShopItem
{
    private AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    public override void Initialize()
    {
        possibleAnimals = GameController.player.animalsInDeck.ToArray();
        AnimalLevelManager levelManager = GameController.animalLevelManager;
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        int animalLevel = levelManager.GetLevel(chosenAnimal.name);
        Animal animalPrefabRef = chosenAnimal.animalPrefab.GetComponent<Animal>();
        descriptionText.text = "";
        if (chosenAnimal.pointsLevelUpIncrease!=0)
        {
            float initial = (animalLevel * chosenAnimal.pointsLevelUpIncrease + animalPrefabRef.pointsToGive); 
            float after = initial + chosenAnimal.pointsLevelUpIncrease;
            descriptionText.text += ("Points: " + initial + " -> " + after +"\n");
        }
        if (chosenAnimal.pointsLevelUpMult!=0)
        {
            float initial = (animalLevel * chosenAnimal.pointsLevelUpMult + animalPrefabRef.pointsMultToGive);
            float after = initial + chosenAnimal.pointsLevelUpMult;
            descriptionText.text += ("Points: x" + initial + "-> x" + after+"\n");
        }
        if (chosenAnimal.currencyLevelUpIncrease!=0)
        {
            float initial = (animalLevel * chosenAnimal.currencyLevelUpIncrease + animalPrefabRef.currencyToGive);
            float after = initial + chosenAnimal.currencyLevelUpIncrease;
            descriptionText.text += ("Coins: " + initial + " -> " + after +"\n");
        }
        if (chosenAnimal.currencyLevelUpMult!=0)
        {
            float initial = (animalLevel * chosenAnimal.currencyLevelUpMult + animalPrefabRef.currencyMultToGive);
            float after = initial + chosenAnimal.currencyLevelUpMult;
            descriptionText.text += ("Coins: x" + initial + " -> x" + after+"\n");
        }
        
        titleText.text = chosenAnimal.name;
        price = (int)(25 * Mathf.Pow(2, animalLevel));
        priceText.text = price.ToString();
        upgradeArt.sprite = chosenAnimal.deckIcon;
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            GameController.animalLevelManager.SetLevel(chosenAnimal.name, GameController.animalLevelManager.GetLevel(chosenAnimal.name)+1);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            
        }
    }
}
