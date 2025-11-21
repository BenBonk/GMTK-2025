using System;
using Steamworks;
using UnityEngine;

public class SteamIntegration : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        try
        {
            SteamClient.Init(3974620);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    
    public bool IsThisAchievementUnlocked(string id)
    {
        //return new Steamworks.Data.Achievement(id).State;
        return true;
    }
    
    public void UnlockAchievement(string id)
    {
        //var ach = new Steamworks.Data.Achievement(id);
        //ach.Trigger();
    }
    
    public void ClearAchievementStatus(string id)
    {
        var ach = new Steamworks.Data.Achievement(id);
        ach.Clear();
    }
    
    void Update()
    {
        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }
}
