using System;
using System.Globalization;
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
        if (animalData.pointsToGive + (level * animalData.pointsLevelUpIncrease) != 0)
        {
            int points = animalData.pointsToGive + (level * animalData.pointsLevelUpIncrease);
            description += points < 0
                ? $"{pointsLoss.GetLocalizedString()} <color=#FC0043>{points}</color>\n"
                : $"{pointsBonus.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{points}</color>\n";
        }
        if (animalData.pointsMultToGive + (level * animalData.pointsLevelUpMult) != 1f)
        {
            float mult = animalData.pointsMultToGive + (level * animalData.pointsLevelUpMult);
            description += mult < 1
                ? $"{pointsMult.GetLocalizedString()} <color=#FC0043>x{Format1or2(mult)}\n</color>"
                : $"{pointsMult.GetLocalizedString()} <color=#FE7B81>x</color><color=#FEE761>{Format1or2(mult)}</color>\n";
        }
        if (animalData.currencyToGive + (level * animalData.currencyLevelUpIncrease) != 0)
        {
            int cash = animalData.currencyToGive + (level * animalData.currencyLevelUpIncrease);
            description += cash < 0
                ? $"{cashLoss.GetLocalizedString()} <color=#FC0043>{cash}</color>\n"
                : $"{cashBonus.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{cash}</color>\n";
        }
        if (animalData.currencyMultToGive + (level * animalData.currencyLevelUpMult) != 0f)
        {
            float mult = animalData.currencyMultToGive + (level * animalData.currencyLevelUpMult);
            description += mult < 0
                ? $"{cashMult.GetLocalizedString()} <color=#FC0043>{Format1or2(mult)}x</color>\n"
                : $"{cashMult.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{Format1or2(mult)}x</color>\n";
        }

        return description;
    }


    //63C74D = green
    //3B8143 = green2
    //F6757A = red
    //9C2531 = red2
    //FEE761 = yellow
    //124A84 = blue
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
                ? $"{pointsLoss.GetLocalizedString()} <color=#FC0043>{basicBoon.pointsBonus}</color>\n"
                : $"{pointsBonus.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{basicBoon.pointsBonus}</color>\n";
        }
        //tes
        if (basicBoon.pointsMult != 1)
        {
            description += basicBoon.pointsMult < 1
                ? $"{pointsMult.GetLocalizedString()} <color=#FC0043>x{Format1or2(basicBoon.pointsMult)}</color>\n"
                : $"{pointsMult.GetLocalizedString()} <color=#FE7B81>x</color><color=#FEE761>{Format1or2(basicBoon.pointsMult)}</color>\n";
        }

        if (basicBoon.currencyBonus != 0)
        {
            description += basicBoon.currencyBonus < 0
                ? $"{cashLoss.GetLocalizedString()} <color=#FC0043>{basicBoon.currencyBonus}</color>\n"
                : $"{cashBonus.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{basicBoon.currencyBonus}</color>\n";
        }

        if (basicBoon.currencyMult != 0)
        {
            description += basicBoon.currencyMult < 0
                ? $"{cashMult.GetLocalizedString()} <color=#FC0043>{Format1or2(basicBoon.currencyMult)}x</color>\n"
                : $"{cashMult.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{Format1or2(basicBoon.currencyMult)}x</color>\n";
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
            description += initial < 0
                ? $"{points.GetLocalizedString()} <color=#FC0043>{initial}</color> -> "
                : $"{points.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{initial}</color> -> ";
            description += after < 0
                ? $"<color=#FC0043>{after}</color>\n"
                : $"<color=#68C84D>+</color><color=#FEE761>{after}</color>\n";
        }

        if (animalData.pointsLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.pointsLevelUpMult + animalData.pointsMultToGive;
            float after = initial + animalData.pointsLevelUpMult;
            description += initial < 1
                ? $"{pointsMult.GetLocalizedString()} <color=#FC0043>x{Format1or2(initial)}</color> -> "
                : $"{pointsMult.GetLocalizedString()} <color=#FE7B81>x</color><color=#FEE761>{Format1or2(initial)}</color> -> ";
            description += after < 1
                ? $"<color=#FC0043>x{Format1or2(after)}</color>\n"
                : $"<color=#FE7B81>x</color><color=#FEE761>{Format1or2(after)}</color>\n";
        }

        if (animalData.currencyLevelUpIncrease != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpIncrease + animalData.currencyToGive;
            float after = initial + animalData.currencyLevelUpIncrease;
            description += initial < 0
                ? $"{cash.GetLocalizedString()} <color=#FC0043>{initial}</color> -> "
                : $"{cash.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{initial}</color> -> ";
            description += after < 0
                ? $"<color=#FC0043>{after}</color>\n"
                : $"<color=#68C84D>+</color><color=#FEE761>{after}</color>\n";
        }

        if (animalData.currencyLevelUpMult != 0)
        {
            float initial = animalLevel * animalData.currencyLevelUpMult + animalData.currencyMultToGive;
            float after = initial + animalData.currencyLevelUpMult;
            description += initial < 0
                ? $"{cashMult.GetLocalizedString()} <color=#FC0043>{Format1or2(initial)}x</color> -> "
                : $"{cashMult.GetLocalizedString()} <color=#68C84D>+</color><color=#FEE761>{Format1or2(initial)}x</color> -> ";
            description += after < 0
                ? $"<color=#FC0043>{Format1or2(after)}x</color>\n"
                : $"<color=#68C84D>+</color><color=#FEE761>{Format1or2(after)}x</color>\n";
        }

        return description;
    }
    
    //what the heck is this lol
    static string Format1or2(double x)
    {
        x = Math.Round(x, 2, MidpointRounding.AwayFromZero); // kill binary noise
        return x.ToString("0.0#", CultureInfo.InvariantCulture); // 1 dec min, up to 2
    }
}

