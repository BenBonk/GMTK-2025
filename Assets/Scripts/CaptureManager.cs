using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class CaptureManager : MonoBehaviour
{
    private Player player;
    private GameManager gameManager;
    private BoonManager boonManager;

    private double pointBonus = 0;
    private double pointMult = 1;
    private double currencyBonus = 0;
    private double currencyMult = 1;
    [HideInInspector] public bool firstCapture;
    [HideInInspector] public float mootiplierMult=0;

    private void Start()
    {
        boonManager = GameController.boonManager;
        gameManager = GameController.gameManager;
        player = GameController.player;
    }

    private int triggers = 1;
    private int currentTrigger = 0;
    private int totalPredatorCount = 0;
    HashSet<Sprite> boonSprites = new HashSet<Sprite>();
    public (double, double, double, double, HashSet<Sprite>) MakeCapture(GameObject[] objectsCaptured)
    {
        pointBonus = 0;
        pointMult = 1;
        currencyBonus = 0;
        currencyMult = 1;
        triggers = 1;
        currentTrigger = 0;
        totalPredatorCount = 0;
        boonSprites = new HashSet<Sprite>();
        if (objectsCaptured.Length == 0)
        {
            return (pointBonus, pointMult, currencyBonus, currencyMult, boonSprites);
        }

        for (currentTrigger = 0; currentTrigger < triggers; currentTrigger++)
        {
            List<Animal> animalsCaptured = new List<Animal>();
            List<Lassoable> lassoablesCaptured = new List<Lassoable>();
            foreach (var item in objectsCaptured)
            {
                if (item.CompareTag("NonAnimalLassoable"))
                {
                    Lassoable lassoable = item.GetComponent<Lassoable>();
                    if (lassoable != null)
                    {
                        lassoablesCaptured.Add(lassoable);
                    }
                }
                else
                {
                    Animal animal = item.GetComponent<Animal>();
                    if (animal != null)
                    {
                        animalsCaptured.Add(animal);
                    }
                }
            }

            var capturedCounts = GetNameCounts(animalsCaptured);

            for (int i = 0; i < player.boonsInDeck.Count; i++)
            {
                if (player.boonsInDeck[i] is not BasicBoon boon)
                {
                    continue;
                }
                var neededCounts = GetNameCounts(boon.animalsNeeded);
                Debug.Log($"Checking boon {boon.name}: Needed = [{string.Join(",", neededCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}], Captured = [{string.Join(",", capturedCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}]");

                if (!boon.isExactMatch)
                {
                    bool subset = IsSubset(neededCounts, capturedCounts);
                    Debug.Log($"Non-exact match check for {boon.name}: Result = {subset}");
                    if (subset) ActivateBoon(boon);
                }

                else
                {
                    if (AreCountsEqual(neededCounts, capturedCounts))
                        ActivateBoon(boon);
                }
            }

            int totalNonPredatorCount = 0;

            int biodiversityBonus = 0;
            foreach (var animal in animalsCaptured)
            {
                if (!animal.isPredator)
                {
                    totalNonPredatorCount++;
                }
                else
                {
                    totalPredatorCount++;
                }
                CaptureAnimal(animal);
            }
            foreach (var lassoable in lassoablesCaptured)
            {
                currencyBonus += lassoable.currencyToGive;
                currencyMult += lassoable.currencyMultToGive;
                pointBonus += lassoable.pointsToGive;
                pointMult *= lassoable.pointsMultToGive;
                if (lassoable.gameObject.name == "ChickenEgg(Clone)")
                {
                    boonSprites.Add(boonManager.boonDict["Eggstravagant"].art);
                }
            }

            if (boonManager.ContainsBoon("Biodiversity") && animalsCaptured.Count > 0)
            {
                boonSprites.Add(boonManager.boonDict["Biodiversity"].art);
                HashSet<string> uniqueAnimalNames = new HashSet<string>();
                foreach (var a in animalsCaptured)
                {
                    uniqueAnimalNames.Add(a.name);
                }
                pointBonus += (10 * uniqueAnimalNames.Count);
            }
            if (boonManager.ContainsBoon("BoonsBonus")   )
            {
                boonSprites.Add(boonManager.boonDict["BoonsBonus"].art);
                pointBonus += (2 * player.boonsInDeck.Count);
            }

            if (animalsCaptured.Count > FBPP.GetInt("largestCapture"))
            {
                FBPP.SetInt("largestCapture", animalsCaptured.Count);
            }

            if (animalsCaptured.Count > 0 && boonManager.ContainsBoon("CaptureClock"))
            {
                boonSprites.Add(boonManager.boonDict["CaptureClock"].art);
                gameManager.roundDuration += 0.5f;
            }

            if (totalNonPredatorCount > 1)
            {
                if (boonManager.ContainsBoon("HerdMentality"))
                {
                    pointMult *= 1 + (.2f * totalNonPredatorCount);
                    boonSprites.Add(boonManager.boonDict["HerdMentality"].art);
                }
                else
                {
                    pointMult *= 1 + (.1f * totalNonPredatorCount);
                }
                int groupsOf3 = totalNonPredatorCount / 3;
                if (boonManager.ContainsBoon("DustyDividend"))
                { 
                    boonSprites.Add(boonManager.boonDict["DustyDividend"].art);
                    currencyBonus += groupsOf3*2;
                }
                else
                {
                    currencyBonus += groupsOf3;   
                }
            }

            if (boonManager.ContainsBoon("Mootiplier"))
            {
                int cowCount = 0;
                foreach (var animal in animalsCaptured)
                {
                    if (animal.animalData.name == "Cow")
                    {
                        cowCount++;
                    }
                }
                if (cowCount == 0 || totalPredatorCount > 0)
                {
                    mootiplierMult = 0;
                }
                else
                {
                    boonSprites.Add(boonManager.boonDict["Mootiplier"].art);
                    mootiplierMult += (.25f * cowCount);
                }
            }
            pointMult += mootiplierMult;

            if (!firstCapture && boonManager.ContainsBoon("EarlyBird"))
            {
                firstCapture = true;
                pointMult *= 2;
                currencyMult += 2;
                boonSprites.Add(boonManager.boonDict["EarlyBird"].art);
            }
            if (boonManager.ContainsBoon("HailMary") && (gameManager.roundDuration - gameManager.elapsedTime) < 10f)
            {
                pointMult *= 2;
                boonSprites.Add(boonManager.boonDict["HailMary"].art);
            }
            if (boonManager.ContainsBoon("HighFive") && animalsCaptured.Count==5)
            {
                currencyBonus +=5;
                pointBonus += 5;
                boonSprites.Add(boonManager.boonDict["HighFive"].art);
            }
            if (boonManager.ContainsBoon("AbsoluteValue"))
            {
                if (pointBonus * pointMult < 0 || currencyBonus*currencyMult < 0)
                {
                    boonSprites.Add(boonManager.boonDict["AbsoluteValue"].art);
                }
                pointBonus = Mathf.Abs((float)pointBonus);
                pointMult = Mathf.Abs((float)pointMult);
                currencyBonus = Mathf.Abs((float)currencyBonus);
                currencyMult = Mathf.Abs((float)currencyMult);
            }
        }
        return (pointBonus, pointMult, currencyBonus, currencyMult, boonSprites);
    }

    public void ActivateBoon(BasicBoon boon)
    {
        currencyBonus += boon.currencyBonus;
        currencyMult += boon.currencyMult;
        pointBonus += boon.pointsBonus;
        pointMult *=  boon.pointsMult;
        boonSprites.Add(boon.art);
        Debug.Log($"Boon activated: {boon.name} - Currency Bonus: {currencyBonus}, Currency Multiplier: {currencyMult}, Point Bonus: {pointBonus}, Point Multiplier: {pointMult}");
    }

    public virtual void CaptureAnimal(Animal capturedAnimal)
    {
        int bonus = 0;
        if (boonManager.ContainsBoon("VeteranFarmhand"))
            bonus = 2;

        if (capturedAnimal.gameObject.CompareTag("PigWithHat"))
        {
            currencyBonus += 50;
            pointBonus += 25;
            boonSprites.Add(boonManager.boonDict["PigWithHat"].art);
        }

        if (capturedAnimal.gameObject.CompareTag("BlackSheep") && currentTrigger == 0)
        {
            triggers += 1;
            boonSprites.Add(boonManager.boonDict["BlackSheep"].art);
        }

        if (boonManager.ContainsBoon("PointPals"))
        {
            pointBonus += 3;
            boonSprites.Add(boonManager.boonDict["PointPals"].art);
        }
        if (boonManager.ContainsBoon("CashCatch"))
        {
            currencyBonus += 2;
            boonSprites.Add(boonManager.boonDict["CashCatch"].art);
        }
        if (boonManager.ContainsBoon("GoodBoy") && capturedAnimal.animalData.name == "Dog" && totalPredatorCount > 0)
        {
            currencyMult += 5f;
            boonSprites.Add(boonManager.boonDict["GoodBoy"].art);
        }
        if (boonManager.ContainsBoon("ScapeGoat") && capturedAnimal.animalData.name == "Goat")
        {
            currencyBonus += (12 * totalPredatorCount);
            pointBonus += (12 * totalPredatorCount);
            boonSprites.Add(boonManager.boonDict["ScapeGoat"].art);
        }

        currencyBonus += (GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name)+bonus) * capturedAnimal.animalData.currencyLevelUpIncrease + capturedAnimal.animalData.currencyToGive;
        currencyMult += (GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name)+bonus) * capturedAnimal.animalData.currencyLevelUpMult + capturedAnimal.animalData.currencyMultToGive;
        pointBonus += (GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name)+bonus) * capturedAnimal.animalData.pointsLevelUpIncrease + capturedAnimal.animalData.pointsToGive;
        pointMult *= (GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name)+bonus) * capturedAnimal.animalData.pointsLevelUpMult + capturedAnimal.animalData.pointsMultToGive;
        pointBonus += capturedAnimal.bonusPoints;
        int numberAnimalsWrangled = FBPP.GetInt("numberAnimalsWrangled");
        FBPP.SetInt("numberAnimalsWrangled", numberAnimalsWrangled+1);
    }

    private Dictionary<string, int> GetNameCounts(IEnumerable<string> animals)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var animal in animals)
        {
            string key = animal.Trim().ToLower();
            if (counts.ContainsKey(key))
                counts[key]++;
            else
                counts[key] = 1;
        }
        return counts;
    }

    private Dictionary<string, int> GetNameCounts(IEnumerable<Animal> animals)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var animal in animals)
        {
            string key = animal.animalData.animalName.GetLocalizedString().Trim().ToLower();
            if (counts.ContainsKey(key))
                counts[key]++;
            else
                counts[key] = 1;
        }
        return counts;
    }

    private bool IsSubset(Dictionary<string, int> required, Dictionary<string, int> actual)
    {
        foreach (var pair in required)
        {
            if (!actual.ContainsKey(pair.Key) || actual[pair.Key] < pair.Value)
                return false;
        }
        return true;
    }

    private bool AreCountsEqual(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count != b.Count) return false;

        foreach (var pair in a)
        {
            if (!b.ContainsKey(pair.Key) || b[pair.Key] != pair.Value)
                return false;
        }
        return true;
    }
}