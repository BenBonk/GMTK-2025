using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "AnimalData", menuName = "Scriptable Objects/AnimalData")]
public class AnimalData : ScriptableObject
{
    [Header("General")]
    public LocalizedString animalName;
    public LocalizedString description;
    private int level;
    public Sprite sprite;
    public Sprite sprite2;
    public Sprite deckIcon;
    public float animSpeed;
    public GameObject animalPrefab;
    public LegendaryBoon legendaryBoon;
    public bool isPredator;
    
    [Space(10), Header("Base Data")]
    public int pointsToGive;
    public float pointsMultToGive;
    public int currencyToGive;
    public float currencyMultToGive;

    [Space(10), Header("Upgrade Data")]
    public int pointsLevelUpIncrease;
    public float pointsLevelUpMult;
    public int currencyLevelUpIncrease;
    public float currencyLevelUpMult;
    public int price;
    public int upgradeCost;
}
