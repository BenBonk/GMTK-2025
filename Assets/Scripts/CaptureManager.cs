using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CaptureManager : MonoBehaviour
{
    private Player player;
    private GameManager gameManager;
    private List<HashSet<string>> synergySets = new List<HashSet<string>>();

    private void Start()
    {
        gameManager = GameController.gameManager;
        player = GameController.player;
    }

    public void MakeCapture(Animal[] animalsCaptured)
    {
        // Check if there is a synergy in the capture
        synergySets.Clear();
        foreach (var captured in player.synergiesInDeck)
        {
            HashSet<string> newSet = new HashSet<string>();
            foreach (var animal in captured.animalsNeeded)
            {
                newSet.Add(animal);
            }
            synergySets.Add(newSet);
        }
        
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
        player.playerCurrency += player.synergiesInDeck[index].currencyBonus;
        gameManager.pointsThisRound += player.synergiesInDeck[index].pointsBonus;
        if (player.synergiesInDeck[index].pointsMult!=0)
        {
            gameManager.pointsThisRound *= player.synergiesInDeck[index].pointsMult;   
        }
    }
}