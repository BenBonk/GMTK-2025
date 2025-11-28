using System;
using System.Collections;
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
    [HideInInspector] public float mootiplier=1;

    private void Start()
    {
        boonManager = GameController.boonManager;
        gameManager = GameController.gameManager;
        player = GameController.player;
        steamIntegration = GameController.steamIntegration;
    }

    private int triggers = 1;
    private int currentTrigger = 0;
    private int totalPredatorCount = 0;
    HashSet<Sprite> boonSprites = new HashSet<Sprite>();
    private SteamIntegration steamIntegration;
    public Sprite lightningBoltIcon;
    public Sprite toniIcon;
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
                        if (animal.animalData.name == "Mouse" && boonManager.ContainsBoon("ThreeBlindMice"))
                        {
                            boonSprites.Add(boonManager.boonDict["ThreeBlindMice"].art);
                            animalsCaptured.Add(animal);
                            animalsCaptured.Add(animal);
                        }
                    }
                }
            }

            if (animalsCaptured.Count>=25 && !steamIntegration.IsThisAchievementUnlocked("Crowd Control"))
            {
                steamIntegration.UnlockAchievement("Crowd Control");
            }
            var capturedCounts = GetNameCounts(animalsCaptured);
            /*foreach (var key in capturedCounts.Keys)
            {
                Debug.Log(key + ": " + capturedCounts[key]);
            }*/

            for (int i = 0; i < player.boonsInDeck.Count; i++)
            {
                if (player.boonsInDeck[i] is not BasicBoon boon)
                {
                    continue;
                }
                var neededCounts = GetNameCounts(boon.animalsNeeded);
                //Debug.Log($"Checking boon {boon.name}: Needed = [{string.Join(",", neededCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}], Captured = [{string.Join(",", capturedCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}]");

                if (!boon.isExactMatch)
                {
                    bool subset = IsSubset(neededCounts, capturedCounts);
                    //Debug.Log($"Non-exact match check for {boon.name}: Result = {subset}");
                    if (subset) ActivateBoon(boon);
                }

                else
                {
                    if (AreCountsEqual(neededCounts, capturedCounts))
                        ActivateBoon(boon);
                }
            }

            int totalNonPredatorCount = 0;

            foreach (var animal in animalsCaptured)
            {
                if (animal.struckByLightning)
                {
                    pointMult *= 1.5;
                    currencyMult *= 1.25;
                    boonSprites.Add(lightningBoltIcon);
                }

                if (animal.animalData.name=="BarnCat" && boonManager.ContainsBoon("Copycat"))
                {
                    animal.animalData = animalsCaptured
                        .OrderByDescending(a => a.animalData.pointsMultToGive)
                        .ThenByDescending(a => a.animalData.pointsToGive)
                        .Select(a => a.animalData)
                        .FirstOrDefault();
                    boonSprites.Add(boonManager.boonDict["Copycat"].art);
                }
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

            if((boonManager.ContainsBoon("Biodiversity") || !FBPP.GetBool("farmer7", false)) && animalsCaptured.Count > 0)
            {
                HashSet<string> uniqueAnimalNames = new HashSet<string>();
                foreach (var a in animalsCaptured)
                {
                    uniqueAnimalNames.Add(a.name);
                }
                if (boonManager.ContainsBoon("Biodiversity"))
                {
                    boonSprites.Add(boonManager.boonDict["Biodiversity"].art);
                    pointBonus += (10 * uniqueAnimalNames.Count);
                }
                if (!FBPP.GetBool("farmer7", false) && uniqueAnimalNames.Count >= gameManager.animalShopItem.possibleAnimals.Length)
                {
                    FBPP.SetBool("farmer7", true);
                    gameManager.auroraUnlock = true;
                }
            }

            if (boonManager.ContainsBoon("BoonsBonus"))
            {
                boonSprites.Add(boonManager.boonDict["BoonsBonus"].art);
                pointBonus += (2 * player.boonsInDeck.Count);
                currencyBonus += player.boonsInDeck.Count;
            }

            if (boonManager.ContainsBoon("Yahtzee"))
            {
                bool hasFive = capturedCounts.Values.Any(v => v == 5);
                if (hasFive)
                {
                    pointMult *= 2;
                    boonSprites.Add(boonManager.boonDict["Yahtzee"].art);
                }
            }
            
           if (boonManager.ContainsBoon("Wolfpack"))
            {
                if (capturedCounts.ContainsKey("wolf") && capturedCounts["wolf"] >=5)
                {
                    pointMult *= 25;
                    boonSprites.Add(boonManager.boonDict["Wolfpack"].art);
                }
            }
            
            if (boonManager.ContainsBoon("NoahsArk") && animalsCaptured.Count == 2 && capturedCounts.Keys.Count == 1)
            {
                if (UnityEngine.Random.value < .07f)
                {
                    boonSprites.Add(boonManager.boonDict["NoahsArk"].art);
                    Animal chosenAnimal = animalsCaptured[0];
                    GameController.animalLevelManager.SetLevel(chosenAnimal.animalData.name, GameController.animalLevelManager.GetLevel(chosenAnimal.animalData.name) + 1);
                    if (GameController.animalLevelManager.GetLevel(chosenAnimal.name) > FBPP.GetInt("highestAnimalLevel"))
                    {
                        FBPP.SetInt("highestAnimalLevel", GameController.animalLevelManager.GetLevel(chosenAnimal.animalData.name));
                    }
                }
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
                    currencyBonus += groupsOf3*5;
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
                    mootiplier = 1;
                }
                else
                {
                    boonSprites.Add(boonManager.boonDict["Mootiplier"].art);
                    mootiplier += (.25f * cowCount);
                }
                pointMult *= mootiplier;
            }

            if (boonManager.ContainsBoon("HoldYourHorses") && capturedCounts.ContainsKey("horse") && capturedCounts["horse"] > 0)
            {
                pointBonus += 10;
            }

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
            if (boonManager.ContainsBoon("TimeIsMoney"))
            {
                currencyMult += 0.33;
                boonSprites.Add(boonManager.boonDict["TimeIsMoney"].art);
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
                pointBonus = Math.Abs(pointBonus);
                pointMult = Math.Abs(pointMult);
                currencyBonus = Math.Abs(currencyBonus);
                currencyMult = Math.Abs(currencyMult);
            }
            if (gameManager.farmerID == 5)
            {
                if (pointBonus * pointMult < 0 || currencyBonus * currencyMult < 0)
                {
                    boonSprites.Add(toniIcon);
                }
                pointBonus = Math.Abs(pointBonus);
                pointMult = Math.Abs(pointMult);
                currencyBonus = Math.Abs(currencyBonus);
                currencyMult = Math.Abs(currencyMult);
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
            boonSprites.Add(boonManager.boonDict["PigsWithHats"].art);
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
        if (UnityEngine.Random.Range(0,4) == 0 && boonManager.ContainsBoon("RabbitsFoot") && capturedAnimal.animalData.name == "Bunny")
        {
            StartCoroutine(RabbitsFoot());
            boonSprites.Add(boonManager.boonDict["RabbitsFoot"].art);
        }
        if (boonManager.ContainsBoon("ForeverFaster") && capturedAnimal.animalData.name == "Puma" && currentTrigger == 0 && capturedAnimal.GetComponent<Puma>().isLeapingAtTarget)
        {
            triggers += 1;
            boonSprites.Add(boonManager.boonDict["ForeverFaster"].art);
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
        if (numberAnimalsWrangled == 100 && !steamIntegration.IsThisAchievementUnlocked("Wrangle Novice"))
        {
            steamIntegration.UnlockAchievement("Wrangle Novice");
        }
        else if (numberAnimalsWrangled == 1000 && !steamIntegration.IsThisAchievementUnlocked("Wrangle Pro"))
        {
            steamIntegration.UnlockAchievement("Wrangle Pro");
        }
        else if (numberAnimalsWrangled == 10000 && !steamIntegration.IsThisAchievementUnlocked("Wrangle Expert"))
        {
            steamIntegration.UnlockAchievement("Wrangle Expert");
        }
    }

    IEnumerator RabbitsFoot()
    {
        yield return new WaitForSeconds(.5f);
        GameController.gameManager.pointsThisRound *= 2;
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