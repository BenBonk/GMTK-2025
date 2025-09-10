using UnityEngine;

public enum ShopPriceSetting
{
    Novice,
    Veteran,
    Expert
}

public enum PointQuotaSetting
{
    Novice,
    Veteran,
    Expert
}

[CreateAssetMenu(fileName = "HarvestData", menuName = "Scriptable Objects/HarvestData")]
public class HarvestData : ScriptableObject
{
    public int harvestLevel;

    [Header("Game Settings")]
    public int roundLength;
    public int dailyCash;
    public int numberOfDays;
    public int startingPredators;

    [Header("Shop Settings")]
    public ShopPriceSetting shopPrices;
    public PointQuotaSetting pointQuotas;
}