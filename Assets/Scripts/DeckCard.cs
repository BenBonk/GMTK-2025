using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckCard : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text desc;
    public Image icon;
    public RectTransform hoverPopup;
    private bool isPopup;
    private Tween a;
    private Tween b;
    public Ease ease;
    public void Initialize(string titleStr, string descStr, Sprite iconSprite)
    {
        title.text = titleStr;
        desc.text = descStr;
        icon.sprite = iconSprite;
    }
    public void HoverOver()
    {
        a = hoverPopup.DOScale(Vector3.one, 0.25f).SetEase(ease);   
        b.Kill();
    }

    public void HoverOverExit()
    {
        b = hoverPopup.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InOutQuad);   
        a.Kill();
    }
}
