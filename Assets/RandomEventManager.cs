using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomEventManager : MonoBehaviour
{
    public GameObject mole;
    public BoxCollider2D moleBounds;
    public GameObject butterfly;
    private GameManager gameManager;
    private int lastEvent = 67;
    private void Start()
    {
        gameManager = GameController.gameManager;
        //TryRandomEvent(); //COMMENT FOR PROD, JUST FOR TESTING
    }

    public void TryRandomEvent()
    {
        if (Random.Range(0,1) > 0) //0,5
        {
            return;
        }

        int chosenEvent = Random.Range(1,2);
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
