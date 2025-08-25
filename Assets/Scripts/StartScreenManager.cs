using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class StartScreenManager : MonoBehaviour
{
    public GameObject[] animalsToSpawn;
    public Transform[] spawnPositions;
    public GameObject hasSaveData;
    public GameObject noSaveData;

    private void Start()
    {
        InvokeRepeating("SpawnAnimal", 1,Random.Range(1.5f, 2.5f));
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

    public void ChangeScene()
    {
        AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 2f, true);
    }
}
