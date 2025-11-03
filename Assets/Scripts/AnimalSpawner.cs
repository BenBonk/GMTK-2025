using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimalSpawner : MonoBehaviour
{
    public float spawnRate; // Time in seconds between spawns
    private float timeSinceLastSpawn = 0f;

    private BoonManager boonManager;
    private PostProcessingManager postProcessing;
    public GameObject animalLight;
    
    //Shoe stuff
    public List<AnimalData> currentShoe;
    private Player player;
    private int shoesize;
    
    private void Start()
    {
        player = GameController.player;
        boonManager = GameController.boonManager;
        postProcessing = GameController.postProcessingManager;
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

    public void GenerateShoe()
    {
        shoesize = 3;
        if(player.animalsInDeck.Count > 15)
            shoesize = 2;
        if(player.animalsInDeck.Count > 40)
            shoesize = 1;
        
        for (int i = 0; i < shoesize; i++)
        {
            currentShoe.AddRange(player.animalsInDeck);
        }

        if (GameController.postProcessingManager.isNight)
        {
            //could either remove nonpredators or add predators, ill opt to just add predators
            //get a list of the predators
            List<AnimalData> predatorsInDeck = new List<AnimalData>();
            foreach (var animal in player.animalsInDeck)
            {
                if (animal.isPredator)
                {
                    predatorsInDeck.Add(animal);
                }
            }
            //proportionally add random predator to shoe depending on shoe size 
            int predatorsToAdd = currentShoe.Count / 4; //add 25% predators for example
            for (int i = 0; i < predatorsToAdd; i++)
            {
                currentShoe.Add(predatorsInDeck[Random.Range(0, predatorsInDeck.Count)]);
            }
            Debug.Log("called");
        }
    }

    private void SpawnRandomAnimal()
    {
        if (currentShoe.Count== 0)
        {
            GenerateShoe();
        }
        
        var selectedAnimal = currentShoe[Random.Range(0, currentShoe.Count)];
        currentShoe.Remove(selectedAnimal);
        if (selectedAnimal.isPredator && Random.Range(0,4)==0 &&boonManager.ContainsBoon("Scarecrow"))
        {
            selectedAnimal = currentShoe[Random.Range(0, currentShoe.Count)];
        }
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

        if (postProcessing.isNight)
        {
            Instantiate(animalLight, animal.transform.position, Quaternion.identity, animal.transform);
        }
    }
}
