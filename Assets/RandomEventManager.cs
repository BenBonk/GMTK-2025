using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
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
    public ParticleSystem rainParticles;
    public ParticleSystem cloudParticles;
    public GameObject mudPuddles;
    public int numberOfMudPuddles = 100;
    [HideInInspector] public bool isRaining;

    public LocalizedString[] randomEventStrings;

    private List<Vector2> placedPositions = new List<Vector2>();
    private void Start()
    {
        gameManager = GameController.gameManager;
        //TryRandomEvent(); //COMMENT FOR PROD, JUST FOR TESTING
        //Invoke("SpawnMole", 7);
        //Invoke("Rain", .5f);
    }

    public int GetRandomEvent()
    {
        if (Random.Range(0,5) > 0) //0,5
        {
            return -1;
        }

        int chosenEvent = Random.Range(0,4);
        while (chosenEvent == lastEvent)
        {
            chosenEvent = Random.Range(0,4);
        }
        lastEvent = chosenEvent;

        if (chosenEvent == 0)
        {
            //Invoke("SpawnMole", 7);
        }
        else if (chosenEvent == 1)
        {
            //Invoke("SpawnButterfly", 7);
        }
        else if (chosenEvent == 2)
        {
            //Invoke("SpawnMud", .5f);
            
        }
        else
        {
            //Invoke("Rain", .5f);
            
        }
        return chosenEvent;
        
    }

    public void StartRandomEvent(int eventID)
    {
        if (eventID == -1)
        {
            return;
        }
        else if (eventID == 0)
        {
            Invoke("SpawnMole", 7);
        }
        else if (eventID == 1)
        {
            Invoke("SpawnButterfly", 7);
        }
        else if (eventID == 2)
        {
            Invoke("SpawnMud", .5f);

        }
        else if(eventID == 3)
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
        Instantiate(mudPuddles, Vector3.zero + new Vector3(Random.Range(-5.0f,5.0f), Random.Range(-4.5f,4.5f),0), Quaternion.identity);
    }

    void Rain()
    {
        isRaining = true;
        rainEffect.SetActive(true);
        rainParticles.Play();
        cloudParticles.Play();
        StartCoroutine(LightningStrike());

    }

    IEnumerator LightningStrike()
    {
        yield return new WaitForSeconds(Random.Range(4.0f,6.0f)); 
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
        try
        {
            chosenAnimal.StruckByLightning();   
        }
        catch (Exception e)
        {
           //whomp whomp
        }
        if (!gameManager.roundCompleted && gameManager.roundDuration>0)
        {
            StartCoroutine(LightningStrike());
        }
        else
        {
            rainParticles.Stop();
            cloudParticles.Stop();
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
