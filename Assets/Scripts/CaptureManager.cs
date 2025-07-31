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

    public (int,float,int,float) MakeCapture(Animal[] animalsCaptured)
    {
        // Check if there is a synergy in the capture
        //synergySets.Clear();
        /*foreach (var playerSynergies in player.synergiesInDeck)
        {
            HashSet<string> newSet = new HashSet<string>();
            foreach (var animal in playerSynergies.animalsNeeded)
            {
                newSet.Add(animal);
            }
            synergySets.Add(newSet);
        }*/
        pointBonus = 0;
        pointMult = 1;
        currencyBonus = 0;
        currencyMult = 1;

        HashSet<string> capturedSet = new HashSet<string>();
        foreach (var animal in animalsCaptured)
        {
            capturedSet.Add(animal.name);
        }

        for (int i = 0; i < player.synergiesInDeck.Count; i++)
        {
            if (!player.synergiesInDeck[i].isExactMatch && player.synergiesInDeck[i].animalsNeeded.All(animal => capturedSet.Contains(animal)))
            {
                // We have a synergy found!
                ActivateSynergy(i);
            }
            else if (player.synergiesInDeck[i].isExactMatch && capturedSet.SetEquals(player.synergiesInDeck[i].animalsNeeded))
            {
                // We have an exact match synergy found!
                ActivateSynergy(i);
            }
        }

        foreach (var animal in animalsCaptured)
        {
            CaptureAnimal(animal);
        }

        return (pointBonus,pointMult,currencyBonus,currencyMult);
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
        currencyBonus += capturedAnimal.currencyToGive;
        currencyMult *= capturedAnimal.currencyMultToGive;
        pointBonus += capturedAnimal.pointsToGive;
        pointMult *= capturedAnimal.pointsMultToGive;
    }
}