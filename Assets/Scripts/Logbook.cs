using UnityEngine;
using UnityEngine.UI;

public class Logbook : MonoBehaviour
{
    public Image backgroundArt;
    public Sprite[] bgSprites;
    public GameObject[] contents;

    public void SwitchTab(int index)
    {
        backgroundArt.sprite = bgSprites[index];
        foreach (var c in contents)
        {
            c.SetActive(false);
        }
        contents[index].SetActive(true);
    }
}
