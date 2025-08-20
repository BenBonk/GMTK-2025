using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public TMP_Text descriptionText2;


    //remove this after trailer
    public AnimalData cow;
    public AnimalData goat;

    public override void Initialize()
    {
        // Bias: non-predators get weight 4, predators get weight 1
        List<Animal> weightedList = new List<Animal>();
        foreach (var animal in possibleAnimals)
        {
            int weight = animal.isPredator ? 1 : 4; // change weights as needed
            for (int i = 0; i < weight; i++)
            {
                weightedList.Add(animal);
            }
        }

        chosenAnimal = weightedList[Random.Range(0, weightedList.Count)].animalData;

        //remove this after trailer
        chosenAnimal = Random.Range(0f, 1f) < 0.5f ? cow: goat;

        titleText.text = chosenAnimal.animalName.GetLocalizedString();
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
        if (buttonFX != null)
        {
            buttonFX.clickSFX = chosenAnimal.animalName.GetLocalizedString();
            buttonFX.highlightSFX = chosenAnimal.animalName.GetLocalizedString();
        }

        string desc = GameController.descriptionManager.GetAnimalDescription(chosenAnimal);
        descriptionText.text = desc;
        descriptionText2.text = chosenAnimal.description.GetLocalizedString();
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
