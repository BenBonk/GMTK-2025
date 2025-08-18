using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class CaptureManager : MonoBehaviour
{
    private Player player;
    private GameManager gameManager;
    // private List<HashSet<string>> synergySets = new List<HashSet<string>>();

    private double pointBonus = 0;
    private double pointMult = 1;
    private double currencyBonus = 0;
    private double currencyMult = 1;

    private void Start()
    {
        gameManager = GameController.gameManager;
        player = GameController.player;
    }

    public (double, double, double, double) MakeCapture(Animal[] animalsCaptured)
    {
        pointBonus = 0;
        pointMult = 1;
        currencyBonus = 0;
        currencyMult = 1;

        var capturedCounts = GetNameCounts(animalsCaptured);

        for (int i = 0; i < player.synergiesInDeck.Count; i++)
        {
            var synergy = player.synergiesInDeck[i];
            var neededCounts = GetNameCounts(synergy.animalsNeeded);
            Debug.Log($"Checking synergy {synergy.name}: Needed = [{string.Join(",", neededCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}], Captured = [{string.Join(",", capturedCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}]");

            if (!synergy.isExactMatch)
            {
                bool subset = IsSubset(neededCounts, capturedCounts);
                Debug.Log($"Non-exact match check for {synergy.name}: Result = {subset}");
                if (subset) ActivateSynergy(i);
            }

            else
            {
                if (AreCountsEqual(neededCounts, capturedCounts))
                    ActivateSynergy(i);
            }
        }

        int totalNonPredatorCount = 0;
        foreach (var animal in animalsCaptured)
        {
            if (!animal.isPredator)
            {
                totalNonPredatorCount++;
            }
            CaptureAnimal(animal);
        }

        if (totalNonPredatorCount > 1)
        {
            pointMult *= 1 + (0.1f * animalsCaptured.Length);
            int groupsOf3 = totalNonPredatorCount / 3;
            currencyBonus += groupsOf3;
        }

        return (pointBonus, pointMult, currencyBonus, currencyMult);
    }

    public void ActivateSynergy(int index)
    {
        currencyBonus += player.synergiesInDeck[index].currencyBonus;
        currencyMult *= player.synergiesInDeck[index].currencyMult;
        pointBonus += player.synergiesInDeck[index].pointsBonus;
        pointMult *= player.synergiesInDeck[index].pointsMult;
        Debug.Log($"Synergy activated: {player.synergiesInDeck[index].name} - Currency Bonus: {currencyBonus}, Currency Multiplier: {currencyMult}, Point Bonus: {pointBonus}, Point Multiplier: {pointMult}");
    }

    public virtual void CaptureAnimal(Animal capturedAnimal)
    {
        currencyBonus += GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.currencyLevelUpIncrease + capturedAnimal.animalData.currencyToGive;
        currencyMult *= GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.currencyLevelUpMult + capturedAnimal.animalData.currencyMultToGive;
        pointBonus += GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.pointsLevelUpIncrease + capturedAnimal.animalData.pointsToGive;
        pointMult *= GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.pointsLevelUpMult + capturedAnimal.animalData.pointsMultToGive;    
        Debug.Log($"Captured animal: {capturedAnimal.name} - Currency Bonus: {currencyBonus}, Currency Multiplier: {currencyMult}, Point Bonus: {pointBonus}, Point Multiplier: {pointMult}");
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