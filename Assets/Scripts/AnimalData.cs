using UnityEngine;

[CreateAssetMenu(fileName = "AnimalData", menuName = "Scriptable Objects/AnimalData")]
public class AnimalData : ScriptableObject
{
    [Header("General")]
    public string animalName;
    public string description;
    private int level;
    public int price;
    public Sprite sprite;
    public Sprite sprite2;
    public Sprite deckIcon;
    public float animSpeed;
    public GameObject animalPrefab;
    
    [Space(10), Header("Base Data")]
    public int currencyToGive;
    public int pointsToGive;
    public float currencyMultToGive;
    public float pointsMultToGive;
    
    [Space(10), Header("Upgrade Data")]
    public int pointsLevelUpIncrease;
    public float pointsLevelUpMult;
    public int currencyLevelUpIncrease;
    public float currencyLevelUpMult;
    public int upgradeCost;
}
