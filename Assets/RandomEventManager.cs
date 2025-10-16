using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomEventManager : MonoBehaviour
{
    public GameObject mole;
    public BoxCollider2D moleBounds;
    public GameObject butterfly;
    private GameManager gameManager;
    private int lastEvent = 67;
    public GameObject[] mudPuddles;
    public int numberOfMudPuddles = 100;
    public RectTransform spawnAreaRectTransform;

    private Vector2 spawnAreaMin;
    private Vector2 spawnAreaMax;
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

        int chosenEvent = Random.Range(2,3);
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
            CalculateSpawnAreaFromRectTransform();
            //SpawnMud();
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
        if (spawnAreaRectTransform == null)
        {
            return;
        }

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

    void CalculateSpawnAreaFromRectTransform()
    {
        Vector3[] worldCorners = new Vector3[4];
        spawnAreaRectTransform.GetWorldCorners(worldCorners);
        
        Vector3 bottomLeft = worldCorners[0];
        Vector3 topRight = worldCorners[2];
        
        spawnAreaMin = new Vector2(bottomLeft.x, bottomLeft.y);
        spawnAreaMax = new Vector2(topRight.x, topRight.y);
    }

    bool TrySpawnGrass()
    {
        Vector2 tryPosition = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );
        foreach (var pos in placedPositions)
        {
            if (Vector2.Distance(pos, tryPosition) < 3)
                return false;
        }
        Instantiate(mudPuddles[Random.Range(0, mudPuddles.Length)], tryPosition, Quaternion.identity);

        placedPositions.Add(tryPosition);
        return true;
    }
    
}
