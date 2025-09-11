using System.Collections;
using System.Collections.Generic;
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
    void Start()
    {
        ChangeScheme(0);
    }

    public void SetRandomScheme()
    {
        int odds = Random.Range(0, 100);
        if (odds < 70)
        {
            ChangeScheme(0);
        }
        else if (odds <= 80)
        {
            ChangeScheme(1);
        }
        else if (odds <= 90)
        {
            ChangeScheme(2);
        }
        else
        {
            ChangeScheme(3);
        }
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

        List<Vector2> spawnPoints = GenerateNonOverlappingPositions(chosenScheme.extraItemsFrequency, chosenScheme.minDistanceBetweenExtraItems);
        foreach (var pos in spawnPoints)
        {
            GameObject prefab = chosenScheme.extraItems[Random.Range(0, chosenScheme.extraItems.Length)];
            GameObject a = Instantiate(prefab, pos, Quaternion.identity);
            a.transform.SetParent(grassSpawner.transform);
        }

        if (chosenScheme.shouldSpawnGrass)
        {
            grassSpawner.colorPalette = chosenScheme.grassColors;
            grassSpawner.SpawnGrass();
        }
    }
    public List<Vector2> GenerateNonOverlappingPositions(int count, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();
        Bounds bounds = extraItemsBounds.bounds;

        int attempts = 0;
        while (positions.Count < count && attempts < 5000)
        {
            attempts++;
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            bool tooClose = false;
            foreach (var pos in positions)
            {
                if (Vector2.Distance(candidate, pos) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                positions.Add(candidate);
        }

        return positions;
    }
}


[System.Serializable]
public class Scheme
{
    public Color mainColor;
    public Color darkColor;
    [Range(0, 1000)] public int extraItemsFrequency;
    public float minDistanceBetweenExtraItems;
    public GameObject[] extraItems;
    public bool shouldSpawnGrass;
    public List<Color> grassColors;

}
