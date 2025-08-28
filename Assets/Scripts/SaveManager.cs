using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class SaveManager : MonoBehaviour
{
    public AnimalData[] animalDatas;
    public Synergy[] synergyDatas;
    public Player player;
    public GameManager gameManager;
    void Awake()
    {
           var config = new FBPPConfig()
           {
               SaveFileName = "donotopenthislol.txt",
               AutoSaveData = false,
               ScrambleSaveData = true,
               EncryptionSecret = "ifyouseethisyouareahacker",
               SaveFilePath = Application.persistentDataPath
   
           };
           FBPP.Start(config);
    }

    //Not sure if we want this for production
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
        ResetVars();
        FBPP.SetBool("playerHasSave", false);
        FBPP.Save();
    }

    void ResetVars()
    {
        FBPP.SetInt("cash", 0);
        FBPP.SetInt("round", 0);
        FBPP.SetString("animalsInDeck", "");
        FBPP.SetString("synergiesInDeck", "");
        GameController.animalLevelManager.ResetLevels();
    }

    public void SaveGameData()
    {
        FBPP.SetInt("cash", (int)GameController.player.playerCurrency);
        FBPP.SetInt("round", GameController.gameManager.roundNumber);
        FBPP.SetString("animalsInDeck", GetSOList(player.animalsInDeck));
        FBPP.SetString("synergiesInDeck", GetSOList(player.synergiesInDeck));
        FBPP.Save();
    }

    public void LoadGameData()
    {
        player.playerCurrency =  FBPP.GetInt("cash");
        gameManager.roundNumber = FBPP.GetInt("round");
        if (FBPP.GetString("animalsInDeck")=="")
        {
            return;
        }
        Debug.Log(FBPP.GetString("animalsInDeck"));
        player.animalsInDeck.Clear();
        player.synergiesInDeck.Clear();
        foreach (var animal in FBPP.GetString("animalsInDeck").Split(","))
        {
            var match = animalDatas.FirstOrDefault(a => a.name == animal);
            if (match != null)
            {
                player.animalsInDeck.Add(match);
                Debug.Log("Loaded: " + animal);
            }
        }
        foreach (var synergy in FBPP.GetString("synergiesInDeck").Split(","))
        {
            var match = synergyDatas.FirstOrDefault(a => a.name == synergy);
            if (match != null)
            {
                player.synergiesInDeck.Add(match);
                Debug.Log("Loaded: " + synergy);
            }
        }
    }
    string GetSOList<T>(List<T> list) where T : ScriptableObject =>
        string.Join(",", list.Select(o => o.name));
}
