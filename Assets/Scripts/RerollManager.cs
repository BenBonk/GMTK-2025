using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RerollManager : MonoBehaviour
{
    public int startingRerollPrice;
    private int rerollPrice;
    [HideInInspector]public int rerollsThisShop;
    private int rerollPriceThisShop;
    public float rerollMultIncrease;
    
    [HideInInspector] private bool cantHoverOver;
    public RectTransform hoverPopup;
    public TMP_Text priceText;
    private Tween a;
    private Tween b;
    public bool canReroll;
    private ShopManager shopManager;
    private BoonManager boonManager;
    private SteamIntegration steamIntegration;
    //private HashSet<Sprite> rrBoonSprites;

    //public int rerollsPerShop = 1;
    [HideInInspector] public int paidRerollsPerShop;
    [HideInInspector] public int freeRerollsPerShop;

    private int rerollsThisGame;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //FOR TESTING
        //rrBoonSprites = new HashSet<Sprite>();
        rerollsThisGame = FBPP.GetInt("rerollsThisGame");
        boonManager = GameController.boonManager;
        rerollPrice = FBPP.GetInt("rerollPrice", startingRerollPrice);
        priceText.text = rerollPrice.ToString();
        shopManager = GameController.shopManager;
        steamIntegration = GameController.steamIntegration;
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
        if (!canReroll || rerollsThisShop >= paidRerollsPerShop+freeRerollsPerShop || ((GameController.player.playerCurrency < rerollPrice && !GameController.boonManager.ContainsBoon("BNPL")) || (GameController.boonManager.ContainsBoon("BNPL") && GameController.player.playerCurrency - rerollPrice <-150) && rerollsThisShop >= freeRerollsPerShop))
        {
            AudioManager.Instance.PlaySFX("no_point_mult");
            return;
        }
        AudioManager.Instance.PlaySFX("reroll");
        AudioManager.Instance.PlaySFX("coins");
        //first paid reroll
        if (rerollsThisShop == freeRerollsPerShop)
        {
            rerollPrice = Mathf.RoundToInt(rerollPrice * rerollMultIncrease);
        }

        if (rerollsThisShop >= freeRerollsPerShop)
        {
            GameController.player.playerCurrency -= rerollPriceThisShop;
        }
        rerollsThisShop++;
        shopManager.InitializeAllUpgrades();
        shopManager.UpdateCashText();
        int totalRerolls = FBPP.GetInt("totalRerolls") + 1;
        FBPP.SetInt("rerollPrice", rerollPrice);
        FBPP.SetInt("totalRerolls", totalRerolls);
        rerollsThisGame++;
        if (rerollsThisGame > FBPP.GetInt("mostRerollsInGame"))
        {
            FBPP.SetInt("mostRerollsInGame", rerollsThisGame);
        }
        if (freeRerollsPerShop+paidRerollsPerShop<=rerollsThisShop)
        {
            transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
            canReroll = false;
        }
        else if (rerollsThisShop < freeRerollsPerShop)
        {
            priceText.text = 0.ToString();
        }
        else
        {
            priceText.text = rerollPriceThisShop.ToString();
        }
        if (totalRerolls == 100 && !steamIntegration.IsThisAchievementUnlocked("Shopaholic"))
        {
            steamIntegration.UnlockAchievement("Shopaholic");
        }
        
    }

    public void Reset()
    {
        paidRerollsPerShop = 1;
        freeRerollsPerShop = 0;
        rerollsThisShop = 0;
        rerollPriceThisShop = rerollPrice;
        //rrBoonSprites.Clear();
        if (boonManager.ContainsBoon("FreshStock"))
        {
            paidRerollsPerShop += 2;
            //rrBoonSprites.Add(boonManager.boonDict["FreshStock"].art);
        }
        if (GameController.gameManager.farmerID == 2)
        {
            freeRerollsPerShop += 1;
            paidRerollsPerShop += 1;
        }
        if (boonManager.ContainsBoon("Freeroll"))
        {
            freeRerollsPerShop += 1;
        }

        if (freeRerollsPerShop > 0)
        {
            priceText.text = 0.ToString();
        }
        else
        {
            priceText.text = rerollPrice.ToString();
        }

        transform.DOScale(new Vector3(2.2f, 2.2f, 1), 0);
        canReroll = true;
    }
}
