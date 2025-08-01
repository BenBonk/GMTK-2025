using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckCard : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text desc;
    public Image icon;

    public void Initialize(string titleStr, string descStr, Sprite iconSprite)
    {
        title.text = titleStr;
        desc.text = descStr;
        icon.sprite = iconSprite;
    }
    
}
