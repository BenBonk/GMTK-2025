using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public RectTransform panel;
    public Image darkCover;
    public bool canOpenClose;
    public bool isOpen;
    
    private Tween animalDeckTween;
    private Tween boonDeckTween;
    public RectTransform deckPanel;
    public RectTransform synergiesPanel;
    public Transform boonDeckParent;
    public GameObject synergiesVisual;
    public RectTransform deckParent;
    private bool deckOpen;
    private bool synergiesOpen;
    private ShopManager shopManager;
    private Logbook logbook;
    public SettingsMenu settings;
    private LassoController lassoController;

    private void Start()
    {
        logbook = GameController.logbook;
        lassoController = GameController.gameManager.lassoController;
        shopManager = GameController.shopManager;
    }

    private void Update()
    {
        if (canOpenClose && !isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Open(); 
        }
        if (canOpenClose && isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close(); 
        }
    }

    public void Open()
    {
        canOpenClose = false;
        isOpen = true;
        Time.timeScale = 0;
        panel.gameObject.SetActive(true);
        panel.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack).SetUpdate(true);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, .5f).OnComplete(()=> canOpenClose = true).SetUpdate(true);
        deckPanel.DOAnchorPosX(-415, 0f).SetEase(Ease.InOutQuad).OnComplete(()=>deckPanel.gameObject.SetActive(false)).SetUpdate(true);
        boonDeckTween = synergiesPanel.DOAnchorPosX(415, 0f).SetEase(Ease.InOutQuad).OnComplete(() => synergiesVisual.SetActive(false)).SetUpdate(true);
        deckOpen = false;
        synergiesOpen = false;
        if (lassoController != null)
        {
            lassoController.DestroyLassoExit(true);
        }
        if (shopManager != null)
        {
            shopManager.UpdateDeck(deckParent);
            shopManager.UpdateSynergies(boonDeckParent);
        }
    }
    public void Close()
    {
        canOpenClose = false;
        isOpen = false;
        Time.timeScale = 1;
        panel.DOAnchorPosY(909, 0.5f).SetEase(Ease.InBack).SetUpdate(true);
        darkCover.DOFade(0f, 0.5f).OnComplete(()=> DoneClose()).SetUpdate(true);
    }

    void DoneClose()
    {
        panel.gameObject.SetActive(false);
        darkCover.enabled = false;
        canOpenClose = true;
    }

    public void Quit()
    {
        Application.Quit();
    }
    public void ToggleDeck()
    {
        if (settings.isOpen || logbook.isOpen)
        {
            return;
        }
        if (animalDeckTween!=null)
        {
            animalDeckTween.Kill();
        }
        deckOpen = !deckOpen;
        if (deckOpen)
        {
            deckPanel.gameObject.SetActive(true);
            animalDeckTween = deckPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            animalDeckTween = deckPanel.DOAnchorPosX(-415, .25f).SetEase(Ease.InOutQuad).OnComplete(()=>deckPanel.gameObject.SetActive(false)).SetUpdate(true);
        }
    }

    public void ToggleSynergies()
    {
        if (settings.isOpen || logbook.isOpen)
        {
            return;
        }
        if (boonDeckTween!=null)
        {
            boonDeckTween.Kill();
        }
        synergiesOpen = !synergiesOpen;
        if (synergiesOpen)
        {
            synergiesVisual.SetActive(true);
            boonDeckTween = synergiesPanel.DOAnchorPosX(0, .35f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            boonDeckTween = synergiesPanel.DOAnchorPosX(415, .25f).SetEase(Ease.InOutQuad).OnComplete(() => synergiesVisual.SetActive(false)).SetUpdate(true);
        }
    }
}
