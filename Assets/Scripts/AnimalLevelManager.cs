using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimalLevelManager : MonoBehaviour
{
    private Dictionary<string, int> animalLevels = new Dictionary<string, int>();

    private void Awake()
    {
        animalLevels["Sheep"] = 1;
        animalLevels["Pig"] = 1;
        animalLevels["Cow"] = 1;
        animalLevels["Wolf"] = 1;
        animalLevels["Chicken"] = 1;
        animalLevels["Fox"] = 1;
        animalLevels["Bear"] = 1;
        animalLevels["Horse"] = 1;
        animalLevels["Goat"] = 1;
        animalLevels["Dog"] = 1;
    }

    public int GetLevel(string name)
    {
        if (animalLevels.TryGetValue(name, out int level))
        {
            return level;
        }
        
        return 1; //Default level
    }

    public void SetLevel(string name, int newLevel)
    {
        animalLevels[name] = newLevel;
    }
}
