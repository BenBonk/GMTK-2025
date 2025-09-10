using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BoonManager : MonoBehaviour
{
    private Player player;

    private void Start()
    {
        player = GameController.player;
    }
    
    
    
    public bool ContainsBoon(string boon)
    {
        return player.boonsInDeck.Any(s => s.name == boon);
    }
}