using UnityEngine;

public class Sheep : Animal
{
    public Sprite blackSheep;
    public override void ActivateLegendary()
    {
        legendary = false;
        if (Random.Range(0, 8) == 0)
        {
            legendary = true;
            GetComponent<SpriteRenderer>().sprite = blackSheep;
        }
    }
}
