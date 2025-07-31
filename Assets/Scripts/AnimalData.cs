using UnityEngine;

[CreateAssetMenu(fileName = "AnimalData", menuName = "Scriptable Objects/AnimalData")]
public class AnimalData : ScriptableObject
{
    public string name;
    public string description;
    private int level;
    public int price;
    public Sprite sprite;
}
