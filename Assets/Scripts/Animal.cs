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

    public string name;
    public string description;
    private int level;
    public int price;
    public Sprite sprite;

    private void Start()
    {
        levelManager = GameController.animalLevelManager; 
        gameManager = GameController.gameManager;
        player = GameController.player;

        level = levelManager.GetLevel(name);
    }

    public virtual void CaptureAnimal()
    {
        gameManager.pointsThisRound += pointsToGive;
        player.playerCurrency += currencyToGive;

    }
}