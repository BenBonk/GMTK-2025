using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimalLevelManager : MonoBehaviour
{
    private Dictionary<string, int> animalLevels = new Dictionary<string, int>();

    private void Start()
    {
        InitAnimal("Sheep");
        InitAnimal("Pig");
        InitAnimal("Cow");
        InitAnimal("Wolf");
        InitAnimal("Chicken");
        InitAnimal("Fox");
        InitAnimal("Bear");
        InitAnimal("Horse");
        InitAnimal("Goat");
        InitAnimal("Dog");
        InitAnimal("Alpaca");
        InitAnimal("Puma");
        InitAnimal("Boar");
        InitAnimal("Mouse");
        InitAnimal("BarnCat");
        InitAnimal("Bunny");
    }
//a
    public void ResetLevels()
    {
        var keys = new List<string>(animalLevels.Keys);
        foreach (var animalName in keys)
        {
            animalLevels[animalName] =0;
            FBPP.SetInt(animalName, 0);
        }
        FBPP.Save();
    }

    private void InitAnimal(string animalName)
    {
        int level = FBPP.GetInt(animalName, 0);
        animalLevels[animalName] = level;
    }

    public int GetLevel(string animalName)
    {
        return animalLevels.TryGetValue(animalName, out int level) ? level : 0;
    }

    public void SetLevel(string animalName, int level)
    {
        animalLevels[animalName] = level;
        FBPP.SetInt(animalName, level);
        FBPP.Save();
    }
    public bool AllAnimalsAtDefaultLevel()
    {
        foreach (var kvp in animalLevels)
        {
            if (kvp.Value != 0)
                return false;
        }
        return true;
    }

}