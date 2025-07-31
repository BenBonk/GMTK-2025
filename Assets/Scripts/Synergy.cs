using UnityEngine;

[CreateAssetMenu(fileName = "Synergy", menuName = "Scriptable Objects/Synergy")]
public class Synergy : ScriptableObject
{
    public string name;
    public string desc;
    public Sprite art;
    public int price;
    public bool isExactMatch;
    
    public int currencyBonus;
    public float currencyMult;
    public int pointsBonus;
    public float pointsMult;
    
    public string[] animalsNeeded;
}