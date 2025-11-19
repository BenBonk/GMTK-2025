using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //public int timeToSpawnAnimals;
    public List<AnimalData> animalsInDeck;
    public List<Boon> boonsInDeck;
    private SteamIntegration steamIntegration;

    private void Start()
    {
        steamIntegration = GameController.steamIntegration;
    }

    public void AddAnimalToDeck(AnimalData animal)
    {
        animalsInDeck.Add(animal);
    }
    public void RemoveAnimalFromDeck(AnimalData animal)
    {
        animalsInDeck.Remove(animal);
        if (animalsInDeck.Count==1 && !steamIntegration.IsThisAchievementUnlocked("Desolation"))
        {
            steamIntegration.UnlockAchievement("Desolation");
        }
    }
    public void AddBoonToDeck(Boon boon)
    {
        GameController.boonManager.AddBoon(boon);
        if (boonsInDeck.Count==10 && !steamIntegration.IsThisAchievementUnlocked("Boons For Days"))
        {
            steamIntegration.UnlockAchievement("Boons For Days");
        }
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