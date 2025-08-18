using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Synergy", menuName = "Scriptable Objects/Synergy")]
public class Synergy : ScriptableObject
{
    public LocalizedString synergyName;
    public LocalizedString desc;
    public Sprite art;
    public int price;
    public bool isExactMatch;
    
    public int currencyBonus;
    public float currencyMult;
    public int pointsBonus;
    public float pointsMult;
    
    public string[] animalsNeeded;
}