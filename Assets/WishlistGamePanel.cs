using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WishlistGamePanel : MonoBehaviour
{
    private PauseMenu pauseMenu;
    public Image darkCover;
    public RectTransform panel;
    public bool canOpenClose;
    public bool isOpen;

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
        darkCover.DOFade(.5f, 0.5f);
        panel.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(()=> canOpenClose = true);
    }
    public void Close()
    {
        canOpenClose = false;
        isOpen = false;
        darkCover.DOFade(0f, 0.5f);
        panel.DOAnchorPosY(-1070, 0.5f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(()=> DoneClose());
    }

    void DoneClose()
    {
        panel.gameObject.SetActive(false);
        canOpenClose = true;
        pauseMenu.canOpenClose = true;
    }
    public void WishlistGame()
    {
        Application.OpenURL("https://store.steampowered.com/app/3974620/Wrangle_Ranch/");
    }
}
