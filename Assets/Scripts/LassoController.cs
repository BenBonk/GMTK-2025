using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LassoController : MonoBehaviour
{
    [HideInInspector]public LineRenderer lineRenderer;
    public GameObject lassoPrefab; // Assign in inspector
    public int smoothingSubdivisions; // Higher = smoother
    public float pointDistanceThreshold; // Minimum distance between points
    public float closeThreshold = 0.5f; // Distance to consider the lasso closed
    private List<Vector2> rawPoints = new List<Vector2>();
    [HideInInspector]public bool isDrawing = false;
    public GameObject feedbackTextGroupPrefab;

    public float feedbackDelay = 0.5f;    // delay between each text popup

    public Color pointBonusColor;
    public Color negativePointBonusColor;

    public Color cashBonusColor;
    public Color negativeCashBonusColor;

    public Color positiveMultColor;
    public Color negativeMultColor;
    public bool canLasso;
    void Update()
    {
        if (!canLasso)
        {
            return;
        }
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
        GameObject newLasso = Instantiate(lassoPrefab, Vector3.zero, Quaternion.identity); // Create a new lasso object
        lineRenderer = newLasso.GetComponent<LineRenderer>();
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




    public void CompleteLasso()
    {
        isDrawing = false;

        if (rawPoints.Count < 3)
        {
            Debug.Log("Lasso too small � discarded.");
            Destroy(lineRenderer.gameObject);
            return;
        }

        Vector2 start = rawPoints[0];
        Vector2 end = rawPoints[rawPoints.Count - 1];

        if (Vector2.Distance(start, end) > closeThreshold)
        {
            Debug.Log("Lasso did not close � discarded.");
            rawPoints.Clear();
            lineRenderer.positionCount = 0;
            Destroy(lineRenderer.gameObject);
            return;
        }

        float area = CalculatePolygonArea(rawPoints);

        // Calculate screen size in world units
        Vector3 screenBottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.main.transform.position.z)));
        Vector3 screenTopRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Mathf.Abs(Camera.main.transform.position.z)));

        float screenWorldWidth = Mathf.Abs(screenTopRight.x - screenBottomLeft.x);
        float screenWorldHeight = Mathf.Abs(screenTopRight.y - screenBottomLeft.y);
        float screenWorldArea = screenWorldWidth * screenWorldHeight;

        // Use a ratio (e.g. 1%) of screen area as minimum valid lasso
        float areaThreshold = screenWorldArea * 0.003f;

        if (area < areaThreshold)
        {
            Debug.Log($"Lasso area too small ({area:F2} < {areaThreshold:F2}) � discarded.");
            rawPoints.Clear();
            Destroy(lineRenderer.gameObject);
            return;
        }

        //GameController.gameManager.lassosUsed++;
        rawPoints.Add(start); // close loop
        AudioManager.Instance.PlaySequentialSFX("lasso_create", "lasso_pull");

        // Generate smooth loop (no tail yet)
        List<Vector3> smoothClosed = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
        smoothClosed.Add(smoothClosed[0]); // ensure visual closure

        // Clone to visual points for LineRenderer
        List<Vector3> visualPoints = new List<Vector3>(smoothClosed);

        // Calculate bottom center of screen in world space
        float zDepth = Mathf.Abs(Camera.main.transform.position.z - lineRenderer.transform.position.z);
        Vector3 screenBottomCenter = new Vector3(Screen.width / 2f, 0, zDepth);
        Vector3 bottomCenterWorld = Camera.main.ScreenToWorldPoint(screenBottomCenter);
        bottomCenterWorld.z = lineRenderer.transform.position.z;

        // Find index of closest point on the lasso loop
        int closestIndex = 0;
        float minDistance = float.MaxValue;
        for (int i = 0; i < smoothClosed.Count; i++)
        {
            float dist = Vector2.Distance(smoothClosed[i], bottomCenterWorld);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        // Insert the bottom center as the next point after the closest one
        visualPoints.Insert(closestIndex + 1, bottomCenterWorld);

        // Apply to LineRenderer
        lineRenderer.positionCount = visualPoints.Count;
        lineRenderer.SetPositions(visualPoints.ToArray());

        // Create parent group and reparent items
        Vector3 groupPosition = CalculateCentroidOfLasso();
        GameObject group = new GameObject("LassoGroup");
        group.transform.position = groupPosition;
        lineRenderer.transform.SetParent(group.transform);

        List<GameObject> lassoedObjects = SelectObjectsInLasso(); // only uses rawPoints
        List<Animal> lassoedAnimals = new List<Animal>();
        if (lassoedObjects != null && lassoedObjects.Count > 0)
        {
            foreach (GameObject item in lassoedObjects)
            {
                item.transform.SetParent(group.transform);
                lassoedAnimals.Add(item.GetComponent<Animal>());
            }
        }

        // Move group to bottom center of screen
        bottomCenterWorld.z = group.transform.position.z;
        bottomCenterWorld.y -= 6f;
        group.transform.DOMove(bottomCenterWorld, 1f)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                var captureResult = GameController.captureManager.MakeCapture(lassoedAnimals.ToArray());
                Destroy(group);
                ShowCaptureFeedback(captureResult);
            });
    }

    List<GameObject> SelectObjectsInLasso()
    {
        if (rawPoints.Count < 3)
            return null;

        Collider2D[] allColliders = FindObjectsOfType<Collider2D>();
        List<GameObject> list = new List<GameObject>();

        foreach (Collider2D col in allColliders)
        {
            Vector2 center = col.bounds.center;

            if (IsPointInPolygon(center, rawPoints))
            {
                //Debug.Log("Selected: " + col.name);
                col.gameObject.GetComponent<Animal>().isLassoed = true;
                list.Add(col.gameObject);
            }
        }

        return list;
    }

    public void ShowCaptureFeedback((int pointBonus, float pointMult, int currencyBonus, float currencyMult) result)
    {
        StartCoroutine(ShowFeedbackSequence(result));
    }


    private IEnumerator ShowFeedbackSequence((int pointBonus, float pointMult, int currencyBonus, float currencyMult) result)
    {
        float zDepth = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 screenBase = new Vector3(Screen.width / 2f, 100f, zDepth);
        Vector3 baseWorld = Camera.main.ScreenToWorldPoint(screenBase);
        baseWorld.z = 0f;

        List<GameObject> createdGroups = new();

        bool bonusPointsShown = result.pointBonus == 0;
        bool multPointsShown = Mathf.Abs(result.pointMult - 1f) <= 0.01f || result.pointBonus == 0;

        bool bonusCashShown = result.currencyBonus == 0;
        bool multCashShown = Mathf.Abs(result.currencyMult - 1f) <= 0.01f || result.currencyBonus == 0;

        int row = 0;

        // === POINTS BONUS ===
        if (true)
        {
            Vector3 offset = new Vector3(0, row++ * 1f, 0);
            GameObject group = Instantiate(feedbackTextGroupPrefab, baseWorld + offset, Quaternion.identity);
            createdGroups.Add(group);

            var bonusText = group.transform.Find("BonusText")?.GetComponent<TMP_Text>();
            var multText = bonusText.transform.Find("MultiplierText")?.GetComponent<TMP_Text>();

            if (bonusText != null)
            {
                bonusText.text = $"+Points: {result.pointBonus}";
                bonusText.color = result.pointBonus >= 0 ? pointBonusColor : negativePointBonusColor;
            }

            if (multText != null)
            {
                if (Mathf.Abs(result.pointMult - 1f) > 0.01f)
                {
                    multText.text = $"x{result.pointMult:F2}";
                    multText.color = result.pointMult > 1f ? positiveMultColor : negativeMultColor;
                    multText.gameObject.SetActive(false); // Hide initially
                }
                else
                {
                    multPointsShown = true;
                    multText.gameObject.SetActive(false);
                }
            }
            if (result.pointBonus > 0)
                AudioManager.Instance.PlaySFX("points");
            else
                AudioManager.Instance.PlaySFX("no_points");

            group.transform.localScale = Vector3.zero;
            Sequence pop = DOTween.Sequence();
            pop.Append(group.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            pop.Append(group.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));
            pop.OnComplete(() =>
            {
                bonusPointsShown = true;

                if (!multPointsShown && multText != null)
                {
                    multPointsShown = true;
                    ShowMultiplierPopIn(multText, bonusText, result.pointBonus, result.pointMult);
                }

                if (bonusPointsShown && multPointsShown)
                {
                    int totalPoints = Mathf.RoundToInt(result.pointBonus * result.pointMult);
                    if (TutorialManager._instance != null)
                    {
                        TutorialManager._instance.pointsThisRound += totalPoints;
                    }
                    else
                    {

                        GameController.gameManager.pointsThisRound += totalPoints;
                    }

                    Debug.Log($"Points added: {totalPoints}");
                }
            });

            bool hasValidPointMultiplier = result.pointBonus != 0 && Mathf.Abs(result.pointMult - 1f) > 0.01f;

            if (hasValidPointMultiplier)
            {
                yield return new WaitForSeconds(feedbackDelay + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(feedbackDelay - 0.3f);
            }
        }

        // === CURRENCY BONUS ===
        if (result.currencyBonus != 0)
        {
            Vector3 offset = new Vector3(0, row++ * 1f, 0);
            GameObject group = Instantiate(feedbackTextGroupPrefab, baseWorld + offset, Quaternion.identity);
            createdGroups.Add(group);

            var bonusText = group.transform.Find("BonusText")?.GetComponent<TMP_Text>();
            var multText = bonusText.transform.Find("MultiplierText")?.GetComponent<TMP_Text>();

            if (bonusText != null)
            {
                bonusText.text = $"+Cash: {result.currencyBonus}";
                bonusText.color = result.currencyBonus >= 0 ? cashBonusColor : negativeCashBonusColor;
            }

            if (multText != null)
            {
                if (Mathf.Abs(result.currencyMult - 1f) > 0.01f)
                {
                    multText.text = $"x{result.currencyMult:F2}";
                    multText.color = result.currencyMult > 1f ? positiveMultColor : negativeMultColor;
                    multText.gameObject.SetActive(false); // Hide initially
                }
                else
                {
                    multCashShown = true;
                    multText.gameObject.SetActive(false);
                }
            }

            if (result.pointBonus > 0)
                AudioManager.Instance.PlaySFX("cash");
            else
                AudioManager.Instance.PlaySFX("no_cash");

            group.transform.localScale = Vector3.zero;
            Sequence pop = DOTween.Sequence();
            pop.Append(group.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            pop.Append(group.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));
            pop.OnComplete(() =>
            {
                bonusCashShown = true;

                if (!multCashShown && multText != null)
                {
                    multCashShown = true;
                    ShowMultiplierPopIn(multText, bonusText, result.currencyBonus, result.currencyMult);
                }

                if (bonusCashShown && multCashShown)
                {
                    int totalCash = Mathf.RoundToInt(result.currencyBonus * result.currencyMult);
                    GameController.player.playerCurrency += totalCash;
                    Debug.Log($"Cash added: {totalCash}");
                }
            });

            yield return new WaitForSeconds(feedbackDelay);
        }

        // Wait before fade/move
        yield return new WaitForSeconds(0.3f);

        float moveUpAmount = 1f;
        float fadeDuration = 1.5f;

        foreach (GameObject group in createdGroups)
        {
            group.transform.DOMoveY(group.transform.position.y + moveUpAmount, fadeDuration).SetEase(Ease.OutSine);

            foreach (TMP_Text txt in group.GetComponentsInChildren<TMP_Text>())
            {
                txt.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad);
            }

            Destroy(group, fadeDuration + 0.1f);
        }
    }

    private void ShowMultiplierPopIn(TMP_Text multText, TMP_Text bonusText, int baseValue, float multiplier)
    {
        if (multText == null || bonusText == null) return;

        // Update bonus text immediately when multiplier is revealed
        int newTotal = Mathf.RoundToInt(baseValue * multiplier);
        bool isPoints = bonusText.text.StartsWith("+Points");
        if (bonusText.text.StartsWith("+Points"))
            bonusText.text = $"+Points: {newTotal}";
        else if (bonusText.text.StartsWith("+Cash"))
            bonusText.text = $"+Cash: {newTotal}";

        if (multiplier > 1f)
        {
            if (isPoints)
            {
                AudioManager.Instance.PlaySFX("point_mult");
            }
            else
            {
                AudioManager.Instance.PlaySFX("cash_mult");
            }
        }
        else
        {
            if (isPoints)
            {
                AudioManager.Instance.PlaySFX("no_point_mult");
            }
            else
            {
                AudioManager.Instance.PlaySFX("no_cash_mult");
            }
        }

        multText.gameObject.SetActive(true);
        multText.transform.localScale = Vector3.zero;
        multText.transform.localRotation = Quaternion.identity;

        Sequence multPop = DOTween.Sequence();

        multPop.Append(multText.transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutBack));
        multPop.Join(multText.transform.DOLocalRotate(new Vector3(0, 0, -15f), 0.05f));
        multPop.Append(multText.transform.DOLocalRotate(new Vector3(0, 0, 15f), 0.1f));
        multPop.Append(multText.transform.DOLocalRotate(Vector3.zero, 0.05f));
        multPop.Append(multText.transform.DOScale(1f, 0.1f).SetEase(Ease.OutCubic));
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

    Vector3 CalculateCentroidOfLasso()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Vector2 point in rawPoints)
        {
            sum += new Vector3(point.x, point.y, 0);
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    private float CalculatePolygonArea(List<Vector2> points)
    {
        if (points == null || points.Count < 3) return 0f;

        float area = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            area += (points[i].x * points[i + 1].y) - (points[i + 1].x * points[i].y);
        }

        // Close the loop (last to first)
        area += (points[points.Count - 1].x * points[0].y) - (points[0].x * points[points.Count - 1].y);

        return Mathf.Abs(area * 0.5f);
    }
}