using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // ==== FBPP bootstrap (runs before first scene) ====
    private static bool _fbppStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapFBPP()
    {
        StartFBPPIfNeeded();
    }

    private static void StartFBPPIfNeeded()
    {
        if (_fbppStarted) return;

        var config = new FBPPConfig
        {
            SaveFileName = "donotopenthislol.txt",
            AutoSaveData = false,
            ScrambleSaveData = true,
            EncryptionSecret = "ifyouseethisyouareahacker",
            SaveFilePath = Application.persistentDataPath
        };

        FBPP.Start(config);
        _fbppStarted = true;
    }
    // ================================================

    public AnimalData[] animalDatas;
    public Boon[] boonDatas;
    public HarvestData[] harvestDatas;
    public FarmerData[] farmerDatas;
    public Player player;
    public GameManager gameManager;

    void Awake()
    {
        // Safety net in case something tries to touch FBPP very early (e.g., in Editor reload)
        StartFBPPIfNeeded();
    }

    // Not sure if we want this for production
    private void OnApplicationQuit()
    {
        FBPP.Save();
    }

    public bool PlayerHasSave()
    {
        return FBPP.GetBool("playerHasSave", false);
    }

    public void NewGame()
    {
        ResetVars();
        FBPP.SetBool("playerHasSave", true);
        FBPP.Save();
    }
    public void ClearGame()
    {
        Debug.Log("clear game called");
        ResetVars();
        FBPP.SetBool("playerHasSave", false);
        FBPP.Save();
    }

    void ResetVars()
    {
        FBPP.SetString("cash", "0");
        FBPP.SetInt("round", 1);
        FBPP.SetString("animalsInDeck", "");
        FBPP.SetString("boonsInDeck", "");
        FBPP.DeleteInt("rerollPrice");
        FBPP.SetInt("rerollsThisGame", 0);
        FBPP.SetFloat("spawnRate", 1f);
        FBPP.SetInt("AnimalPurchasedThisGame", 0);
        FBPP.SetInt("boonDeckSize", 5);
        GameController.animalLevelManager.ResetLevels();
    }

    public void InitializeSaveData(int harvestLevel, int farmerIndex)
    {
        ResetVars();
        List<AnimalData> startingAnimals = new List<AnimalData>();
        FBPP.SetString("animalsInDeck", GetSOList(startingAnimals));
        FBPP.SetInt("harvestLevel", harvestLevel);
        FBPP.SetInt("farmerID", farmerIndex);
        GameController.animalLevelManager.ResetLevels();
        FBPP.SetBool("playerHasSave", true);
        FBPP.Save();
    }

    public void SaveGameData()
    {
        FBPP.SetString("cash", GameController.player.playerCurrency.ToString(System.Globalization.CultureInfo.InvariantCulture));
        FBPP.SetInt("round", GameController.gameManager.roundNumber);
        FBPP.SetString("animalsInDeck", GetSOList(player.animalsInDeck));
        FBPP.SetString("boonsInDeck", GetSOList(player.boonsInDeck));
        FBPP.SetInt("harvestLevel", GameController.gameManager.harvestLevel);
        FBPP.SetBool("playerHasSave", true);
        FBPP.Save();
    }

    public void LoadGameData()
    {
        player.playerCurrency =  double.Parse(FBPP.GetString("cash"), System.Globalization.CultureInfo.InvariantCulture);
        gameManager.roundNumber = FBPP.GetInt("round");
        gameManager.harvestLevel = FBPP.GetInt("harvestLevel", 1);
        gameManager.farmerID = FBPP.GetInt("farmerID", 0);
        gameManager.foxThiefStolenStats = gameManager.animalShopItem.possibleAnimals[FBPP.GetInt("chosenToStealIndex", 0)].animalData;
        if (FBPP.GetString("animalsInDeck")=="")
        {
            List<AnimalData> startingAnimals = new List<AnimalData>();
            FBPP.SetString("animalsInDeck", GetSOList(startingAnimals));
        }
        //Debug.Log(FBPP.GetString("animalsInDeck"));
        player.animalsInDeck.Clear();
        player.boonsInDeck.Clear();
        foreach (var animal in FBPP.GetString("animalsInDeck").Split(","))
        {
            var match = animalDatas.FirstOrDefault(a => a.name == animal);
            if (match != null)
            {
                player.animalsInDeck.Add(match);
                Debug.Log("Loaded: " + animal);
            }
        }
        foreach (var booon in FBPP.GetString("boonsInDeck").Split(","))
        {
            var match = boonDatas.FirstOrDefault(a => a.name == booon);
            if (match != null)
            {
                player.AddBoonToDeck(match);
                //Debug.Log("Loaded: " + synergy);
            }
        }
    }
    string GetSOList<T>(List<T> list) where T : ScriptableObject =>
        string.Join(",", list.Select(o => o.name));
}
