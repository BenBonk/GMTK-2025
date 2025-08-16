using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalShopItem : ShopItem
{
    public Animal[] possibleAnimals;
    private AnimalData chosenAnimal;
    private Sprite sprite1;
    private Sprite sprite2;
    private float animSpeed;
    private Coroutine animCo;
    public override void Initialize()
    {
        // Bias: non-predators get weight 3, predators get weight 1
        List<Animal> weightedList = new List<Animal>();
        foreach (var animal in possibleAnimals)
        {
            int weight = animal.isPredator ? 1 : 3; // change weights as needed
            for (int i = 0; i < weight; i++)
            {
                weightedList.Add(animal);
            }
        }

        chosenAnimal = weightedList[Random.Range(0, weightedList.Count)].animalData;
        titleText.text = chosenAnimal.animalName;
        priceText.text = chosenAnimal.price.ToString();
        price = chosenAnimal.price;
        upgradeArt.sprite = chosenAnimal.sprite;
        upgradeArt.SetNativeSize();
        sprite1 = chosenAnimal.sprite;
        sprite2 = chosenAnimal.sprite2;
        animSpeed = chosenAnimal.animSpeed;
        if (animCo!=null)
        {
            StopCoroutine(animCo);
        }
        animCo = StartCoroutine(Animate());
        descriptionText.text = "";
        int level = GameController.animalLevelManager.GetLevel(chosenAnimal.name);
        if (buttonFX != null)
        {
            buttonFX.clickSFX = chosenAnimal.animalName;
            buttonFX.highlightSFX = chosenAnimal.animalName;
        }
        if (chosenAnimal.pointsToGive!=0)
        {
            if (chosenAnimal.pointsToGive<0)
            {
                descriptionText.text += ("Points loss: " + (chosenAnimal.pointsToGive+(level*chosenAnimal.pointsLevelUpIncrease)) + "\n");   
            }
            else
            {
                descriptionText.text += ("Points bonus: +" + (chosenAnimal.pointsToGive+(level*chosenAnimal.pointsLevelUpIncrease)) + "\n");   
            }
        }
        if (chosenAnimal.pointsMultToGive!=1f)
        {
            descriptionText.text += ("Points mult: x" + (chosenAnimal.pointsMultToGive+(level*chosenAnimal.pointsLevelUpMult)) + "\n");
        }
        if (chosenAnimal.currencyToGive!=0)
        {
            if (chosenAnimal.currencyToGive < 0)
            {
                descriptionText.text  += ("Cash loss: " + (chosenAnimal.currencyToGive+(level*chosenAnimal.currencyLevelUpIncrease)) + "\n");
            }
            else
            {
                descriptionText.text  += ("Cash bonus: +" + (chosenAnimal.currencyToGive+(level*chosenAnimal.currencyLevelUpIncrease)) + "\n");    
            }
        }
        if (chosenAnimal.currencyMultToGive!=1f)
        {
            descriptionText.text += ("Cash mult: x" + (chosenAnimal.currencyMultToGive+(level*chosenAnimal.currencyLevelUpMult)) + "\n");
        }
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            GameController.player.AddAnimalToDeck(chosenAnimal);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            shopManager.UpdateDeck();
        }
    }

    IEnumerator Animate()
    {
        while (canPurchase)
        {
            upgradeArt.sprite = sprite1;
            yield return new WaitForSeconds(animSpeed);
            upgradeArt.sprite = sprite2;
            yield return new WaitForSeconds(animSpeed);   
        }
    }
}
