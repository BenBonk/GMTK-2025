using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalShopItem : ShopItem
{
    public Animal[] possibleAnimals;
    [HideInInspector] public AnimalData chosenAnimal;
    private Sprite sprite1;
    private Sprite sprite2;
    private float animSpeed;
    private Coroutine animCo;
    public TMP_Text descriptionText2;
    public override void Initialize()
    {
        // Bias: non-predators get weight 5, predators get weight 1
        List<Animal> weightedList = new List<Animal>();
        foreach (var animal in possibleAnimals)
        {
            if (GameController.gameManager.roundNumber == 1 && (animal.isPredator || animal.animalData.name == "Horse"))
            {
                Debug.Log("Skipping " + animal.animalData.name + " in round 1");
                continue; // skip predators/horses in round 1
            }
            int weight = animal.isPredator ? 1 : 5; // change weights as needed
            for (int i = 0; i < weight; i++)
            {
                weightedList.Add(animal);
            }
        }

        chosenAnimal = weightedList[Random.Range(0, weightedList.Count)].animalData;
        titleText.text = chosenAnimal.animalName.GetLocalizedString();
        price = chosenAnimal.price;
        if (GameController.boonManager.ContainsBoon("Auctioneer"))
        {
            price = Random.Range(15, 201);
        }
        priceText.text = price.ToString();
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
            AudioManager.Instance.PlaySFX("coins");
            AudioManager.Instance.PlaySFX(chosenAnimal.name);
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            FBPP.SetInt(chosenAnimal.name, FBPP.GetInt(chosenAnimal.name)+1);
            FBPP.SetInt("totalAnimalsPurchased", FBPP.GetInt("totalAnimalsPurchased")+1);
            GameController.player.AddAnimalToDeck(chosenAnimal);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
            shopManager.UpdateDeck();
        }
        else
        {
            AudioManager.Instance.PlaySFX("no_point_mult");
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
