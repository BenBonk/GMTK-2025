using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Animal : MonoBehaviour
{
    private GameManager gameManager;
    private Player player;
    private AnimalLevelManager levelManager;

    public int currencyToGive;
    public int pointsToGive;

    public float speed;
    
    //movement parameters
    public bool isLassoed;
    public float traveled;
    public float leftEdgeX;
    public Vector3 startPos;


    public void Start()
    {
        levelManager = GameController.animalLevelManager; 
        gameManager = GameController.gameManager;
        player = GameController.player;
        //level = levelManager.GetLevel(name);

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 1f;

            // Set starting position slightly offscreen right
            startPos = new Vector3(rightEdge.x + 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }

        traveled = 0f;
    }

    public virtual void CaptureAnimal()
    {
        gameManager.pointsThisRound += pointsToGive;
        player.playerCurrency += currencyToGive;

    }

    public virtual void Move()
    {
        traveled += speed * Time.deltaTime;
        float x = startPos.x - traveled;

        transform.position = new Vector3(x, startPos.y, startPos.z);
    }

    public void Update()
    {
        if (!isLassoed)
        {
            Move();
        }
    }
}