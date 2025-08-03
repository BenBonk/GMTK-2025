using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimalLevelManager : MonoBehaviour
{
    private Dictionary<string, int> animalLevels = new Dictionary<string, int>();

    private void Awake()
    {
        animalLevels["Sheep"] = 0;
        animalLevels["Pig"] = 0;
        animalLevels["Cow"] = 0;
        animalLevels["Wolf"] = 0;
        animalLevels["Chicken"] = 0;
        animalLevels["Fox"] = 0;
        animalLevels["Bear"] = 0;
        animalLevels["Horse"] = 0;
        animalLevels["Goat"] = 0;
        animalLevels["Dog"] = 0;
    }

    public int GetLevel(string name)
    {
        if (animalLevels.TryGetValue(name, out int level))
        {
            return level;
        }
        
        return 0; //Default level
    }

    public void SetLevel(string name, int newLevel)
    {
        animalLevels[name] = newLevel;
    }
}
