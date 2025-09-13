using System.Collections.Generic;
using UnityEngine;

public class AnimalSpawner : MonoBehaviour
{
    public float spawnRate; // Time in seconds between spawns
    private float timeSinceLastSpawn = 0f;
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
        GameObject animal = Instantiate(GameController.player.animalsInDeck[Random.Range(0,GameController.player.animalsInDeck.Count)].animalPrefab);
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
    }
}
