using DG.Tweening;
using System;
using UnityEngine;

public class FarmerSelectManager : MonoBehaviour
{
    public Farmer[] farmers;
    public GameObject harvestLevelPanel;
    public GameObject startGameButton;
    public GameObject titleObject;
    public int selectedFarmerIndex = -1;

    private void Awake()
    {
        CheckUnlokcedFarmers();
    }

    public void SelectFarmer(int index)
    {
        selectedFarmerIndex = index;

        HideAllFarmerInfo(index);

        if (!harvestLevelPanel.activeInHierarchy)
        {
            harvestLevelPanel.SetActive(true);
            GetComponent<RectTransform>().DOAnchorPosX(0, 0.35f).SetEase(Ease.InBack);
            titleObject.GetComponent<RectTransform>().DOAnchorPosX(210f, 0.35f).SetEase(Ease.InBack);
            harvestLevelPanel.GetComponent<RectTransform>().DOAnchorPosY(-35f, 0.5f).SetEase(Ease.InOutBack).SetDelay(0.25f);
        }
        if (!startGameButton.activeInHierarchy)
        {
            startGameButton.SetActive(true);
            startGameButton.GetComponent<RectTransform>().DOAnchorPosY(0f, 0.5f).SetEase(Ease.InOutBack).SetDelay(0.25f);
        }
    }

    public void CheckUnlokcedFarmers()
    {
        foreach (var dude in farmers)
        {
            if (dude.farmerData != null)
            {
                if (FBPP.GetBool("farmer" + dude.farmerData.farmerIndex,false))
                {
                    dude.isUnlocked = true;
                }
            }
        }
    }

    public void HideAllFarmerInfo(int except)
    {
        foreach (var dude in farmers)
        {
            if (dude.farmerData != null && dude.farmerData.farmerIndex != except)
                dude.HideFarmerInfo();
        }
    }
}
