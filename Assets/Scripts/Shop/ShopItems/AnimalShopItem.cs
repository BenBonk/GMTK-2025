using UnityEngine;

public class AnimalShopItem : ShopItem
{
    public AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    public override void Initialize()
    {
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        titleText.text = chosenAnimal.name;
        descriptionText.text = chosenAnimal.description;
        priceText.text = chosenAnimal.price.ToString();
        price = chosenAnimal.price;
        upgradeArt.sprite = chosenAnimal.sprite;
    }
    public override void PurchaseUpgrade()
    {
        if (canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            canPurchase = false;
            GameController.player.AddAnimalToDeck(chosenAnimal.animalPrefab);
        }
    }
}
