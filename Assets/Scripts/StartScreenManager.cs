using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class StartScreenManager : MonoBehaviour
{
    public GameObject[] animalsToSpawn;
    public Transform[] spawnPositions;

    private void Start()
    {
        InvokeRepeating("SpawnAnimal", 1,Random.Range(1.5f, 2.5f));
    }

    void SpawnAnimal()
    {
        Instantiate(animalsToSpawn[Random.Range(0, animalsToSpawn.Length)], spawnPositions[Random.Range(0, spawnPositions.Length)].position, Quaternion.identity);
    }
}
