using System;
using UnityEngine;
using UnityEngine.Localization;

public class DescriptionManager : MonoBehaviour
{
    private AnimalLevelManager levelManager;
    public LocalizedString pointsLoss;
    public LocalizedString pointsBonus;
    public LocalizedString pointsMult;
    public LocalizedString cashLoss;
    public LocalizedString cashBonus;
    public LocalizedString cashMult;
    public LocalizedString points;
    public LocalizedString cash;

    private void Start()
    {
        levelManager = GameController.animalLevelManager;
    }

    public string GetAnimalDescription(AnimalData animalData)
    {
        int level = levelManager.GetLevel(animalData.name);
        string description = "";
        if (animalData.pointsToGive != 0)
        {
            int points = animalData.pointsToGive + (level * animalData.pointsLevelUpIncrease);
            description += points < 0
                ? $"{pointsLoss.GetLocalizedString()} {points}\n"
                : $"{pointsBonus.GetLocalizedString()} +{points}\n";
        }
        if (animalData.pointsMultToGive != 1f)
        {
            float mult = animalData.pointsMultToGive + (level * animalData.pointsLevelUpMult);
            description += $"{pointsMult.GetLocalizedString()} x{mult}\n";
        }
        if (animalData.currencyToGive != 0)
        {
            int cash = animalData.currencyToGive + (level * animalData.currencyLevelUpIncrease);
            description += cash < 0
                ? $"{cashLoss.GetLocalizedString()} {cash}\n"
                : $"{cashBonus.GetLocalizedString()} +{cash}\n";
        }
        if (animalData.currencyMultToGive != 0f)
        {
            float mult = animalData.currencyMultToGive + (level * animalData.currencyLevelUpMult);
            description += mult < 0
                ? $"{pointsMult.GetLocalizedString()} {mult}\n"
                : $"{pointsMult.GetLocalizedString()} +{mult}\n";
        }

        return description;
    }
    
    public string GetBoonDescription(Boon boonData)
    {
        if (boonData is SpecialtyBoon specialtyBoon)
        {
            return "hidepopup";
        }
        if (boonData is LegendaryBoon legendaryBoon)
        {
            return "hidepopup";
        }

        BasicBoon basicBoon = (BasicBoon)boonData;
        
        string description = "";
        if (basicBoon.pointsBonus != 0)
        {
            description += basicBoon.pointsBonus < 0
                ? $"{pointsLoss.GetLocalizedString()} <color=#FEE761>{basicBoon.pointsBonus}</color>\n"
                : $"{pointsBonus.GetLocalizedString()} <color=#FEE761>+{basicBoon.pointsBonus}</color>\n";
        }

        if (basicBoon.pointsMult != 1)
        {
            description += $"{pointsMult.GetLocalizedString()} <color=#F6757A>x{basicBoon.pointsMult}</color>\n";
        }

        if (basicBoon.currencyBonus != 0)
        {
            description += basicBoon.currencyBonus < 0
                ? $"{cashLoss.GetLocalizedString()} <color=#FEE761>{basicBoon.currencyBonus}</color>\n"
                : $"{cashBonus.GetLocalizedString()} <color=#FEE761>+{basicBoon.currencyBonus}</color>\n";
        }

        if (basicBoon.currencyMult != 0)
        {
            description += basicBoon.currencyMult < 0 
                ? $"{cashMult.GetLocalizedString()} <color=#F6757A>{basicBoon.currencyMult}</color>\n"
                : $"{cashMult.GetLocalizedString()} <color=#F6757A>+{basicBoon.currencyMult}</color>\n";
        }

        return description.TrimEnd('\n');
    }

    public string GetAnimalLevelDescription(AnimalData animalData)
    {
        levelManager = GameController.animalLevelManager;
        int animalLevel = levelManager.GetLevel(animalData.animalName.GetLocalizedString());
        string description = "";
        if (animalData.pointsLevelUpIncrease != 0)
        {
            float initial = animalLevel * animalData.pointsLevelUpIncrease + animalData.pointsToGive; 
            float after = initial + animalData.pointsLevelUpIncrease;
            description += $"{points.GetLocalizedString()} {initial} -> {after}\n";
        }

        if (animalData.pointsLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.pointsLevelUpMult + animalData.pointsMultToGive;
            float after = initial + animalData.pointsLevelUpMult;
            description += $"{points.GetLocalizedString()} x{initial} -> x{after}\n";
        }

        if (animalData.currencyLevelUpIncrease != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpIncrease + animalData.currencyToGive;
            float after = initial + animalData.currencyLevelUpIncrease;
            description += $"{cash.GetLocalizedString()} {initial} -> {after}\n";
        }

        if (animalData.currencyLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpMult + animalData.currencyMultToGive;
            float after = initial + animalData.currencyLevelUpMult;
            description += $"{cash.GetLocalizedString()} +{initial} -> +{after}\n";
        }

        return description;
    }
}

