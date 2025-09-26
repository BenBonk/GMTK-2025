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
                GameController.localizationManager.localCashString.Arguments[0] = LassoController.FormatNumber(_playerCurrency);
                GameController.localizationManager.localCashString.RefreshString();
            }
        }
    }

    public event Action<double> OnCurrencyChanged;
}