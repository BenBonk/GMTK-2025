using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class Farmer : MonoBehaviour
{
    public GameObject farmerInfo;
    public Image farmerImg;
    public int farmerIndex;
    public Sprite unlockedSprite;
    public bool isUnlocked;
    public AnimalData[] startingDeck;

    private void Start()
    {
        if (isUnlocked)
        {
            farmerImg.sprite = unlockedSprite;
        }
    }

    public void ShowFarmerInfo()
    {
        if (farmerInfo.activeInHierarchy)
        {
            farmerInfo.SetActive(false);
        }
        else
        {

            if (isUnlocked)
            {
                farmerInfo.SetActive(true);
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
        farmerInfo.SetActive(false);
    }
}
