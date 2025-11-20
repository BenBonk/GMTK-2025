using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public bool leftAnimal;
    public override void Initialize()
    {
        // Bias: non-predators get weight 5, predators get weight 1
        List<Animal> weightedList = new List<Animal>();
        foreach (var animal in possibleAnimals)
        {
            if (GameController.gameManager.roundNumber == 1 && (animal.isPredator || animal.animalData.name == "Horse"))
            {
                continue; // skip predators/horses in round 1
            }
            int weight = animal.isPredator ? 1 : 5; // change weights as needed
            for (int i = 0; i < weight; i++)
            {
                weightedList.Add(animal);
            }
        }

        chosenAnimal = weightedList[Random.Range(0, weightedList.Count)].animalData;
        if (leftAnimal && GameController.boonManager.ContainsBoon("MaskOfMany"))
        {
            Dictionary<AnimalData, int> animalsCount = new Dictionary<AnimalData, int>();
            foreach (var a in GameController.player.animalsInDeck)
            {
                if (!animalsCount.ContainsKey(a))
                    animalsCount[a] = 0;
                animalsCount[a]++;
            }
            chosenAnimal = animalsCount.OrderByDescending(kv => kv.Value).First().Key;
        }
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
        if (!shopManager.cantPurchaseItem && canPurchase && (GameController.player.playerCurrency >= price || (GameController.boonManager.ContainsBoon("BNPL") && GameController.player.playerCurrency - price >=-150)))
        {
            AudioManager.Instance.PlaySFX("coins");
            AudioManager.Instance.PlaySFX(chosenAnimal.name);
            StartCoroutine(DeckPulse());
            canPurchase = false;
            FBPP.SetInt(chosenAnimal.name+"_count", FBPP.GetInt(chosenAnimal.name+"_count")+1);
            FBPP.SetInt("totalAnimalsPurchased", FBPP.GetInt("totalAnimalsPurchased")+1);
            GameController.player.AddAnimalToDeck(chosenAnimal);
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
            shopManager.UpdateDeck(shopManager.deckParent);
            FBPP.SetInt("AnimalPurchasedThisGame", 1);
            //triggers save, goes last
            GameController.player.playerCurrency -= price;
            shopManager.UpdateCashText();
        }
        else
        {
            AudioManager.Instance.PlaySFX("no_point_mult");
        }
    }

    IEnumerator DeckPulse()
    {
        yield return new WaitForSeconds(.25f);
        Sequence pulse = DOTween.Sequence();
        pulse.Append(shopManager.animalDeckButton.transform.DOScale(1.10f, 0.1f).SetEase(Ease.OutBack));
        pulse.Append(shopManager.animalDeckButton.transform.DOShakeRotation(
            duration: 0.15f,
            strength: new Vector3(0f, 0f, 6f), 
            vibrato: 5,
            randomness: 90,
            fadeOut: true
        ));
        pulse.Append(shopManager.animalDeckButton.transform.DOScale(1f, 0.15f).SetEase(Ease.OutExpo));
        pulse.Join(shopManager.animalDeckButton.transform.DOLocalRotate(Vector3.zero, 0.15f, RotateMode.Fast));
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
