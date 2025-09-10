using System;
using System.Collections.Generic;
using System.Linq;
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
    private SaveManager saveManager;

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
        saveManager = GameController.saveManager;
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
        List<string> top3Animals = saveManager.animalDatas
            .OrderByDescending(a => FBPP.GetInt(a.name))  // sort by value
            .Take(3)                                       // take top 3
            .Select(a => a.name)                           // select the names
            .ToList();
        
        animalStatTexts[0].text = $"{animalStat1.GetLocalizedString()} {FBPP.GetInt("numberAnimalsWrangled")}";
        animalStatTexts[1].text = $"{animalStat2.GetLocalizedString()} {top3Animals[0]}, {top3Animals[1]}, {top3Animals[2]}";
        animalStatTexts[2].text = $"{animalStat3.GetLocalizedString()} {FBPP.GetInt("totalAnimalsPurchased")}";
        animalStatTexts[3].text = $"{animalStat4.GetLocalizedString()} {FBPP.GetInt("largestCapture")}";
        animalStatTexts[4].text = $"{animalStat5.GetLocalizedString()} {FBPP.GetFloat("highestPointsPerLasso")}";
        
        List<string> top3Boons = saveManager.boonDatas
            .OrderByDescending(a => FBPP.GetInt(a.name))  // sort by value
            .Take(3)                                       // take top 3
            .Select(a => a.synergyName.GetLocalizedString())                           // select the names
            .ToList();

        boonStatTexts[0].text = $"{boonsStat1.GetLocalizedString()} {top3Boons[0]}, {top3Boons[1]}, {top3Boons[2]}";
        boonStatTexts[1].text = $"{boonsStat2.GetLocalizedString()} {FBPP.GetInt("totalBoonsPurchased")}";
        boonStatTexts[2].text = $"{boonsStat3.GetLocalizedString()} {FBPP.GetInt("totalUpgradesPurchased")}";
        boonStatTexts[3].text = $"{boonsStat4.GetLocalizedString()} {FBPP.GetInt("highestAnimalLevel")}";
        
        econStatTexts[0].text = $"{econStat1.GetLocalizedString()} {FBPP.GetFloat("highestCashPerLasso")}";
        econStatTexts[1].text = $"{econStat2.GetLocalizedString()} {FBPP.GetFloat("highestCash")}";
        econStatTexts[2].text = $"{econStat3.GetLocalizedString()} TBD";
        econStatTexts[3].text = $"{econStat4.GetLocalizedString()} TBD";
        
        recordsStatTexts[0].text = $"{recordsStat1.GetLocalizedString()} {FBPP.GetInt("highestRound")}";
        recordsStatTexts[1].text = $"{recordsStat2.GetLocalizedString()} TBD";
        recordsStatTexts[2].text = $"{recordsStat3.GetLocalizedString()} {FBPP.GetInt("closeCalls")}";
    }
}
