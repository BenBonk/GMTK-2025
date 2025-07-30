using System.Text.RegularExpressions;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    [SerializeField] Player localPlayer;
    [SerializeField] ShopManager localShopManager;
    [SerializeField] GameManager localGameManager;
    [SerializeField] CaptureManager localCaptureManager;

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
}
