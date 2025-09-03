using DG.Tweening;
using TMPro;
using UnityEngine;

public class RerollManager : MonoBehaviour
{
    public int startingRerollPrice;
    private int rerollPrice;
    public float rerollMultIncrease;
    
    [HideInInspector] private bool cantHoverOver;
    public RectTransform hoverPopup;
    public TMP_Text priceText;
    private Tween a;
    private Tween b;
    public bool canReroll;
    private ShopManager shopManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //FOR TESTING
        FBPP.SetInt("rerollPrice", 50);
        rerollPrice = FBPP.GetInt("rerollPrice", startingRerollPrice);
        priceText.text = rerollPrice.ToString();
        shopManager = GameController.shopManager;
    }

    public void HoverOver()
    {
        if (cantHoverOver || shopManager.cantPurchaseItem)
        {
            return;
        }
        a = hoverPopup.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);   
        b.Kill();
    }

    public void HoverExit()
    {
        b = hoverPopup.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InOutQuad);   
        a.Kill();
    }
    public void Reroll()
    {
        if (!canReroll)
        {
            return;
        }
        shopManager.InitializeAllUpgrades();

        transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
        canReroll = false;
        GameController.player.playerCurrency -= rerollPrice;
        shopManager.UpdateCashText();
        rerollPrice = Mathf.RoundToInt(rerollPrice * rerollMultIncrease);
        FBPP.SetInt("rerollPrice", rerollPrice);
        
    }

    public void Reset()
    {
        transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0);
        canReroll = true;
        priceText.text = rerollPrice.ToString();
    }
}
