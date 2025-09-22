using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class Farmer : MonoBehaviour
{
    public RectTransform farmerInfo;
    public Image farmerImg;
    public int farmerIndex;
    public Sprite unlockedSprite;
    public bool isUnlocked;
    public AnimalData[] startingDeck;

    private void Start()
    {
        HideFarmerInfo();
        if (isUnlocked)
        {
            farmerImg.sprite = unlockedSprite;
        }
    }

    public void ShowFarmerInfo()
    {
        if (farmerInfo.gameObject.activeInHierarchy)
        {
            HideFarmerInfo();
        }
        else
        {
            if (isUnlocked)
            {
                farmerInfo.gameObject.SetActive(true);
                farmerInfo.DOScale(new Vector3(3.571429f,3.571429f,3.571429f), .25f).SetEase(Ease.OutBack);
                GameController.farmerSelectManager.SelectFarmer(farmerIndex);
            }
            else
            {
                //not available in demo!
                GetComponent<StampPopup>().ShowStampAtMouse();
            }
        }
    }

    public void HideFarmerInfo()
    {
        farmerInfo.DOScale(Vector3.zero, .25f).SetEase(Ease.InBack)
            .OnComplete(() => farmerInfo.gameObject.SetActive(false));
    }
}
