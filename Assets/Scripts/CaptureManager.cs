using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CaptureManager : MonoBehaviour
{
    private Player player;
    private GameManager gameManager;
    public Synergy[] possibleSynergies;
    private List<HashSet<string>> synergySets = new List<HashSet<string>>();

    private void Start()
    {
        gameManager = GameController.gameManager;
        player = GameController.player;
        foreach (var captured in possibleSynergies)
        {
            HashSet<string> newSet = new HashSet<string>();
            foreach (var animal in captured.animalsNeeded)
            {
                newSet.Add(animal.name);
            }
            synergySets.Add(newSet);
        }
    }

    public void MakeCapture(Animal[] animalsCaptured)
    {
        // Check if there is a synergy in the capture
        
        HashSet<string> capturedSet = new HashSet<string>();
        foreach (var animal in animalsCaptured)
        {
            capturedSet.Add(animal.name);
        }

        for (int i = 0; i < synergySets.Count; i++)
        {
            if (capturedSet.IsSubsetOf(synergySets[i]))
            {
                // We have a synergy found!
                ActivateSynergy(i);
            }
        }

        foreach (var animal in animalsCaptured)
        {
            animal.CaptureAnimal();
        }
    }

    public void ActivateSynergy(int index)
    {
        player.playerCurrency += possibleSynergies[index].currencyBonus;
        gameManager.pointsThisRound += possibleSynergies[index].pointsBonus;
        gameManager.pointsThisRound *= possibleSynergies[index].pointsMult;
    }
}

[System.Serializable]
public class Synergy
{
    public Animal[] animalsNeeded;
    public int currencyBonus;
    public int pointsBonus;
    public int pointsMult;
}