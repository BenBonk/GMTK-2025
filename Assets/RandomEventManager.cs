using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomEventManager : MonoBehaviour
{
    public GameObject mole;
    public BoxCollider2D moleBounds;
    public GameObject butterfly;
    public GameObject lightning;
    private GameManager gameManager;
    private int lastEvent = 67;
    public GameObject rainEffect;
    public GameObject[] mudPuddles;
    public int numberOfMudPuddles = 100;

    private List<Vector2> placedPositions = new List<Vector2>();
    private void Start()
    {
        gameManager = GameController.gameManager;
        TryRandomEvent(); //COMMENT FOR PROD, JUST FOR TESTING
    }

    public void TryRandomEvent()
    {
        if (Random.Range(0,1) > 0) //0,5
        {
            return;
        }

        int chosenEvent = Random.Range(3,4);
        if (chosenEvent == lastEvent)
        {
            chosenEvent = Random.Range(0,999999);
        }
        
        if (chosenEvent == 0)
        {
            Invoke("SpawnMole", 7);
        }
        else if (chosenEvent == 1)
        {
            Invoke("SpawnButterfly", 7);
        }
        else if (chosenEvent == 2)
        {
            Invoke("SpawnMud", .5f);
            
        }
        else
        {
            Invoke("Rain", .5f);
            
        }
        
    }
    void SpawnMole()
    {
        Bounds bounds = moleBounds.bounds;
        Vector2 pos = new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y));
        Instantiate(mole, pos, Quaternion.identity);
        if (!gameManager.roundCompleted && gameManager.roundDuration>0)
        {
            Invoke("SpawnMole", Random.Range(3.0f, 7.0f));   
        }
    }
    public void SpawnMud()
    {
        int attempts = 0;
        int placed = 0;
        int maxAttempts = numberOfMudPuddles * 10;
        placedPositions.Clear();

        while (placed < numberOfMudPuddles && attempts < maxAttempts)
        {
            if (TrySpawnGrass())
                placed++;

            attempts++;
        }
    }

    bool TrySpawnGrass()
    {
        Bounds bounds = moleBounds.bounds;
        Vector2 candidate = new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );
        foreach (var pos in placedPositions)
        {
            if (Vector2.Distance(pos, candidate) < 4)
                return false;
        }
        Instantiate(mudPuddles[Random.Range(0, mudPuddles.Length)], candidate, Quaternion.identity);
        placedPositions.Add(candidate);
        return true;
    }

    void Rain()
    {
        rainEffect.SetActive(true);
        StartCoroutine(LightningStrike());

    }

    IEnumerator LightningStrike()
    {
        yield return new WaitForSeconds(Random.Range(5.0f,7.0f)); //7,10
        //Choose random animal
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        if (allAnimals.Length <= 0)
        {
            yield return null;
            yield break;
        }
        
        Animal chosenAnimal = allAnimals[Random.Range(0, allAnimals.Length)];
        Instantiate(lightning, new Vector3(chosenAnimal.transform.position.x-2, chosenAnimal.transform.position.y, 0), Quaternion.identity); //Quaternion.Euler(0, 0, Random.Range(-30, 30))
        yield return new WaitForSeconds(.7f);
        if (chosenAnimal.gameObject!=null)
        {
            chosenAnimal.StruckByLightning();   
        }
        if (!gameManager.roundCompleted && gameManager.roundDuration>0)
        {
            StartCoroutine(LightningStrike());
        }
    }
    
    void SpawnButterfly()
    {
        float topBuffer = 0.25f;
        float bottomBuffer = 0.25f;
        
        // Get vertical bounds of the camera in world space
        float z = Mathf.Abs(Camera.main.transform.position.z - mole.transform.position.z);
        Vector3 screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float minY = screenBottom.y + bottomBuffer;
        float maxY = screenTop.y - topBuffer;

        //  Choose a random Y position safely within bounds
        float randomY = Random.Range(minY, maxY);

        //  Set spawn position at the right edge
        float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
        Instantiate(butterfly, new Vector3(rightEdgeX, randomY,0), Quaternion.identity);
        if (!gameManager.roundCompleted && gameManager.roundDuration>0)
        {
            Invoke("SpawnButterfly", Random.Range(3.0f, 7.0f));    
        }
    }
}
