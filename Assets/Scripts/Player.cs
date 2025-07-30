using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerCurrency;
    public int timeToSpawnAnimals;
    
    public List<GameObject> animalsInDeck;

    public void AddAnimalToDeck(GameObject animal)
    {
        animalsInDeck.Add(animal);
    }
}