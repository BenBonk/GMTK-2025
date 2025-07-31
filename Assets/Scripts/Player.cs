using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerCurrency;
    //public int timeToSpawnAnimals;
    
    public List<GameObject> animalsInDeck;
    public List<Synergy> synergiesInDeck;
    public void AddAnimalToDeck(GameObject animal)
    {
        animalsInDeck.Add(animal);
    }
    public void AddSynergyToDeck(Synergy synergy)
    {
        synergiesInDeck.Add(synergy);
    }
}