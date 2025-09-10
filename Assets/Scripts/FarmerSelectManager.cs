using UnityEngine;
using DG.Tweening;

public class FarmerSelectManager : MonoBehaviour
{
    public Farmer[] farmers;
    public GameObject harvestLevelPanel;
    public GameObject startGameButton;
    public GameObject titleObject;
    public int selectedFarmerIndex = -1;
    public void SelectFarmer(int index)
    {
        selectedFarmerIndex = index;
        foreach (var dude in farmers)
        {
            if (dude.farmerIndex != index)
            {
                dude.HideFarmerInfo();
            }
        }
        if (!harvestLevelPanel.activeInHierarchy)
        {
            harvestLevelPanel.SetActive(true);
            GetComponent<RectTransform>().DOAnchorPosX(0, 0.25f).SetEase(Ease.InBack);
            titleObject.GetComponent<RectTransform>().DOAnchorPosX(0, 0.25f).SetEase(Ease.InBack);
            harvestLevelPanel.GetComponent<RectTransform>().DOAnchorPosY(-35f, 0.3f).SetEase(Ease.InOutBack).SetDelay(0.25f);
        }
        if (!startGameButton.activeInHierarchy)
        {
            startGameButton.SetActive(true);
            startGameButton.GetComponent<RectTransform>().DOAnchorPosY(-440f, 0.3f).SetEase(Ease.InOutBack).SetDelay(0.25f);
        }
    }
}
