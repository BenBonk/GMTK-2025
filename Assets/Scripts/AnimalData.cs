using UnityEngine;

[CreateAssetMenu(fileName = "AnimalData", menuName = "Scriptable Objects/AnimalData")]
public class AnimalData : ScriptableObject
{
    public string name;
    public string description;
    private int level;
    public int price;
    public Sprite sprite;
    public Sprite sprite2;
    public Sprite deckIcon;
    public float animSpeed;
    public GameObject animalPrefab;
    public int pointsLevelUpIncrease;
    public float pointsLevelUpMult;
    public int currencyLevelUpIncrease;
    public float currencyLevelUpMult;
}
