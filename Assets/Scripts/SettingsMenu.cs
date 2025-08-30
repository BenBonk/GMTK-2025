using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fullScreenModeDropdown;
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown framerateDropdown;
    public Toggle vsyncToggle;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    
    [HideInInspector]public int resolutionValue;
    [HideInInspector]public int fullscreenModeValue;
    [HideInInspector]public float musicValue;
    [HideInInspector]public float sfxValue;
    [HideInInspector]public int languageValue;
    [HideInInspector]public bool vsyncValue;
    [HideInInspector]public int framerateCapValue;
    
    private Resolution[] resolutions;
    private Resolution[] uniqueResolutions;
    public int[] framerateOptions;

    public RectTransform panel;
    public bool canOpenClose;
    public bool isOpen;
    private PauseMenu pauseMenu;
    private AudioManager audioManager;
    public LocalizedString[] localFullscreenModes;
    public LocalizedString localUnlimited;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioManager = AudioManager.Instance;
        pauseMenu = GameController.pauseMenu;
        resolutions = Screen.resolutions;
        uniqueResolutions = resolutions
            .GroupBy(res => new { res.width, res.height })
            .Select(group => group.OrderByDescending(res => res.refreshRate).First())
            .ToArray();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < uniqueResolutions.Length; i++)
        {
            string option = uniqueResolutions[i].width + "x" + uniqueResolutions[i].height;
            options.Add(option);
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
        InitializeSettings();
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
        InitializeSettings();
        panel.gameObject.SetActive(true);
        panel.DOAnchorPosX(0, .5f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(()=> canOpenClose = true);
    }
    public void Close()
    {
        canOpenClose = false;
        isOpen = false;
        SaveSettings();
        panel.DOAnchorPosX(-1950, 0.5f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(()=> DoneClose());
    }

    void DoneClose()
    {
        panel.gameObject.SetActive(false);
        canOpenClose = true;
        pauseMenu.canOpenClose = true;
    }

    public void SaveSettings()
    {
        FBPP.SetInt("resolutionValue", resolutionValue);
        FBPP.SetInt("fullscreenModeValue", fullscreenModeValue);
        FBPP.SetFloat("sfxValue", sfxValue);
        FBPP.SetFloat("musicValue", musicValue);
        FBPP.SetInt("languageValue",languageValue);
        FBPP.SetBool("vsyncValue",vsyncValue);
        FBPP.SetInt("framerateCapValue",framerateCapValue);
        FBPP.Save();
    }

    private void InitializeSettings()
    {
        framerateDropdown.options[0].text = localUnlimited.GetLocalizedString();
        for (int i = 0; i < fullScreenModeDropdown.options.Count; i++)
        {
            fullScreenModeDropdown.options[i].text = localFullscreenModes[i].GetLocalizedString();
        }
        resolutionValue = FBPP.GetInt("resolutionValue");
        fullscreenModeValue = FBPP.GetInt("fullscreenModeValue", 0);
        sfxValue = FBPP.GetFloat("sfxValue", 0);
        musicValue = FBPP.GetFloat("musicValue", 0);
        languageValue = FBPP.GetInt("languageValue");
        vsyncValue = FBPP.GetBool("vsyncValue", true);
        framerateCapValue = FBPP.GetInt("framerateCapValue");
        
        
        SetSFX(sfxValue);
        SetMusic(musicValue);
        SetVsync(vsyncValue);
        SetLanguage(languageValue);
        SetFramerateCap(framerateCapValue);
        StartCoroutine(WaitSetResolution());
        
        
        musicValueText.text = ""+musicValue.ToString("F2");
        sfxValueText.text = ""+sfxValue.ToString("F2");
        
        resolutionDropdown.value = resolutionValue;
        resolutionDropdown.RefreshShownValue();
        fullScreenModeDropdown.value = fullscreenModeValue;
        fullScreenModeDropdown.RefreshShownValue();
        languageDropdown.value = languageValue;
        languageDropdown.RefreshShownValue();
        framerateDropdown.value = framerateCapValue;
        framerateDropdown.RefreshShownValue();
        vsyncToggle.isOn = vsyncValue;
    }
    IEnumerator WaitSetResolution()
    {
        SetFullscreenMode(fullscreenModeValue);
        yield return new WaitForEndOfFrame(); //needed because of how unity handles fullscreenMode
        SetResolution(resolutionValue);
    }
    public void SetMusic(float value)
    {
        audioManager.SetMusicVolume(value);
        musicValue = value;
        musicValueText.text = ""+musicValue.ToString("F2");
    }
    private int lastStep = -1;
    public void SetSFX(float value)
    {
        audioManager.SetSFXVolume(value);
        sfxValue = value;
        sfxValueText.text = ""+sfxValue.ToString("F2");
        
        int step = Mathf.RoundToInt(value / 0.1f);
        if (step != lastStep)
        {
            audioManager.PlaySFX("ui_hover");
            lastStep = step;
        }
    }
    public void SetResolution(int resolutionIndex)
    {
        //Screen.SetResolution(1280, 540, false);
        Screen.SetResolution(uniqueResolutions[resolutionIndex].width, uniqueResolutions[resolutionIndex].height, Screen.fullScreenMode);
        resolutionValue = resolutionIndex;
        UpdateCanvasScaling(resolutionIndex);
    }
    public void SetFullscreenMode(int value)
    {
        if (value == 0)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else if (value == 1)
            Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed;
        fullscreenModeValue = value;
        
    }
    public void SetVsync(bool isOn)
    {
        if (isOn)
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }
        vsyncValue = isOn;
    }
    public void SetFramerateCap(int index)
    {
        Application.targetFrameRate = framerateOptions[index];
        framerateCapValue = index;
    }
    public void SetLanguage(int index)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        languageValue = index;
    }

    public void UpdateCanvasScaling(int resolutionIndex)
    {
        if ((float)uniqueResolutions[resolutionIndex].width/uniqueResolutions[resolutionIndex].height > (float)16/9) //aspect ratio is greater than 16:9
        {
            foreach (var canvasScaler in FindObjectsOfType<CanvasScaler>())
            {
                canvasScaler.matchWidthOrHeight = 1;
            }
        }
        else
        {
            foreach (var canvasScaler in FindObjectsOfType<CanvasScaler>())
            {
                canvasScaler.matchWidthOrHeight = 0;
            }
        }
    }
}
