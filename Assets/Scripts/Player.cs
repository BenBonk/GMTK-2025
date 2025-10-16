using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //public int timeToSpawnAnimals;
    
    public List<AnimalData> animalsInDeck;
    public List<Boon> boonsInDeck;

    private void Start()
    {
    }

    public void AddAnimalToDeck(AnimalData animal)
    {
        animalsInDeck.Add(animal);
    }
    public void RemoveAnimalFromDeck(AnimalData animal)
    {
        animalsInDeck.Remove(animal);
    }
    public void AddBoonToDeck(Boon boon)
    {
        GameController.boonManager.AddBoon(boon);
    }

    private double _playerCurrency;
    public double playerCurrency
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

    public event Action<double> OnCurrencyChanged;
}