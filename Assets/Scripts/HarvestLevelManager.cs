using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class HarvestLevelManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text roundLengthText;
    public TMP_Text dailyCashText;
    public TMP_Text numberOfDaysText;
    public TMP_Text predatorFrequencyText;
    public TMP_Text predatorOptionsText;
    public TMP_Text pointQuotasText;
    public TMP_Text challengeIntensityText;
    public TMP_Text harvestLevelText;
    
    public LocalizedString roundLengthLocalized;
    public LocalizedString dailyCashLocalized;
    public LocalizedString numberOfDaysLocalized;
    public LocalizedString predatorFrequencyLocalized;
    public LocalizedString predatorFrequencyLocalized2;
    public LocalizedString predatorOptionsLocalized;
    public LocalizedString pointQuotasLocalized;
    public LocalizedString challengeIntensityLocalized;

    public LocalizedString[] difficultyLevels;

    public GameObject decreaseButton;
    public GameObject increaseButton;
    void Start()
    {
        SetHarvestLevel(1); // Initialize to level 1
    }

    public void SetHarvestLevel(int levelIndex)
    {
        levelIndex--; // Convert to 0-based index

        increaseButton.SetActive(true);
        if (levelIndex == 0)
        {
            decreaseButton.SetActive(false);
        }
        else
        {
            decreaseButton.SetActive(true);
        }

        if (levelIndex < 0 || levelIndex >= FBPP.GetInt("harvestLevelsUnlocked", 1)-1)
        {
            if (levelIndex >= GameController.saveManager.harvestDatas.Length)
            {
                GetComponent<StampPopup>().ShowStampAtMouse();
                AudioManager.Instance.PlaySFX("no_point_mult");
                return;
            }
            else
            {
                increaseButton.SetActive(false);
            }
        }

        HarvestData data = GameController.saveManager.harvestDatas[levelIndex];
        AudioManager.Instance.PlaySFX("ui_crunch");

        roundLengthText.text = $"{roundLengthLocalized.GetLocalizedString()} {data.roundLength}";
        dailyCashText.text = $"{dailyCashLocalized.GetLocalizedString()} {data.dailyCash}";
        numberOfDaysText.text = $"{numberOfDaysLocalized.GetLocalizedString()} {data.numberOfDays}";
        predatorFrequencyText.text = $"{predatorFrequencyLocalized.GetLocalizedString()} {data.predatorFrequency} {predatorFrequencyLocalized2.GetLocalizedString()}";
        predatorOptionsText.text = $"{predatorOptionsLocalized.GetLocalizedString()} {data.predatorOptions}";
        pointQuotasText.text = $"{pointQuotasLocalized.GetLocalizedString()} {difficultyLevels[(int)data.pointQuotas].GetLocalizedString()}";
        challengeIntensityText.text = $"{challengeIntensityLocalized.GetLocalizedString()} {difficultyLevels[(int)data.challengeRoundIntensity].GetLocalizedString()}";
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
