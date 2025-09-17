using UnityEngine;

public class Sheep : Animal
{
    public Sprite blackSheep;
    public override void ActivateLegendary()
    {
        if (Random.Range(0, 8) == 0)
        {
            GetComponent<SpriteRenderer>().sprite = blackSheep;
        }
    }
}
