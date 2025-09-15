using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimalSpawner : MonoBehaviour
{
    public float spawnRate; // Time in seconds between spawns
    private float timeSinceLastSpawn = 0f;

    private BoonManager boonManager;

    private void Start()
    {
        boonManager = GameController.boonManager;
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime; // Increment the timer
        if (timeSinceLastSpawn >= spawnRate && GameController.gameManager.roundInProgress) // Check if it's time to spawn a new animal
        {
            SpawnRandomAnimal();
            timeSinceLastSpawn = 0f; // Reset the timer
        }
    }

    private void SpawnRandomAnimal()
    {
        var selectedAnimal =
            GameController.player.animalsInDeck[Random.Range(0, GameController.player.animalsInDeck.Count)];
        GameObject animal = Instantiate(selectedAnimal.animalPrefab);
        float topBuffer = 0.25f;
        float bottomBuffer = 0.25f;
        
        // Get vertical bounds of the camera in world space
        float z = Mathf.Abs(Camera.main.transform.position.z - animal.transform.position.z);
        Vector3 screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        // Account for the sprite's vertical size
        SpriteRenderer sr = animal.GetComponent<SpriteRenderer>();
        float halfHeight = sr.bounds.extents.y;

        float minY = screenBottom.y + halfHeight + bottomBuffer;
        float maxY = screenTop.y - halfHeight - topBuffer;

        //  Choose a random Y position safely within bounds
        float randomY = Random.Range(minY, maxY);

        //  Set spawn position at the right edge
        float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x + sr.bounds.extents.x;

        animal.transform.position = new Vector3(rightEdgeX, randomY, 0f);
        if (boonManager.ContainsBoon("Wolfpack") && selectedAnimal.name=="Wolf")
        {
            Instantiate(selectedAnimal.animalPrefab, new Vector3(rightEdgeX+Random.Range(2f,4f), randomY+Random.Range(0.5f, 1.5f), 0f), Quaternion.identity);
            if (Random.Range(0,2)==0)
            {
                Instantiate(selectedAnimal.animalPrefab, new Vector3(rightEdgeX+Random.Range(2f,4f), randomY+Random.Range(-1.5f, 0.5f), 0f), Quaternion.identity);
            }
        }
    }
}
