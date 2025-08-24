using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class SaveManager : MonoBehaviour
{
    public AnimalData[] animalDatas;
    public Synergy[] synergyDatas;
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

    public bool PlayerHasSave()
    {
        return FBPP.GetBool("playerHasSave");
    }

    public void ClearSave()
    {
        
    }

    public void SaveGameData()
    {
        FBPP.SetInt("cash", (int)GameController.player.playerCurrency);
        FBPP.SetInt("round", GameController.gameManager.roundNumber);
        FBPP.SetString("animalsInDeck", GetSOList(GameController.player.animalsInDeck));
        FBPP.SetString("synergiesInDeck", GetSOList(GameController.player.synergiesInDeck));
        FBPP.Save();
    }

    public void LoadGameData()
    {
        GameController.player.playerCurrency = FBPP.GetInt("cash");
        GameController.gameManager.roundNumber = FBPP.GetInt("round");
        foreach (var animal in FBPP.GetString("animalsInDeck").Split(","))
        {
            var match = animalDatas.FirstOrDefault(a => a.name == animal);
            if (match != null)
            {
                GameController.player.animalsInDeck.Add(match);
                Debug.Log("Loaded: " + animal);
            }
        }
        foreach (var synergy in FBPP.GetString("animalsInDeck").Split(","))
        {
            var match = synergyDatas.FirstOrDefault(a => a.name == synergy);
            if (match != null)
            {
                GameController.player.synergiesInDeck.Add(match);
                Debug.Log("Loaded: " + synergy);
            }
        }
    }

    string GetSOList<T>(List<T> objects) where T : ScriptableObject
    {
        return objects == null || objects.Count == 0
            ? string.Empty
            : string.Join(", ", objects.Select(o => o.name));
    }
}
