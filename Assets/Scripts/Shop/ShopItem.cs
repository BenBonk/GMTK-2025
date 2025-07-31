using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ShopItem : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;
    public Image upgradeArt;
    
    [HideInInspector]public int price;
    public bool canPurchase = true;

    public virtual void Initialize()
    {
        
    }
    
    public virtual void PurchaseUpgrade()
    {
        
    }

    public void HoverOver()
    {
        
    }

    public void HoverExit()
    {
        
    }
    
}
