using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoonManager : MonoBehaviour
{
    private Player player;
    public Boon[] boonsToAddForTesting;

    // Dictionary for fast lookup: key = boon name, value = Boon object
    public Dictionary<string, Boon> boonDict = new Dictionary<string, Boon>();

    private void Start()
    {
        player = GameController.player;
        foreach (var b in boonsToAddForTesting)
        {
            player.boonsInDeck.Add(b);
        }
        InitializeDictionary();
    }
    private void InitializeDictionary()
    {
        boonDict = player.boonsInDeck.ToDictionary(b => b.name, b => b);
    }
    public bool ContainsBoon(string boonName)
    {
        return boonDict.ContainsKey(boonName);
    }
    public void AddBoon(Boon boon)
    {
        if (!boonDict.ContainsKey(boon.name))
        {
            player.boonsInDeck.Add(boon);
            boonDict.Add(boon.name, boon);
        }
    }
    public void RemoveBoon(Boon boon)
    {
        if (boonDict.ContainsKey(boon.name))
        {
            player.boonsInDeck.Remove(boon);
            boonDict.Remove(boon.name);
        }
    }
}