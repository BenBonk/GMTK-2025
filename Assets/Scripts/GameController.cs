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
    [SerializeField] SaveManager localSaveManager;
    [SerializeField] PauseMenu localPauseMenu;
    [SerializeField] Logbook localLogbook;
    [SerializeField] RerollManager localRerollManager;
    [SerializeField] FarmerSelectManager localFarmerSelectManager;
    [SerializeField] BoonManager localBoonManager;
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
    public static SaveManager saveManager
    {
        get { return instance.localSaveManager; }
        set { instance.localSaveManager = value; }
    }
    public static PauseMenu pauseMenu
    {
        get { return instance.localPauseMenu; }
        set { instance.localPauseMenu = value; }
    }
    public static Logbook logbook
    {
        get { return instance.localLogbook; }
        set { instance.localLogbook = value; }
    }
    public static RerollManager rerollManager
    {
        get { return instance.localRerollManager; }
        set { instance.localRerollManager = value; }
    }
    public static FarmerSelectManager farmerSelectManager
    {
        get { return instance.localFarmerSelectManager; }
        set { instance.localFarmerSelectManager = value; }
    }
    public static BoonManager boonManager
    {
        get { return instance.localBoonManager; }
        set { instance.localBoonManager = value; }
    }
}
