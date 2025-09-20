using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using static Unity.VisualScripting.Metadata;
using DG.Tweening;
using Random = UnityEngine.Random;
using TMPro;

public class StartScreenManager : MonoBehaviour
{
    public GameObject[] animalsToSpawn;
    public Transform[] spawnPositions;
    public GameObject hasSaveData;
    public GameObject noSaveData;
    public GameObject title;
    public GameObject farmerSelect;
    public GameObject wishlist;

    private IEnumerator Start()
    {
            
        InvokeRepeating("SpawnAnimal", 1,Random.Range(1f, 2f));
        AudioManager.Instance.PlayMusicWithFadeOutOld("main_theme", 2f,true);
        if (GameController.saveManager.PlayerHasSave())
        {
            hasSaveData.SetActive(true);
        }
        else
        {
            noSaveData.SetActive(true);
        }

        yield return new WaitForSeconds(5);
        if (!GameController.gameManager.roundCompleted)
        {
            GameController.wishlistPanel.Open();   
        }
    }

    void SpawnAnimal()
    {
        Instantiate(animalsToSpawn[Random.Range(0, animalsToSpawn.Length)], spawnPositions[Random.Range(0, spawnPositions.Length)].position, Quaternion.identity);
    }


    public void InitializeGameData(TMP_Text harvestLevelText)
    {
        GameController.saveManager.InitializeSaveData(int.Parse(harvestLevelText.text),GameController.farmerSelectManager.selectedFarmerIndex);
    }

    public void ChangeScene()
    {
        AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 2f, true);
    }

    public void LoadFarmers()
    {
        hasSaveData.GetComponent<RectTransform>().DOAnchorPosX(-2000, 1f).SetEase(Ease.InOutBack);
        noSaveData.GetComponent<RectTransform>().DOAnchorPosX(-2000, 1f).SetEase(Ease.InOutBack);
        title.GetComponent<RectTransform>().DOAnchorPosX(-2000, .75f).SetEase(Ease.InOutBack);
        wishlist.GetComponent<RectTransform>().DOAnchorPosX(-2000, .5f).SetEase(Ease.InOutBack);
        farmerSelect.GetComponent<RectTransform>().DOAnchorPosX(210, .6f).SetEase(Ease.OutBack).SetDelay(0.4f);
        GameController.gameManager.roundCompleted = true;
        CancelInvoke("SpawnAnimal");
    }
}
