using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "FarmerData", menuName = "Scriptable Objects/FarmerData")]
public class FarmerData : ScriptableObject
{
    public LocalizedString farmerName;
    public LocalizedString description;
    public LocalizedString unlockDescription;
    public Sprite sprite;
    public Sprite lockedSprite;
    public Sprite selectedSprite;
    public AnimalData[] startingDeck;
    public string audioClipName;
    public int farmerIndex;
}