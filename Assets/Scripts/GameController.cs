using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    [SerializeField] Player localPlayer;
    [SerializeField] ShopManager localShopManager;
    [SerializeField] GameManager localGameManager;
    [SerializeField] CaptureManager localCaptureManager;
    [SerializeField] AnimalLevelManager localAnimalLevelManager;
    [SerializeField] PredatorSelect localPredatorSelect;
    [SerializeField] DescriptionManager localDescriptionManager;
    [SerializeField] LocalizationManager localLocalizationManager;
    [SerializeField] TMP_FontAsset localDefaultFont;
    [SerializeField] TMP_FontAsset localLocalizedFont;
    void Awake()
    {
        instance = this;
    }

    public static Player player
    {
        get { return instance.localPlayer; }
        set { instance.localPlayer = value; }
    }
    public static ShopManager shopManager
    {
        get { return instance.localShopManager; }
        set { instance.localShopManager = value; }
    }
    public static GameManager gameManager
    {
        get { return instance.localGameManager; }
        set { instance.localGameManager = value; }
    }
    public static CaptureManager captureManager
    {
        get { return instance.localCaptureManager; }
        set { instance.localCaptureManager = value; }
    }
    public static AnimalLevelManager animalLevelManager
    {
        get { return instance.localAnimalLevelManager; }
        set { instance.localAnimalLevelManager = value; }
    }
    public static PredatorSelect predatorSelect
    {
        get { return instance.localPredatorSelect; }
        set { instance.localPredatorSelect = value; }
    }
    public static DescriptionManager descriptionManager
    {
        get { return instance.localDescriptionManager; }
        set { instance.localDescriptionManager = value; }
    }
    public static LocalizationManager localizationManager
    {
        get { return instance.localLocalizationManager; }
        set { instance.localLocalizationManager = value; }
    }
    public static TMP_FontAsset defaultFont
    {
        get { return instance.localDefaultFont; }
        set { instance.localDefaultFont = value; }
    }
    public static TMP_FontAsset localizedFont
    {
        get { return instance.localLocalizedFont; }
        set { instance.localLocalizedFont = value; }
    }
}
