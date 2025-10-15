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
    public void SelectPredator()
    {
        if (cantSelect)
        {
            return;
        }

        cantSelect = true;
        GameController.challengeRewardSelect.StartCoroutine("Exit");

        if (rewardType == ChallengeRewardType.BoonSlot)
        {
            //add boon slot
        }
        else if (rewardType == ChallengeRewardType.SpawnRate)
        {
            //change spawn rate
        }
        else if (rewardType == ChallengeRewardType.DeckRemoval)
        {
            //open deck removal UI
        }
    }
}
