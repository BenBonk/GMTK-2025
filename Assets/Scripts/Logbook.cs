using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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

    [Header("Animals")]
    public LocalizedString animalStat1;
    public LocalizedString animalStat2;
    public LocalizedString animalStat3;
    public LocalizedString animalStat4;
    public LocalizedString animalStat5;
    public TMP_Text[] animalStatTexts;
    [Header("Boons")]
    public LocalizedString boonsStat1;
    public LocalizedString boonsStat2;
    public LocalizedString boonsStat3;
    public LocalizedString boonsStat4;
    public TMP_Text[] boonStatTexts;
    [Header("Economy")]
    public LocalizedString econStat1;
    public LocalizedString econStat2;
    public LocalizedString econStat3;
    public LocalizedString econStat4;
    public TMP_Text[] econStatTexts;
    [Header("Records")]
    public LocalizedString recordsStat1;
    public LocalizedString recordsStat2;
    public LocalizedString recordsStat3;
    public TMP_Text[] recordsStatTexts;
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
        UpdateLogbook();
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

    public void UpdateLogbook()
    {
        //animalStatTexts[0].text = animalStat1.GetLocalizedString() + FBPP.GetInt()
    }
}
