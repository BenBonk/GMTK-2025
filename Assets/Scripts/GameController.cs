using System.Text.RegularExpressions;
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
}
