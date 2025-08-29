using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fullSreenModeDropdown;
    public AudioMixer sfxMixer;
    public AudioMixer musicMixer;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    
    [HideInInspector]public int resolutionValue;
    [HideInInspector]public int fullscreenModeValue;
    [HideInInspector]public float musicValue;
    [HideInInspector]public float sfxValue;
    [HideInInspector]public int languageValue;
    
    private Resolution[] resolutions;
    private Resolution[] uniqueResolutions;
    
    public RectTransform panel;
    public bool canOpenClose;
    public bool isOpen;
    private PauseMenu pauseMenu;
    private SaveManager saveManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenu = GameController.pauseMenu;
        saveManager = GameController.saveManager;
        resolutions = Screen.resolutions;
        uniqueResolutions = resolutions
            .GroupBy(res => new { res.width, res.height })
            .Select(group => group.OrderByDescending(res => res.refreshRate).First())
            .ToArray();
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

    public void SaveSettings()
    {
        FBPP.SetInt("resolutionValue", resolutionValue);
        FBPP.SetInt("fullscreenModeValue", fullscreenModeValue);
        FBPP.SetFloat("sfxValue", sfxValue);
        FBPP.SetFloat("musicValue", musicValue);
        FBPP.SetInt("languageValue",languageValue);
        FBPP.Save();
    }

    private void InitializeSettings()
    {
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < uniqueResolutions.Length; i++)
        {
            string option = uniqueResolutions[i].width + "x" + uniqueResolutions[i].height;
            options.Add(option);
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
        resolutionValue = FBPP.GetInt("resolutionValue");
        fullscreenModeValue = FBPP.GetInt("fullscreenModeValue", 0);
        sfxValue = FBPP.GetFloat("sfxValue", 0);
        musicValue = FBPP.GetFloat("musicValue", 0);
        languageValue = FBPP.GetInt("languageValue");
        
        
        SetSFX(sfxValue);
        SetMusic(musicValue);
        StartCoroutine(WaitSetResolution());
        
        
        musicValueText.text = ""+musicValue; // musicValueText.text = ""+Mathf.RoundToInt((musicValue+20)*5);
        sfxValueText.text = ""+sfxValue;
        resolutionDropdown.value = resolutionValue;
        resolutionDropdown.RefreshShownValue();
        fullSreenModeDropdown.value = fullscreenModeValue;
        fullSreenModeDropdown.RefreshShownValue();
    }
    IEnumerator WaitSetResolution()
    {
        SetFullscreenMode(fullscreenModeValue);
        yield return new WaitForEndOfFrame(); //needed because of how unity handles fullscreenMode
        SetResolution(resolutionValue);
    }
    
    public void SetSFX(float value)
    {
        sfxMixer.SetFloat("vol", value);
        sfxValue = value;
        sfxValueText.text = ""+sfxValue;
    }
    public void SetMusic(float value)
    {
        musicMixer.SetFloat("vol", value-15);
        musicValue = value;
        musicValueText.text = ""+musicValue;
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
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        if (value == 0)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else if (value == 1)
            Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed;
        fullscreenModeValue = value;
        
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
