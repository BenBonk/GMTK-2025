using System;
using System.Collections.Generic;
using UnityEngine;

public class Animal : MonoBehaviour
{
    private GameManager gameManager;
    private Player player;
    
    
    public int currencyToGive;
    public int pointsToGive;
    public string name;
    public string description;

    private void Start()
    {
        gameManager = GameController.gameManager;
        player = GameController.player;
    }

    public virtual void CaptureAnimal()
    {
        gameManager.pointsThisRound += pointsToGive;
        player.playerCurrency += currencyToGive;

    }
}