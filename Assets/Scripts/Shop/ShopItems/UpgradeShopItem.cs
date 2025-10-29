using DG.Tweening;
using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using Random = UnityEngine.Random;

public class UpgradeShopItem : ShopItem
{
    private List<AnimalData> possibleAnimals;
    private AnimalData chosenAnimal;
    public UpgradeShopItem partnerUpgrade;
    public AnimalShopItem[] animalShopItems;
    public TMP_Text levelText;
    public LocalizedString levelString;
    public override void Initialize()
    {
        possibleAnimals = new List<AnimalData>(GameController.player.animalsInDeck);
        foreach (var item in animalShopItems)
        {
            possibleAnimals.Add(item.chosenAnimal);
        }

        if (GameController.boonManager.ContainsBoon("Thief"))
        {
            possibleAnimals.RemoveAll(a => a.name == "Fox");
        }
        AnimalLevelManager levelManager = GameController.animalLevelManager;
        chosenAnimal = possibleAnimals[Random.Range(0, possibleAnimals.Count)];
        int animalLevel = levelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString());
        levelText.text = levelString.GetLocalizedString() + " " + (2+animalLevel);

        UpdateDescription();

        titleText.text = chosenAnimal.animalName.GetLocalizedString();
        double priceIncrease = 1.5;
        if (GameController.gameManager.farmerID == 1)
        {
            priceIncrease = 1.3;
        }
        price = Math.Round(chosenAnimal.upgradeCost * Math.Pow(priceIncrease, animalLevel));
        priceText.text = LassoController.FormatNumber(price);
        upgradeArt.sprite = chosenAnimal.deckIcon;
        upgradeArt.transform.DOScale(Vector3.one, 0);
        upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.one, .25f);
    }

    void UpdateDescription()
    {
        descriptionText.text = GameController.descriptionManager.GetAnimalLevelDescription(chosenAnimal);
        int animalLevel = GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString());
        levelText.text = levelString.GetLocalizedString() + " " + (2+animalLevel);
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && (GameController.player.playerCurrency >= price || (GameController.boonManager.ContainsBoon("BNPL") && GameController.player.playerCurrency - price >=-200)))
        {
            AudioManager.Instance.PlaySFX("ui_click");
            AudioManager.Instance.PlaySFX("coins");
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            FBPP.SetInt("totalUpgradesPurchased", FBPP.GetInt("totalUpgradesPurchased")+1);
            int newLevel = GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString()) + 1;
            GameController.animalLevelManager.SetLevel(chosenAnimal.animalName.GetLocalizedString(), newLevel);
            Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
            if (GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString()) > FBPP.GetInt("highestAnimalLevel"))
            {
                FBPP.SetInt("highestAnimalLevel", GameController.animalLevelManager.GetLevel(chosenAnimal.animalName.GetLocalizedString()));
            }

            if (newLevel == 10 && GameController.steamIntegration.IsThisAchievementUnlocked("Beefy"))
            {
                GameController.steamIntegration.UnlockAchievement("Beefy");
            }

            if (!shopManager.cantPurchaseItem)
            {
                partnerUpgrade.UpdateDescription();
            }

            foreach (var a in animalShopItems)
            {
                if (a.chosenAnimal.animalName==chosenAnimal.animalName)
                {
                    a.descriptionText.text = GameController.descriptionManager.GetAnimalDescription(a.chosenAnimal);
                }
            }

            foreach (var card in FindObjectsOfType<DeckCard>(true))
            {
                if (!card.bounch && card.icon.sprite == chosenAnimal.deckIcon)
                {
                    card.desc.text = GameController.descriptionManager.GetAnimalDescription(chosenAnimal);
                }
            }
            upgradeArt.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            upgradeArt.transform.parent.GetChild(1).DOScale(Vector3.zero, .25f).SetEase(Ease.OutBack);
            
        }
        else
        {
            AudioManager.Instance.PlaySFX("no_point_mult");
        }
    }
}
