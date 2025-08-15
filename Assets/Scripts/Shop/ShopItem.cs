using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ShopItem : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;
    public Image upgradeArt;
    public ButtonFX buttonFX;

    [HideInInspector]public double price;
    public bool canPurchase = true;
    [HideInInspector] private bool cantHoverOver;
    public RectTransform hoverPopup;
    private Tween a;
    private Tween b;
    protected ShopManager shopManager;

    private void Start()
    {
        shopManager = GameController.shopManager;
    }

    public virtual void Initialize()
    {
        
    }
    
    public virtual void PurchaseUpgrade()
    {
        
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
    
}
