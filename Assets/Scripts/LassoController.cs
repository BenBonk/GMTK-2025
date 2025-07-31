using System.Collections.Generic;
using UnityEngine;

public class LassoController : MonoBehaviour
{
    public LineRenderer lineRenderer; // Assign in inspector
    public int smoothingSubdivisions; // Higher = smoother
    public float pointDistanceThreshold; // Minimum distance between points
    public float closeThreshold = 0.5f; // Distance to consider the lasso closed
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

        if (rawPoints.Count == 0 || Vector2.Distance(mouseWorld, rawPoints[rawPoints.Count - 1]) > pointDistanceThreshold)
        {
            rawPoints.Add(mouseWorld);

            if (rawPoints.Count >= 4)
            {
                Vector2 newStart = rawPoints[rawPoints.Count - 2];
                Vector2 newEnd = rawPoints[rawPoints.Count - 1];

                for (int i = 0; i < rawPoints.Count - 3; i++)
                {
                    Vector2 segStart = rawPoints[i];
                    Vector2 segEnd = rawPoints[i + 1];

                    if (GetLineIntersection(segStart, segEnd, newStart, newEnd, out Vector2 intersection))
                    {
                        List<Vector2> loopPoints = new List<Vector2>();

                        // Include the intersection point as start
                        loopPoints.Add(intersection);

                        // Include points from start to end
                        for (int j = i + 1; j < rawPoints.Count - 1; j++)
                            loopPoints.Add(rawPoints[j]);

                        // Add the intersection again to close the polygon
                        loopPoints.Add(intersection);

                        rawPoints = loopPoints;

                        CompleteLasso();
                        return;
                    }
                }
            }

            // Update visual if still drawing
            List<Vector3> smooth = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
            lineRenderer.positionCount = smooth.Count;
            lineRenderer.SetPositions(smooth.ToArray());
        }
    }




    void CompleteLasso()
    {
        isDrawing = false;

        if (rawPoints.Count <3)
        {
            Debug.Log("Lasso too small — discarded.");
            return;
        }
        Vector2 start = rawPoints[0];
        Vector2 end = rawPoints[rawPoints.Count - 1];

        // If not already closed (e.g. via self-intersection)
        if (Vector2.Distance(start, end) > closeThreshold)
        {
            Debug.Log("Lasso did not close — discarded.");
            rawPoints.Clear();
            lineRenderer.positionCount = 0;
            return;
        }

        // Close the polygon
        if (rawPoints.Count > 2)
            rawPoints.Add(start);

        // render the smoothed line
        List<Vector3> smoothClosed = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
        smoothClosed.Add(smoothClosed[0]);
        lineRenderer.positionCount = smoothClosed.Count;
        lineRenderer.SetPositions(smoothClosed.ToArray());

        SelectObjectsInLasso();
    }

    void SelectObjectsInLasso()
    {
        if (rawPoints.Count < 3)
            return;

        Collider2D[] allColliders = FindObjectsOfType<Collider2D>();

        foreach (Collider2D col in allColliders)
        {
            Vector2 center = col.bounds.center;

            if (IsPointInPolygon(center, rawPoints))
            {
                Debug.Log("Selected: " + col.name);
            }
        }
    }

    //Catmull-Rom Smoothing
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

    bool GetLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
        if (Mathf.Approximately(d, 0)) return false;

        float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        float v = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

        if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
        {
            intersection = a1 + u * (a2 - a1);
            return true;
        }

        return false;
    }

    bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            if ((pi.y > point.y) != (pj.y > point.y) &&
                point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + float.Epsilon) + pi.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}