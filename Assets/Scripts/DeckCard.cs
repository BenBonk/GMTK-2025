using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckCard : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text desc;
    public TMP_Text desc2;
    public Image icon;
    public RectTransform hoverPopup;
    public GameObject subPopup;
    private bool isPopup;
    private Tween a;
    private Tween b;
    public bool bounch;
    public void Initialize(string titleStr, string descStr, Sprite iconSprite, string descStr2="")
    {
        title.text = titleStr;
        desc.text = descStr;
        icon.sprite = iconSprite;
        if (descStr2=="hidepopup")
        {
            subPopup.SetActive(false);
            hoverPopup.DOLocalMoveY(hoverPopup.position.y - 100,0);
        }
        if (descStr2!="")
        {
            desc2.text = descStr2;
        }
    }
    public void HoverOver()
    {
        if (bounch)
        {
            a = hoverPopup.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);      
        }
        else
        {
            a = hoverPopup.DOScale(Vector3.one, 0.15f).SetEase(Ease.InOutQuad);      
        }
        b.Kill();
    }

    public void HoverOverExit()
    {
        b = hoverPopup.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InOutQuad);   
        a.Kill();
    }
}
