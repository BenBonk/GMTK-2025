using System;
using TMPro;
using UnityEditorInternal.VR;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeReward : MonoBehaviour
{
    public enum ChallengeRewardType
    {
        SpawnRate,
        BoonSlot,
        DeckRemoval
    }
    public ChallengeRewardType rewardType;
    [HideInInspector] public bool cantSelect;
    public void SelectReward()
    {
        if (cantSelect)
        {
            return;
        }

        cantSelect = true;

        if (rewardType == ChallengeRewardType.BoonSlot)
        {
            //add boon slot
            GameController.challengeRewardSelect.StartCoroutine("Exit");
        }
        else if (rewardType == ChallengeRewardType.SpawnRate)
        {
            //change spawn rate
            FBPP.SetFloat("spawnRate", FBPP.GetFloat("spawnRate",1f) *0.75f);
            GameController.challengeRewardSelect.StartCoroutine("Exit");
        }
        else if (rewardType == ChallengeRewardType.DeckRemoval)
        {
            //open deck removal UI
            GameController.challengeRewardSelect.StartCoroutine("OpenDeckRemoval");
        }
    }
}
