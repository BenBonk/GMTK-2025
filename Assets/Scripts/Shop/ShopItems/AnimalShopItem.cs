using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AnimalShopItem : ShopItem
{
    public AnimalData[] possibleAnimals;
    private AnimalData chosenAnimal;
    private Sprite sprite1;
    private Sprite sprite2;
    private float animSpeed;
    private Coroutine animCo;
    public override void Initialize()
    {
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Length)];
        titleText.text = chosenAnimal.name;
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
        Animal animalRef = chosenAnimal.animalPrefab.GetComponent<Animal>();
        if (buttonFX != null)
        {
            buttonFX.clickSFX = animalRef.name;
            buttonFX.highlightSFX = animalRef.name;
        }
        if (animalRef.pointsToGive!=0)
        {
            if (animalRef.pointsToGive<0)
            {
                descriptionText.text += ("Points loss: " + animalRef.pointsToGive + "\n");   
            }
            else
            {
                descriptionText.text += ("Points bonus: +" + animalRef.pointsToGive + "\n");   
            }
        }
        if (animalRef.pointsMultToGive!=1f)
        {
            descriptionText.text += ("Points mult: x" + animalRef.pointsMultToGive + "\n");
        }
        if (animalRef.currencyToGive!=0)
        {
            if (animalRef.currencyToGive < 0)
            {
                descriptionText.text  += ("Cash loss: " + animalRef.currencyToGive + "\n");
            }
            else
            {
                descriptionText.text  += ("Cash bonus: +" + animalRef.currencyToGive + "\n");    
            }
        }
        if (animalRef.currencyMultToGive!=1f)
        {
            descriptionText.text += ("Cash mult: x" + animalRef.currencyMultToGive + "\n");
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
