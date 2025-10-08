using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class Farmer : MonoBehaviour
{
    public RectTransform farmerInfo;
    public Image farmerImg;
    public int farmerIndex;
    public Sprite unlockedSprite;
    public bool isUnlocked;
    public AnimalData[] startingDeck;
    public TextMeshProUGUI[] animalCountTexts;
    public Image[] animalImages;
    public LocalizedString unlockDescription;
    public TextMeshProUGUI unlockText;
    public TextMeshProUGUI farmerName;

    private void Start()
    {
        HideFarmerInfo();
        if (isUnlocked)
        {
            farmerImg.sprite = unlockedSprite;
            Dictionary<string, (int count, AnimalData reference)> uniqueObjects = new Dictionary<string, (int, AnimalData)>();
            foreach (AnimalData obj in startingDeck)
            {
                string name = obj.animalName.GetLocalizedString();

                if (uniqueObjects.ContainsKey(name))
                {
                    uniqueObjects[name] = (uniqueObjects[name].count + 1, uniqueObjects[name].reference);
                }
                else
                {
                    uniqueObjects[name] = (1, obj);
                }
            }
            int i = 0;
            foreach (var entry in uniqueObjects)
            {
                int count = entry.Value.count;
                AnimalData reference = entry.Value.reference;
                animalCountTexts[i].transform.parent.gameObject.SetActive(true);
                animalCountTexts[i].text = "x" + count;
                animalImages[i].sprite = reference.deckIcon;
                i++;
            }
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
                GameController.farmerSelectManager.SelectFarmer(farmerIndex);
                farmerInfo.gameObject.SetActive(true);
                farmerInfo.DOScale(new Vector3(3.571429f, 3.571429f, 3.571429f), .25f).SetEase(Ease.OutBack);
            }
            else
            {
                if (unlockedSprite == null)
                {
                    //not available in demo!
                    GetComponent<StampPopup>().ShowStampAtMouse();
                }
                else
                {
                    GameController.farmerSelectManager.HideAllFarmerInfo(farmerIndex);
                    farmerInfo.gameObject.SetActive(true);
                    farmerInfo.DOScale(new Vector3(3.571429f, 3.571429f, 3.571429f), .25f).SetEase(Ease.OutBack);
                    foreach (var image in animalImages)
                    {
                        image.transform.parent.gameObject.SetActive(false);
                    }
                    unlockText.text = unlockDescription.GetLocalizedString();
                    farmerName.text = "???";
                }
            }
        }
    }

    public void HideFarmerInfo()
    {
        farmerInfo.DOScale(Vector3.zero, .25f).SetEase(Ease.InBack)
            .OnComplete(() => farmerInfo.gameObject.SetActive(false));
    }
}
