using DG.Tweening;
using System;
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

    private void Start()
    {
        InvokeRepeating("SpawnAnimal", 1,Random.Range(0.8f, 1.5f));
        AudioManager.Instance.PlayMusicWithFadeOutOld("main_theme", 2f,true);
        if (GameController.saveManager.PlayerHasSave())
        {
            hasSaveData.SetActive(true);
        }
        else
        {
            noSaveData.SetActive(true);
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
        hasSaveData.GetComponent<RectTransform>().DOAnchorPosX(-2000, 0.5f).SetEase(Ease.InOutBack);
        noSaveData.GetComponent<RectTransform>().DOAnchorPosX(-2000, 0.5f).SetEase(Ease.InOutBack);
        title.GetComponent<RectTransform>().DOAnchorPosX(-2000, 0.5f).SetEase(Ease.InOutBack);
        farmerSelect.GetComponent<RectTransform>().DOAnchorPosX(210, 0.5f).SetEase(Ease.OutQuad).SetDelay(0.5f);
        GameController.gameManager.roundCompleted = true;
        CancelInvoke("SpawnAnimal");
    }

}
