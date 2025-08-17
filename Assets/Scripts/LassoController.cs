using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LassoController : MonoBehaviour
{
    [HideInInspector] public LineRenderer lineRenderer;
    public GameObject lassoPrefab;
    public int smoothingSubdivisions; // Higher = smoother
    public float pointDistanceThreshold; // Minimum distance between points
    public float closeThreshold = 0.5f; // Release within this distance to close
    private List<Vector2> rawPoints = new List<Vector2>();
    [HideInInspector] public bool isDrawing = false;
    public GameObject feedbackTextGroupPrefab;

    public float tipRadius = 0.9f;     //Auto-close detection radius
    Transform tipXform;


    public float feedbackDelay = 0.5f;    // delay between each text popup

    public Color pointBonusColor;
    public Color negativePointBonusColor;

    public Color cashBonusColor;
    public Color negativeCashBonusColor;

    public Color positiveMultColor;
    public Color negativeMultColor;
    public bool canLasso;


    [Header("Forward Tip Bubble (offset)")]
    public float tipForwardOffset = 0.35f;        // push bubble forward of the pen

    // ==========================================================

    [Header("Tip Forward Offset")]
    public bool tipUseForwardOffset = true;   // off by default (keeps current behavior)
    [Min(1)]
    public int tipForwardLookback = 3;


    [Header("Tip Tail Skip (by distance)")]
    public float tipTailDist = 0.35f;          // ignore last X meters of stroke
    public int tipTailMaxPts = 20;             // but cap by count

    [Header("Auto-close guards")]
    public int minPointsBeforeChecking = 50;     
    public int minArcPoints = 6;                
    public float minArcDistance = 0.5f;

    // Debug values so the yellow visual matches real test center
    public bool debugDrawTip = true;
    Vector2 debugTipCenter;
    Vector2 debugTipTangent;
    bool debugTipCenterValid = false;

    void Awake()
    {
        smoothingSubdivisions = Mathf.Max(1, smoothingSubdivisions);
    }

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

        // Tip collider
        var tipGO = new GameObject("LassoTip");
        tipXform = tipGO.transform;
    }

    void UpdateLasso()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (rawPoints.Count == 0 || Vector2.Distance(mouseWorld, rawPoints[rawPoints.Count - 1]) > pointDistanceThreshold)
        {
            rawPoints.Add(mouseWorld);

            if (tipXform != null) { tipXform.position = new Vector3(mouseWorld.x, mouseWorld.y, 0f); }

            // auto-close if tip overlaps
            TryAutoCloseWithTip();

            if (!isDrawing || lineRenderer == null)
                return;

            if (rawPoints.Count >= 4)
            {
                Vector2 newStart = rawPoints[rawPoints.Count - 2];
                Vector2 newEnd = rawPoints[rawPoints.Count - 1];

                for (int i = 0; i < rawPoints.Count - 3; i++)
                {
                    Vector2 segStart = rawPoints[i];
                    Vector2 segEnd = rawPoints[i + 1];

                    //check if intersecting (redundant with tip auto-close, but more general)
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
            if (rawPoints.Count > 0)
            {
                var smooth = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
                lineRenderer.positionCount = smooth.Count;
                if (smooth.Count > 0)
                    lineRenderer.SetPositions(smooth.ToArray());
                else
                    lineRenderer.positionCount = 0;
            }
            else
            {
                lineRenderer.positionCount = 0;
            }
        }

        if (debugDrawTip && debugTipCenterValid)
        {
            int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = (Vector3)debugTipCenter + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * tipRadius;
                Vector3 p1 = (Vector3)debugTipCenter + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * tipRadius;
                Debug.DrawLine(p0, p1, Color.yellow);
            }

            // Optional: draw forward tangent ray so you can see "front"
            Debug.DrawLine(debugTipCenter, debugTipCenter + debugTipTangent * (tipRadius * 1.2f), Color.magenta);
        }
    }

    public void CompleteLasso()
    {
        isDrawing = false;
        if (rawPoints == null || rawPoints.Count < 3) { DestroyLassoExit(true); return; }

        Vector2 start = rawPoints[0];
        Vector2 end = rawPoints[rawPoints.Count - 1];

        // Consider "already closed" either by being near start OR explicitly closed (last == first)
        bool explicitlyClosed = (rawPoints[0] - rawPoints[rawPoints.Count - 1]).sqrMagnitude <= 1e-6f;
        bool alreadyClosed = explicitlyClosed || Vector2.Distance(start, end) <= closeThreshold;

        // Mouse-up bubble close is disabled for now; rely on tip auto-close during draw.
        if (!alreadyClosed)
        {
            Debug.Log("Lasso did not close, discarded.");
            DestroyLassoExit(true);
            return;
        }

        // Ensure explicit closure exactly once
        if ((rawPoints[rawPoints.Count - 1] - rawPoints[0]).sqrMagnitude > 1e-6f)
            rawPoints.Add(rawPoints[0]); // only if not already closed

        // Area check on lasso polygon
        float area = CalculatePolygonAreaNormalized(rawPoints);

        float z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 bl = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z));
        Vector3 tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
        float screenWorldArea = Mathf.Abs(tr.x - bl.x) * Mathf.Abs(tr.y - bl.y);
        float areaThreshold = screenWorldArea * 0.003f;

        if (area < areaThreshold)
        {
            Debug.Log($"Lasso area too small ({area:F2} < {areaThreshold:F2}), discarded.");
            DestroyLassoExit(true);
            return;
        }

        AudioManager.Instance.PlaySequentialSFX("lasso_create", "lasso_pull");

        // Generate smooth loop
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

        // find animals within the lasso
        List<GameObject> lassoedObjects = SelectObjectsInLasso();
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

        DestroyLassoExit(false);
    }

    private IEnumerator ShowFeedbackSequence((double pointBonus, double pointMult, double currencyBonus, double currencyMult) result)
    {
        float zDepth = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 screenBase = new Vector3(Screen.width / 2f, 100f, zDepth);
        Vector3 baseWorld = Camera.main.ScreenToWorldPoint(screenBase);
        baseWorld.z = 0f;

        List<GameObject> createdGroups = new();

        bool bonusPointsShown = result.pointBonus == 0;
        bool multPointsShown = Math.Abs(result.pointMult - 1f) <= 0.01f || result.pointBonus == 0;

        bool bonusCashShown = result.currencyBonus == 0;
        bool multCashShown = Math.Abs(result.currencyMult - 1f) <= 0.01f || result.currencyBonus == 0;

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
                bonusText.text = $"+Points: {FormatNumber(result.pointBonus)}";
                bonusText.color = result.pointBonus >= 0 ? pointBonusColor : negativePointBonusColor;
            }

            if (multText != null)
            {
                if (Math.Abs(result.pointMult - 1f) > 0.01f)
                {
                    multText.text = $"x{FormatMult(result.pointMult)}";
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
                    double totalPoints = Math.Round(result.pointBonus * result.pointMult);
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

            bool hasValidPointMultiplier = result.pointBonus != 0 && Math.Abs(result.pointMult - 1f) > 0.01f;

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
                bonusText.text = $"+Cash: {FormatNumber(result.currencyBonus)}";
                bonusText.color = result.currencyBonus >= 0 ? cashBonusColor : negativeCashBonusColor;
            }

            if (multText != null)
            {
                if (Math.Abs(result.currencyMult - 1f) > 0.01f)
                {
                    multText.text = $"x{FormatMult(result.currencyMult)}";
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
                    double totalCash = Math.Round(result.currencyBonus * result.currencyMult);
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

    List<GameObject> SelectObjectsInLasso()
    {
        if (rawPoints.Count < 3) return null;

        var list = new List<GameObject>();
        var animals = FindObjectsOfType<Animal>(false); // only active

        foreach (var animal in animals)
        {
            // Use collider center if present; fall back to transform.position
            var col = animal.GetComponent<Collider2D>();
            Vector2 point = col ? (Vector2)col.bounds.center : (Vector2)animal.transform.position;

            if (IsPointInPolygon(point, rawPoints))
            {
                animal.isLassoed = true;
                list.Add(animal.gameObject);
            }
        }

        return list;
    }

    public void ShowCaptureFeedback((double pointBonus, double pointMult, double currencyBonus, double currencyMult) result)
    {
        StartCoroutine(ShowFeedbackSequence(result));
    }

    private void ShowMultiplierPopIn(TMP_Text multText, TMP_Text bonusText, double baseValue, double multiplier)
    {
        if (multText == null || bonusText == null) return;

        // Update bonus text immediately when multiplier is revealed
        double newTotal = Math.Round(baseValue * multiplier);
        bool isPoints = bonusText.text.StartsWith("+Points");
        if (bonusText.text.StartsWith("+Points"))
            bonusText.text = $"+Points: {FormatNumber(newTotal)}";
        else if (bonusText.text.StartsWith("+Cash"))
            bonusText.text = $"+Cash: {FormatNumber(newTotal)}";

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

    public static string FormatNumber(double value)
    {
        if (Math.Abs(value) < 1e12)
            return value.ToString("N0");
        else
            return value.ToString("0.00E+0");
    }

    public static string FormatMult(double value)
    {
        if (Math.Abs(value) < 1000)
            return value.ToString("N2");
        else if (Math.Abs(value) < 1e10)
            return value.ToString("N0");
        else
            return value.ToString("0.00E+0");
    }

    void DestroyLassoExit(bool discardLineObject)
    {
        // Kill the floating tip if it exists
        if (tipXform != null)
        {
            Destroy(tipXform.gameObject);
            tipXform = null;
        }

        // Optionally destroy the line object (used on failure paths)
        if (discardLineObject && lineRenderer != null)
        {
            var go = lineRenderer.gameObject;
            lineRenderer = null;          // clear reference before destroy
            Destroy(go);
        }
        else
        {
            // On success, we keep the line (it’s been reparented to the group)
            lineRenderer = null;          // but still clear our reference
        }

        // Reset state
        rawPoints.Clear();
        isDrawing = false;
        debugTipCenterValid = false;
    }

    List<Vector3> GenerateSmoothLasso(List<Vector3> controlPoints, int subdivisions)
    {
        var smoothPoints = new List<Vector3>();

        if (controlPoints == null || controlPoints.Count == 0)
            return smoothPoints;

        if (controlPoints.Count == 1)
        {
            smoothPoints.Add(controlPoints[0]);
            return smoothPoints;
        }

        subdivisions = Mathf.Max(1, subdivisions);

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            int last = controlPoints.Count - 1;
            Vector3 p0 = controlPoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = controlPoints[Mathf.Min(i + 2, last)];

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

    float CalculatePolygonAreaNormalized(List<Vector2> pts)
    {
        int n = pts.Count;
        if (n < 3) return 0f;
        bool dupEnd = (pts[0] - pts[n - 1]).sqrMagnitude <= 1e-6f;
        int m = dupEnd ? n - 1 : n;

        float area = 0f;
        for (int i = 0; i < m - 1; i++)
            area += (pts[i].x * pts[i + 1].y) - (pts[i + 1].x * pts[i].y);
        area += (pts[m - 1].x * pts[0].y) - (pts[0].x * pts[m - 1].y);

        return Mathf.Abs(area * 0.5f);
    }

    void TryAutoCloseWithTip()
    {
        debugTipCenterValid = false;
        if (tipXform == null) return;
        if (rawPoints == null || rawPoints.Count < Mathf.Max(3, minPointsBeforeChecking)) return;

        // --- tangent & tip center ---
        Vector2 end = rawPoints[rawPoints.Count - 1];
        Vector2 tangent = Vector2.zero;
        int k = Mathf.Min(tipForwardLookback, rawPoints.Count - 1);
        for (int i = 0; i < k; i++)
            tangent += rawPoints[rawPoints.Count - 1 - i] - rawPoints[rawPoints.Count - 2 - i];

        if (tangent.sqrMagnitude < 1e-8f) tangent = Vector2.right; else tangent.Normalize();

        Vector2 baseTip = (Vector2)tipXform.position;
        Vector2 tipCenter = tipUseForwardOffset ? (baseTip + tangent * tipForwardOffset) : baseTip;

        debugTipCenter = tipCenter;
        debugTipTangent = tangent;
        debugTipCenterValid = true;

        float r2 = tipRadius * tipRadius;

        // Tail skip (distance-based)
        int tailSkip = ComputeTailSkipByDistance(tipTailDist, tipTailMaxPts);

        // Also ensure we never consider the last minArcPoints anyway
        int usableCount = Mathf.Max(0, rawPoints.Count - Mathf.Max(tailSkip, minArcPoints));
        if (usableCount < 2) return;

        int bestIdx = -1;
        float bestD2 = float.MaxValue;

        for (int i = 0; i < usableCount; i++)
        {
            int arcPts = rawPoints.Count - i;
            if (arcPts < minArcPoints) continue;           // guard 1: minimum points to end

            // guard 2: minimum arc length to end
            float dist = 0f;
            for (int j = i + 1; j < rawPoints.Count; j++)
            {
                dist += Vector2.Distance(rawPoints[j], rawPoints[j - 1]);
                if (dist >= minArcDistance) break;
            }
            if (dist < minArcDistance) continue;

            float d2 = ((Vector2)rawPoints[i] - tipCenter).sqrMagnitude;
            if (d2 <= r2 && d2 < bestD2)
            {
                bestD2 = d2;
                bestIdx = i;
            }
        }

        if (bestIdx == -1) return;

        AutoCloseAtPointIndex(bestIdx);
    }

    void AutoCloseAtPointIndex(int hitIndex)
    {
        // trims the leftover path 
        int count = rawPoints.Count - hitIndex;
        if (count < 2) return; // need at least hit + one more point

        var loop = new List<Vector2>(count + 1);
        for (int i = hitIndex; i < rawPoints.Count; i++)
            loop.Add(rawPoints[i]);

        //remove consecutive duplicates
        PruneConsecutiveDuplicates(loop, 1e-8f);

        // Ensure closure
        if ((loop[loop.Count - 1] - loop[0]).sqrMagnitude > 1e-6f)
            loop.Add(loop[0]);

        // Replace
        rawPoints = loop;

        if (tipXform) Destroy(tipXform.gameObject);
        tipXform = null;
        debugTipCenterValid = false;

        CompleteLasso();
    }

    int ComputeTailSkipByDistance(float tailDist, int maxPts)
    {
        int endIdx = rawPoints.Count - 1;
        float acc = 0f;
        int skip = 0;
        for (int i = endIdx; i > 0; --i)
        {
            acc += Vector2.Distance(rawPoints[i], rawPoints[i - 1]);
            ++skip;
            if (acc >= tailDist || skip >= maxPts) break;
        }
        return skip;
    }

    void PruneConsecutiveDuplicates(List<Vector2> pts, float minSqr)
    {
        for (int i = pts.Count - 2; i >= 0; i--)
            if ((pts[i + 1] - pts[i]).sqrMagnitude <= minSqr)
                pts.RemoveAt(i + 1);
    }
}