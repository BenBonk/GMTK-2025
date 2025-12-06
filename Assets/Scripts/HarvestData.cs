using UnityEngine;

public enum DifficultySetting
{
    None,
    Novice,
    Veteran,
    Expert,
    Beginner
}

[CreateAssetMenu(fileName = "HarvestData", menuName = "Scriptable Objects/HarvestData")]
public class HarvestData : ScriptableObject
{
    public int harvestLevel;

    [Header("Game Settings")]
    public int roundLength;
    public int numberOfDays;
    public int dailyCash;
    public int predatorFrequency;
    public int predatorOptions;
    public DifficultySetting pointQuotas;
    public DifficultySetting challengeRoundIntensity;
}