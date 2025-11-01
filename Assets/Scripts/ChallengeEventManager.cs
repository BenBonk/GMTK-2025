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
    public int minCacti = 2;
    public int maxCacti = 4;
    public GameObject bee;
    public GameObject tumbleweed;
    public GameObject wind;

    public int lastEvent = 67;
    public LocalizedString[] challengeEventStrings;
    GameManager gameManager;

    private List<Vector2> placedPositions = new List<Vector2>();
    private void Start()
    {
        gameManager = GameController.gameManager;
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
        return 4;
        return chosenEvent;

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
        }
    }

    public void SpawnCacti()
    {
        int attempts = 0;
        int placed = 0;
        int maxAttempts = maxCacti * 10;
        int numberOfCacti = Random.Range(minCacti, maxCacti + 1);
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
            if (Vector2.Distance(pos, candidate) < 4)
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
        float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
        Instantiate(bee, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);
        if (!gameManager.roundCompleted && gameManager.roundDuration > 0)
        {
            Invoke("SpawnBee", Random.Range(3.0f, 7.0f));
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
        float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
        Instantiate(tumbleweed, new Vector3(rightEdgeX, randomY, 0), Quaternion.identity);
        if (!gameManager.roundCompleted && gameManager.roundDuration > 0)
        {
            Invoke("SpawnTumbleweed", Random.Range(3.0f, 7.0f));
        }
    }

    public void EndChallenge()
    {
        GameController.challengeEventManager.wind.SetActive(false);
        Animal.DisableSpeedModifier("tailwind");
        foreach (var cact in GameObject.FindGameObjectsWithTag("Cactus"))
        {
            Destroy(cact);
        }
    }
}
