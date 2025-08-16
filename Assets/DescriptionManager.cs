using System;
using UnityEngine;

public class DescriptionManager : MonoBehaviour
{
    private AnimalLevelManager levelManager;

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
                ? $"Points loss: {points}\n"
                : $"Points bonus: +{points}\n";
        }
        if (animalData.pointsMultToGive != 1f)
        {
            float mult = animalData.pointsMultToGive + (level * animalData.pointsLevelUpMult);
            description += $"Points mult: x{mult}\n";
        }
        if (animalData.currencyToGive != 0)
        {
            int cash = animalData.currencyToGive + (level * animalData.currencyLevelUpIncrease);
            description += cash < 0
                ? $"Cash loss: {cash}\n"
                : $"Cash bonus: +{cash}\n";
        }
        if (animalData.currencyMultToGive != 1f)
        {
            float mult = animalData.currencyMultToGive + (level * animalData.currencyLevelUpMult);
            description += $"Cash mult: x{mult}\n";
        }

        return description;
    }
    
    public string GetSynergyDescription(Synergy synergyData)
    {
        string description = "";
        if (synergyData.pointsBonus != 0)
        {
            description += synergyData.pointsBonus < 0
                ? $"Points loss: {synergyData.pointsBonus}\n"
                : $"Points bonus: +{synergyData.pointsBonus}\n";
        }

        if (synergyData.pointsMult != 1)
        {
            description += $"Points mult: x{synergyData.pointsMult}\n";
        }

        if (synergyData.currencyBonus != 0)
        {
            description += synergyData.currencyBonus < 0
                ? $"Cash loss: {synergyData.currencyBonus}\n"
                : $"Cash bonus: +{synergyData.currencyBonus}\n";
        }

        if (synergyData.currencyMult != 1)
        {
            description += $"Cash mult: x{synergyData.currencyMult}\n";
        }

        return description;
    }

    public string GetAnimalLevelDescription(AnimalData animalData)
    {
        int animalLevel = levelManager.GetLevel(animalData.animalName);
        string description = "";
        if (animalData.pointsLevelUpIncrease != 0)
        {
            float initial = animalLevel * animalData.pointsLevelUpIncrease + animalData.pointsToGive; 
            float after = initial + animalData.pointsLevelUpIncrease;
            description += $"Points: {initial} -> {after}\n";
        }

        if (animalData.pointsLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.pointsLevelUpMult + animalData.pointsMultToGive;
            float after = initial + animalData.pointsLevelUpMult;
            description += $"Points: x{initial} -> x{after}\n";
        }

        if (animalData.currencyLevelUpIncrease != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpIncrease + animalData.currencyToGive;
            float after = initial + animalData.currencyLevelUpIncrease;
            description += $"Coins: {initial} -> {after}\n";
        }

        if (animalData.currencyLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpMult + animalData.currencyMultToGive;
            float after = initial + animalData.currencyLevelUpMult;
            description += $"Coins: x{initial} -> x{after}\n";
        }

        return description;
    }
}

