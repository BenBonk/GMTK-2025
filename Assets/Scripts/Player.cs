using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //public int timeToSpawnAnimals;
    
    public List<AnimalData> animalsInDeck;
    public int lassosPerRound;
    public List<Synergy> synergiesInDeck;
    public void AddAnimalToDeck(AnimalData animal)
    {
        animalsInDeck.Add(animal);
    }
    public void AddSynergyToDeck(Synergy synergy)
    {
        synergiesInDeck.Add(synergy);
    }

    private int _playerCurrency;
    public int playerCurrency
    {
        get => _playerCurrency;
        set
        {
            if (_playerCurrency != value)
            {
                _playerCurrency = value;
                OnCurrencyChanged?.Invoke(_playerCurrency);
            }
        }
    }

    public event Action<int> OnCurrencyChanged;
}