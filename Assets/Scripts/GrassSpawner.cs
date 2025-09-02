using UnityEngine;
using System.Collections.Generic;

public class GrassSpawner : MonoBehaviour
{
    public Sprite[] grassSprites;
    public int numberOfGrassObjects = 100;

    public List<Color> colorPalette = new List<Color>();

    public float minDistanceBetweenGrass = 0.5f;

    public RectTransform spawnAreaRectTransform;

    public int sortingOrder = 0;

    private Vector2 spawnAreaMin;
    private Vector2 spawnAreaMax;
    private List<Vector2> placedPositions = new List<Vector2>();

    void Start()
    {
       //SpawnGrass();
    }

    public void SpawnGrass()
    {
        if (spawnAreaRectTransform == null)
        {
            return;
        }

        CalculateSpawnAreaFromRectTransform();

        int attempts = 0;
        int placed = 0;
        int maxAttempts = numberOfGrassObjects * 10;

        while (placed < numberOfGrassObjects && attempts < maxAttempts)
        {
            if (TrySpawnGrass())
                placed++;

            attempts++;
        }
    }

    void CalculateSpawnAreaFromRectTransform()
    {
        Vector3[] worldCorners = new Vector3[4];
        spawnAreaRectTransform.GetWorldCorners(worldCorners);
        // worldCorners: [0]=bottomLeft, [1]=topLeft, [2]=topRight, [3]=bottomRight

        Vector3 bottomLeft = worldCorners[0];
        Vector3 topRight = worldCorners[2];

        spawnAreaMin = new Vector2(bottomLeft.x, bottomLeft.y);
        spawnAreaMax = new Vector2(topRight.x, topRight.y);
    }

    bool TrySpawnGrass()
    {
        if (grassSprites.Length == 0 || colorPalette.Count == 0)
        {
            Debug.Log("aaaaaaa");
            return false;
        }

        Vector2 tryPosition = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        foreach (var pos in placedPositions)
        {
            if (Vector2.Distance(pos, tryPosition) < minDistanceBetweenGrass)
                return false;
        }

        GameObject grassGO = new GameObject("Grass");
        SpriteRenderer sr = grassGO.AddComponent<SpriteRenderer>();
        sr.sprite = grassSprites[Random.Range(0, grassSprites.Length)];
        sr.color = colorPalette[Random.Range(0, colorPalette.Count)];
        sr.sortingOrder = sortingOrder;
        grassGO.tag = "SchemeDeco";
        grassGO.transform.position = tryPosition;
        grassGO.transform.SetParent(this.transform);

        placedPositions.Add(tryPosition);
        return true;
    }
}
