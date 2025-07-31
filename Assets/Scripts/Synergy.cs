using UnityEngine;

[CreateAssetMenu(fileName = "Synergy", menuName = "Scriptable Objects/Synergy")]
public class Synergy : ScriptableObject
{
    public string name;
    public string desc;
    public Sprite art;
    public int price;
    
    public int currencyBonus;
    public int pointsBonus;
    public int pointsMult;
    
    public string[] animalsNeeded;
}