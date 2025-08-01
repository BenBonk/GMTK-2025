using UnityEngine;

public class UpgradeShopItem : ShopItem
{
    public AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    public override void Initialize()
    {
        AnimalLevelManager levelManager = GameController.animalLevelManager;
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        int animalLevel = levelManager.GetLevel(chosenAnimal.name);
        
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
            canPurchase = false;
            GameController.animalLevelManager.SetLevel(chosenAnimal.name, GameController.animalLevelManager.GetLevel(chosenAnimal.name)+1);
        }
    }
}
