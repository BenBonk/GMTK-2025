using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Logbook : MonoBehaviour
{
    public Image backgroundArt;
    public Sprite[] bgSprites;
    public GameObject[] contents;
    public RectTransform panel;
    public bool canOpenClose;
    public bool isOpen;
    private PauseMenu pauseMenu;

    private void Start()
    {
        pauseMenu = GameController.pauseMenu;
    }

    private void Update()
    {
        if (canOpenClose && isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close(); 
        }
    }
    
    public void Open()
    {
        canOpenClose = false;
        pauseMenu.canOpenClose = false;
        isOpen = true;
        panel.gameObject.SetActive(true);
        panel.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(()=> canOpenClose = true);
    }
    public void Close()
    {
        canOpenClose = false;
        isOpen = false;
        panel.DOAnchorPosY(-1070, 0.5f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(()=> DoneClose());
    }

    void DoneClose()
    {
        panel.gameObject.SetActive(false);
        canOpenClose = true;
        pauseMenu.canOpenClose = true;
    }
    
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
