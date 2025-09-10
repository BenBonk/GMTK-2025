using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Boon", menuName = "Scriptable Objects/BasicBoon")]
public class BasicBoon: Boon
{
    public bool isExactMatch;
    public int currencyBonus;
    public float currencyMult;
    public int pointsBonus;
    public float pointsMult;
    public string[] animalsNeeded;
}