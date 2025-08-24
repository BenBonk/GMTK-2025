using System.Collections.Generic;
using UnityEngine;

public class AnimalLevelManager : MonoBehaviour
{
    private Dictionary<string, int> animalLevels = new Dictionary<string, int>();

    private void Awake()
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
    }

    private void InitAnimal(string animalName)
    {
        int level = PlayerPrefs.GetInt(animalName, 0); // default 0 if not set
        animalLevels[animalName] = level;
    }

    public int GetLevel(string animalName)
    {
        return animalLevels.TryGetValue(animalName, out int level) ? level : 0;
    }

    public void SetLevel(string animalName, int level)
    {
        animalLevels[animalName] = level;
        PlayerPrefs.SetInt(animalName, level);
        PlayerPrefs.Save();
    }
}