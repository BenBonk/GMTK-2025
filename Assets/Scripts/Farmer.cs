using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
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
    public TextMeshProUGUI[] animalCountTexts;
    public Image[] animalImages;
    public LocalizedString unlockDescription;
    public TextMeshProUGUI unlockText;

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
            foreach (var entry in uniqueObjects)
            {
                int count = entry.Value.count;
                AnimalData reference = entry.Value.reference;
                animalCountTexts[Array.IndexOf(startingDeck, reference)].transform.parent.gameObject.SetActive(true);
                animalCountTexts[Array.IndexOf(startingDeck, reference)].text = "x" + count;
                animalImages[Array.IndexOf(startingDeck, reference)].sprite = reference.deckIcon;
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
                farmerInfo.gameObject.SetActive(true);
                farmerInfo.DOScale(new Vector3(3.571429f,3.571429f,3.571429f), .25f).SetEase(Ease.OutBack);
                GameController.farmerSelectManager.SelectFarmer(farmerIndex);
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
                    foreach (var image in animalImages)
                    {
                        image.transform.parent.gameObject.SetActive(false);
                    }
                    unlockText.text = unlockDescription.GetLocalizedString();
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
