using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SchemeManager : MonoBehaviour
{
    public Image background;
    public BoxCollider2D extraItemsBounds;
    public GrassSpawner grassSpawner;
    public SpriteRenderer[] grassTextures;
    public Scheme[] schemes;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        ChangeScheme(2);
    }

    public void ChangeScheme(int index)
    {
        foreach (var deco in GameObject.FindGameObjectsWithTag("SchemeDeco"))
        {
            Destroy(deco);
        }
        
        Scheme chosenScheme = schemes[index];
        background.color = chosenScheme.mainColor;
        foreach (var grass in grassTextures)
        {
            grass.color = chosenScheme.darkColor;
        }

        for (int i = 0; i < chosenScheme.extraItemsFrequency; i++)
        {
            Instantiate(chosenScheme.extraItems[Random.Range(0, chosenScheme.extraItems.Length)], GetRandomPointInBox(), Quaternion.identity);
        }
    }

    public Vector2 GetRandomPointInBox()
    {
        Bounds bounds = extraItemsBounds.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }
}

[System.Serializable]
public class Scheme
{
    public Color mainColor;
    public Color darkColor;
    [Range(0, 10)] public int extraItemsFrequency;
    public GameObject[] extraItems;
    public bool shouldSpawnGrass;

}
