using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using System.Linq;

public class CaptureManager : MonoBehaviour
{
    private Player player;
    private GameManager gameManager;
    // private List<HashSet<string>> synergySets = new List<HashSet<string>>();

    private int pointBonus = 0;
    private float pointMult = 1;
    private int currencyBonus = 0;
    private float currencyMult = 1;

    private void Start()
    {
        gameManager = GameController.gameManager;
        player = GameController.player;
    }

    public (int, float, int, float) MakeCapture(Animal[] animalsCaptured)
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

            if (!synergy.isExactMatch)
            {
                if (IsSubset(neededCounts, capturedCounts))
                    ActivateSynergy(i);
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
        }

        return (pointBonus, pointMult, currencyBonus, currencyMult);
    }

    public void ActivateSynergy(int index)
    {
        currencyBonus += player.synergiesInDeck[index].currencyBonus;
        currencyMult *= player.synergiesInDeck[index].currencyMult;
        pointBonus += player.synergiesInDeck[index].pointsBonus;
        pointMult *= player.synergiesInDeck[index].pointsMult;
    }

    public virtual void CaptureAnimal(Animal capturedAnimal)
    {
        currencyBonus += GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.currencyLevelUpIncrease + capturedAnimal.currencyToGive;
        currencyMult *= GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.currencyLevelUpMult + capturedAnimal.currencyMultToGive;
        pointBonus += GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.pointsLevelUpIncrease + capturedAnimal.pointsToGive;
        pointMult *= GameController.animalLevelManager.GetLevel(capturedAnimal.animalData.name) * capturedAnimal.animalData.pointsLevelUpMult + capturedAnimal.pointsMultToGive;
    }

    private Dictionary<string, int> GetNameCounts(IEnumerable<String> animals)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var animal in animals)
        {
            if (counts.ContainsKey(animal))
                counts[name]++;
            else
                counts[name] = 1;
        }
        return counts;
    }

    private Dictionary<string, int> GetNameCounts(IEnumerable<Animal> animals)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var animal in animals)
        {
            if (counts.ContainsKey(animal.name))
                counts[name]++;
            else
                counts[name] = 1;
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