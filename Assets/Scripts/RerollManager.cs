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
    private BoonManager boonManager;

    public int rerollsPerShop = 1;

    private int rerollsThisGame;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //FOR TESTING
        rerollsThisGame = FBPP.GetInt("rerollsThisGame");
        FBPP.SetInt("rerollPrice", 50);
        boonManager = GameController.boonManager;
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
        if (!canReroll || rerollsPerShop <= 0 || GameController.player.playerCurrency < rerollPrice)
        {
            return;
        }

        GameController.player.playerCurrency -= rerollPrice;
        rerollsPerShop--;
        if (rerollsPerShop <= 0)
        {
            rerollPrice = Mathf.RoundToInt(rerollPrice * rerollMultIncrease);
        }
        shopManager.InitializeAllUpgrades();
        shopManager.UpdateCashText();
        FBPP.SetInt("rerollPrice", rerollPrice);
        FBPP.SetInt("totalRerolls", FBPP.GetInt("totalRerolls")+1);
        rerollsThisGame++;
        if (rerollsThisGame > FBPP.GetInt("mostRerollsInGame"))
        {
            FBPP.SetInt("mostRerollsInGame", rerollsThisGame);
        }
        if (rerollsPerShop<=0)
        {
            transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
            canReroll = false;
        }
        else
        {
            priceText.text = rerollPrice.ToString();
        }
        
    }

    public void Reset()
    {
        rerollsPerShop = 1;
        if (boonManager.ContainsBoon("FreshStock"))
        {
            rerollsPerShop = 2;
        }
        transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0);
        canReroll = true;
        priceText.text = rerollPrice.ToString();
    }
}
