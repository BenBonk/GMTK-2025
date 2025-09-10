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
        if (animalData.currencyMultToGive != 1f)
        {
            float mult = animalData.currencyMultToGive + (level * animalData.currencyLevelUpMult);
            description += $"{cashMult.GetLocalizedString()} x{mult}\n";
        }

        return description;
    }
    
    public string GetBoonDescription(Boon boonData)
    {
        if (boonData is SpecialtyBoon specialtyBoon)
        {
            return specialtyBoon.desc.GetLocalizedString();
        }
        if (boonData is LegendaryBoon legendaryBoon)
        {
            return legendaryBoon.desc.GetLocalizedString();
        }

        BasicBoon basicBoon = (BasicBoon)boonData;
        
        string description = "";
        if (basicBoon.pointsBonus != 0)
        {
            description += basicBoon.pointsBonus < 0
                ? $"{pointsLoss.GetLocalizedString()}{basicBoon.pointsBonus}\n"
                : $"{pointsBonus.GetLocalizedString()} +{basicBoon.pointsBonus}\n";
        }

        if (basicBoon.pointsMult != 1)
        {
            description += $"{pointsMult.GetLocalizedString()} x{basicBoon.pointsMult}\n";
        }

        if (basicBoon.currencyBonus != 0)
        {
            description += basicBoon.currencyBonus < 0
                ? $"{cashLoss.GetLocalizedString()} {basicBoon.currencyBonus}\n"
                : $"{cashBonus.GetLocalizedString()} +{basicBoon.currencyBonus}\n";
        }

        if (basicBoon.currencyMult != 1)
        {
            description += $"{cashMult.GetLocalizedString()} x{basicBoon.currencyMult}\n";
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
            description += $"{cash.GetLocalizedString()} x{initial} -> x{after}\n";
        }

        return description;
    }
}

