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
        descriptionText.text = chosenAnimal.description;
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
