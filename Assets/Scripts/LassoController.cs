using System.Collections.Generic;
using UnityEngine;

public class LassoController : MonoBehaviour
{
    public LineRenderer lineRenderer; // Assign in inspector
    public int smoothingSubdivisions; // Higher = smoother
    public float pointDistanceThreshold; // Minimum distance between points
    private List<Vector2> rawPoints = new List<Vector2>();
    private bool isDrawing = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartLasso();
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            UpdateLasso();
        }
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            CompleteLasso();
        }
    }

    void StartLasso()
    {
        isDrawing = true;
        rawPoints.Clear();
        lineRenderer.positionCount = 0;
    }

    void UpdateLasso()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (rawPoints.Count == 0 || Vector2.Distance(mouseWorld, rawPoints[rawPoints.Count - 1]) > 0.1f)
        {
            rawPoints.Add(mouseWorld);

            List<Vector3> smooth = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
            lineRenderer.positionCount = smooth.Count;
            lineRenderer.SetPositions(smooth.ToArray());
        }
    }

    void CompleteLasso()
    {
        isDrawing = false;

        // Close the polygon
        if (rawPoints.Count > 2)
            rawPoints.Add(rawPoints[0]);

        // Close the smoothed line
        List<Vector3> smoothClosed = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
        smoothClosed.Add(smoothClosed[0]); // Close visually
        lineRenderer.positionCount = smoothClosed.Count;
        lineRenderer.SetPositions(smoothClosed.ToArray());

        SelectObjectsInLasso();
    }

    void SelectObjectsInLasso()
    {
        // Create a temporary GameObject with a PolygonCollider2D
        GameObject lassoArea = new GameObject("TempLassoCollider");
        PolygonCollider2D poly = lassoArea.AddComponent<PolygonCollider2D>();

        // Convert raw lasso points to local space of collider
        Vector2[] localPoints = rawPoints.ToArray();
        poly.points = localPoints;

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // match everything

        List<Collider2D> results = new List<Collider2D>();
        int count = poly.Overlap(filter, results);

        foreach (Collider2D col in results)
        {
            Debug.Log("Selected: " + col.name);
        }

        Destroy(lassoArea);
    }

    // Catmull-Rom Smoothing
    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    List<Vector3> GenerateSmoothLasso(List<Vector3> controlPoints, int subdivisions)
    {
        List<Vector3> smoothPoints = new List<Vector3>();

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector3 p0 = controlPoints[Mathf.Clamp(i - 1, 0, controlPoints.Count - 1)];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = controlPoints[Mathf.Clamp(i + 2, 0, controlPoints.Count - 1)];

            for (int j = 0; j < subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                smoothPoints.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        smoothPoints.Add(controlPoints[controlPoints.Count - 1]);
        return smoothPoints;
    }
}