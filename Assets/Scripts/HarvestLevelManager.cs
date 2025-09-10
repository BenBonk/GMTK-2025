using UnityEngine;
using TMPro;
public class HarvestLevelManager : MonoBehaviour
{

    [Header("Harvest Levels")]
    public HarvestData[] harvestLevels;

    [Header("UI References")]
    public TMP_Text roundLengthText;
    public TMP_Text dailyCashText;
    public TMP_Text numberOfDaysText;
    public TMP_Text startingPredatorsText;
    public TMP_Text shopPricesText;
    public TMP_Text pointQuotasText;
    public TMP_Text harvestLevelText;

    public GameObject decreaseButton;
    void Start()
    {
        SetHarvestLevel(1); // Initialize to level 1
    }

    public void SetHarvestLevel(int levelIndex)
    {
        levelIndex--; // Convert to 0-based index

        if (levelIndex == 0)
        {
            decreaseButton.SetActive(false);
        }
        else
        {
            decreaseButton.SetActive(true);
        }

        if (levelIndex < 0 || levelIndex >= harvestLevels.Length)
        {
            if (levelIndex >= harvestLevels.Length)
            {
                GetComponent<StampPopup>().ShowStampAtMouse();
            }
            Debug.LogWarning($"Invalid harvest level index: {levelIndex}");
            return;
        }

        HarvestData data = harvestLevels[levelIndex];

        roundLengthText.text = $"Round Length: {data.roundLength}";
        dailyCashText.text = $"Daily Cash: {data.dailyCash}";
        numberOfDaysText.text = $"Number of Days: {data.numberOfDays}";
        startingPredatorsText.text = $"Starting Predators: +{data.startingPredators}";
        shopPricesText.text = $"Shop Prices: {data.shopPrices}";
        pointQuotasText.text = $"Point Quotas: {data.pointQuotas}";
        harvestLevelText.text = $"{data.harvestLevel}";
    }

    public void IncreaseLevel()
    {
        SetHarvestLevel(int.Parse(harvestLevelText.text) + 1);
    }

    public void DecreaseLevel() 
    {
        SetHarvestLevel(int.Parse(harvestLevelText.text) - 1);
    }


}
