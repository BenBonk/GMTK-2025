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
        GameObject animal = Instantiate(GameController.player.animalsInDeck[Random.Range(0,GameController.player.animalsInDeck.Count)]);
        //animal.GetComponent<Animal>().Move(); // Call the Move method on the animal to start its movement
    }
}
