using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Random = UnityEngine.Random;

public class ChallengeEventManager : MonoBehaviour
{
    public BoxCollider2D cactusBounds;
    public GameObject[] cacti;
    private int numCacti;
    private float minBeeSpawnTime;
    private float maxBeeSpawnTime;
    private float minTumbleweedSpawnTime;
    private float maxTumbleweedSpawnTime;
    public GameObject bee;
    public GameObject tumbleweed;
    public GameObject beeRight;
    public GameObject tumbleweedRight;
    public GameObject wind;

    public int lastEvent = 67;
    public LocalizedString[] challengeEventStrings;
    GameManager gameManager;

    private List<Vector2> placedPositions = new List<Vector2>();
    private DifficultySetting intensityLevel;
    private bool spawnFromRight;
    private string tailwindModifer="tailwind";
    [HideInInspector]public int nightPredatorIncrease;
    private void Start()
    {
        gameManager = GameController.gameManager;
    }

    public void SetDifficulty(DifficultySetting difficultySetting)
    {
        intensityLevel = difficultySetting;
        switch (difficultySetting)
        {
            case DifficultySetting.Novice:
                SetDifficulty(
                    cactii: 2,
                    minBee: 3f, maxBee: 7f,
                    minTumble: 2.5f, maxTumble: 4.5f,
                    tailwind: "tailwind",
                    particleSpeedMin: 5, particleSpeedMax: 11,
                    particleRate: 15,
                    nightPredatorInc: 4
                );
                break;

            case DifficultySetting.Veteran:
                SetDifficulty(
                    cactii: 3,
                    minBee: 2f, maxBee: 5.5f,
                    minTumble: 2f, maxTumble: 3.5f,
                    tailwind: "tailwind2",
                    particleSpeedMin: 7, particleSpeedMax: 14,
                    particleRate: 19,
                    nightPredatorInc: 3
                );
                break;

            case DifficultySetting.Expert:
                SetDifficulty(
                    cactii: 4,
                    minBee: 1.75f, maxBee: 4f,
                    minTumble: 1.5f, maxTumble: 2.5f,
                    tailwind: "tailwind3",
                    particleSpeedMin: 9, particleSpeedMax: 16,
                    particleRate: 23,
                    nightPredatorInc: 2
                );
                spawnFromRight = true;
                break;
        }
    }

    void SetDifficulty(int cactii, float minBee, float maxBee, float minTumble, float maxTumble, string tailwind, float particleSpeedMin, float particleSpeedMax, float particleRate, int nightPredatorInc)
    {
        numCacti = cactii;
        minBeeSpawnTime = minBee;
        maxBeeSpawnTime = maxBee;
        minTumbleweedSpawnTime = minTumble;
        maxTumbleweedSpawnTime = maxTumble;
        tailwindModifer = tailwind;
        nightPredatorIncrease = nightPredatorInc;
        
        var ps = wind.transform.GetChild(0).GetComponent<ParticleSystem>();

        var main = ps.main;
        var speed = main.startSpeed;
        speed.mode = ParticleSystemCurveMode.TwoConstants;
        speed.constantMin = particleSpeedMin;
        speed.constantMax = particleSpeedMax;
        main.startSpeed = speed;

        var emission = ps.emission;
        emission.rateOverTime = particleRate;

    }

    public int GetChallengeEvent()
    {
        int chosenEvent = Random.Range(0, 5);
        while (chosenEvent == lastEvent)
        {
            chosenEvent = Random.Range(0, 5);
        }
        lastEvent = chosenEvent;

        if (chosenEvent == 0)
        {
            //tailwind
        }
        else if (chosenEvent == 1)
        {
            //tumbleweed
            //Invoke("SpawnTumbleweed", 0.5f);
        }
        else if (chosenEvent == 2)
        {
            //cacti
            //Invoke("SpawnCacti", 0.5f);
        }
        else if (chosenEvent == 3)
        {
            //bees
            //Invoke("SpawnBee", 0.5f);
        }
        else
        {
            //night

        }
        return chosenEvent; //chosen event

    }

    public void StartChallenge(int eventID)
    {
        if (eventID == -1)
        {
            return;
        }
        if (eventID == 0)
        {
            //tailwind
            wind.SetActive(true);
            Animal.EnableSpeedModifier("tailwind", 1.5f);
            AudioManager.Instance.PlayAmbientWithFadeOutOld("wind_ambient");
        }
        else if (eventID == 1)
        {
            //tumbleweed
            Invoke("SpawnTumbleweed", 7f);
        }
        else if (eventID == 2)
        {
            //cacti
            SpawnCacti();
        }
        else if (eventID == 3)
        {
            //bees
            Invoke("SpawnBee", 7f);
        }
        else
        {
            //night
            GameController.postProcessingManager.NightModeOn();
            AudioManager.Instance.PlayAmbientWithFadeOutOld("night_ambient");
        }
    }

    public void SpawnCacti()
    {
        int attempts = 0;
        int placed = 0;
        int maxAttempts = numCacti * 10;
        int numberOfCacti = numCacti;
        placedPositions.Clear();

        while (placed < numberOfCacti && attempts < maxAttempts)
        {
            if (TrySpawnCactus())
                placed++;

            attempts++;
        }
    }

    bool TrySpawnCactus()
    {
        Bounds bounds = cactusBounds.bounds;
        Vector2 candidate = new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );
        foreach (var pos in placedPositions)
        {
            if (Vector2.Distance(pos, candidate) < 6)
                return false;
        }
        Instantiate(cacti[Random.Range(0, cacti.Length)], candidate, Quaternion.identity);
        placedPositions.Add(candidate);
        return true;
    }

    void SpawnBee()
    {
        float topBuffer = 0.25f;
        float bottomBuffer = 0.25f;

        // Get vertical bounds of the camera in world space
        float z = Mathf.Abs(Camera.main.transform.position.z - bee.transform.position.z);
        Vector3 screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float minY = screenBottom.y + bottomBuffer;
        float maxY = screenTop.y - topBuffer;

        //  Choose a random Y position safely within bounds
        float randomY = Random.Range(minY, maxY);

        //  Set spawn position at the right edge
        if (spawnFromRight && Random.Range(0, 2) == 0)
        {
            float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;
            Instantiate(beeRight, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);
        }
        else
        {
            float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
            Instantiate(bee, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);    
        }
        
        if (!gameManager.roundCompleted && gameManager.roundDuration > 0)
        {
            Invoke("SpawnBee", Random.Range(minBeeSpawnTime, maxBeeSpawnTime));
        }
    }

    void SpawnTumbleweed()
    {
        float topBuffer = 0.25f;
        float bottomBuffer = 0.25f;

        // Get vertical bounds of the camera in world space
        float z = Mathf.Abs(Camera.main.transform.position.z - tumbleweed.transform.position.z);
        Vector3 screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float minY = screenBottom.y + bottomBuffer;
        float maxY = screenTop.y - topBuffer;

        //  Choose a random Y position safely within bounds
        float randomY = Random.Range(minY, maxY);

        //  Set spawn position at the right edge
        if (spawnFromRight && Random.Range(0,2)==0)
        {
            float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;
            Instantiate(tumbleweedRight, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);
        }
        else
        {
            float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
            Instantiate(tumbleweed, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);    
        }
        
        if (!gameManager.roundCompleted && gameManager.roundDuration > 0)
        {
            Invoke("SpawnTumbleweed", Random.Range(minTumbleweedSpawnTime, maxTumbleweedSpawnTime));
        }
    }

    public void EndChallenge()
    {
        GameController.challengeEventManager.wind.SetActive(false);
        Animal.DisableSpeedModifier(tailwindModifer);
        foreach (var cact in GameObject.FindGameObjectsWithTag("Cactus"))
        {
            Destroy(cact);
        }
    }
}
