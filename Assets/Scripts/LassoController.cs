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

    // ===== High-impact knobs =====
    [Header("Tip while drawing")]
    [Range(0f, 1f)] public float drawFrontFactor = 0.35f; // offset = drawFrontFactor * tipRadius

    [Header("Release bubble (single knob)")]
    [Range(0f, 1f)] public float releaseAggression = 0.5f; // drives start/step/max
    [Range(0f, 1f)] public float releaseFrontFactor = 0.5f; // offset = releaseFrontFactor * radius

    [Header("Tail + guards")]
    public float tipTailDist = 0.35f;      // distance-only tail skip
    public float minArcDistance = 0.5f;    // require at least this much path length from candidate->end

    [Header("Tip/Debug")]
    public float tipRadius = 0.9f;         // auto-close detection radius (draw-time)
    public bool tipUseForwardOffset = true;
    [Min(1)] public int tipForwardLookback = 3; // segments to build a tangent
    public bool debugDrawTip = true;

    // Debug values so the yellow visual matches real test center
    Vector2 debugTipCenter;
    Vector2 debugTipTangent;
    bool debugTipCenterValid = false;

    // runtime
    Transform tipXform;

    // feedback colors/audio
    public float feedbackDelay = 0.5f;    // delay between each text popup
    public Color pointBonusColor;
    public Color negativePointBonusColor;
    public Color cashBonusColor;
    public Color negativeCashBonusColor;
    public Color positiveMultColor;
    public Color negativeMultColor;
    public bool canLasso;

    LocalizationManager localization;

    void Awake()
    {
        smoothingSubdivisions = Mathf.Max(1, smoothingSubdivisions);
    }

    private void Start()
    {
        localization = GameController.localizationManager;
    }

    void Update()
    {
        if (!canLasso) return;

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
        GameObject newLasso = Instantiate(lassoPrefab, Vector3.zero, Quaternion.identity);
        lineRenderer = newLasso.GetComponent<LineRenderer>();
        rawPoints.Clear();
        lineRenderer.positionCount = 0;

        // lightweight tip anchor
        var tipGO = new GameObject("LassoTip");
        tipXform = tipGO.transform;

        debugTipCenterValid = false;
    }

    void UpdateLasso()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (rawPoints.Count == 0 || Vector2.Distance(mouseWorld, rawPoints[rawPoints.Count - 1]) > pointDistanceThreshold)
        {
            rawPoints.Add(mouseWorld);

            if (tipXform != null) tipXform.position = new Vector3(mouseWorld.x, mouseWorld.y, 0f);

            // try to auto-close while drawing
            TryAutoCloseWithTip();

            // if auto-closed, CompleteLasso() may have run — bail out safely
            if (!isDrawing || lineRenderer == null) return;

            // (Optional) segment intersection fallback
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
                        loopPoints.Add(intersection);
                        for (int j = i + 1; j < rawPoints.Count - 1; j++)
                            loopPoints.Add(rawPoints[j]);
                        loopPoints.Add(intersection);

                        rawPoints = loopPoints;

                        CompleteLasso();
                        return;
                    }
                }
            }

            // Update visual while drawing
            if (rawPoints.Count > 0)
            {
                var smooth = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
                lineRenderer.positionCount = smooth.Count;
                if (smooth.Count > 0) lineRenderer.SetPositions(smooth.ToArray());
                else lineRenderer.positionCount = 0;
            }
            else
            {
                lineRenderer.positionCount = 0;
            }
        }

        // debug ring (matches logical center)
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
            Debug.DrawLine(debugTipCenter, debugTipCenter + debugTipTangent * (tipRadius * 1.2f), Color.magenta);
        }
    }

    public void CompleteLasso()
    {
        isDrawing = false;
        if (rawPoints == null || rawPoints.Count < 3) { DestroyLassoExit(true); return; }

        Vector2 start = rawPoints[0];
        Vector2 end = rawPoints[rawPoints.Count - 1];

        bool explicitlyClosed = (rawPoints[0] - rawPoints[rawPoints.Count - 1]).sqrMagnitude <= 1e-6f;
        bool alreadyClosed = explicitlyClosed || Vector2.Distance(start, end) <= closeThreshold;

        if (!alreadyClosed)
        {
            if (!ReleaseAutoClose())
            {
                Debug.Log("Lasso did not close, discarded.");
                DestroyLassoExit(true);
                return;
            }
        }

        // Ensure explicit closure exactly once
        if ((rawPoints[rawPoints.Count - 1] - rawPoints[0]).sqrMagnitude > 1e-6f)
            rawPoints.Add(rawPoints[0]);

        // Area check
        float area = CalculatePolygonAreaNormalized(rawPoints);
        float areaThreshold = GetScreenWorldAreaThreshold(0.003f); // factor retained from your version

        if (area < areaThreshold)
        {
            Debug.Log($"Lasso area too small ({area:F2} < {areaThreshold:F2}), discarded.");
            DestroyLassoExit(true);
            return;
        }

        AudioManager.Instance.PlaySequentialSFX("lasso_create", "lasso_pull");

        // Smooth loop for renderer
        List<Vector3> smoothClosed = GenerateSmoothLasso(rawPoints.ConvertAll(p => (Vector3)p), smoothingSubdivisions);
        smoothClosed.Add(smoothClosed[0]);

        // Visual points + bottom-center insertion (unchanged)
        List<Vector3> visualPoints = new List<Vector3>(smoothClosed);
        float zDepth = Mathf.Abs(Camera.main.transform.position.z - lineRenderer.transform.position.z);
        Vector3 screenBottomCenter = new Vector3(Screen.width / 2f, 0, zDepth);
        Vector3 bottomCenterWorld = Camera.main.ScreenToWorldPoint(screenBottomCenter);
        bottomCenterWorld.z = lineRenderer.transform.position.z;

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
        visualPoints.Insert(closestIndex + 1, bottomCenterWorld);

        lineRenderer.positionCount = visualPoints.Count;
        lineRenderer.SetPositions(visualPoints.ToArray());

        // Group and reparent
        Vector3 groupPosition = CalculateCentroidOfLasso();
        GameObject group = new GameObject("LassoGroup");
        group.transform.position = groupPosition;
        lineRenderer.transform.SetParent(group.transform);

        // Select objects inside
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

        // Animate to bottom
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
                localization.localPointsPopup.Arguments[0] = FormatNumber(result.pointBonus);
                localization.localPointsPopup.RefreshString();
                bonusText.text = localization.pointsPopup;
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
                    ShowMultiplierPopIn(multText, bonusText, result.pointBonus, result.pointMult, true);
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
                localization.localCashPopup.Arguments[0] = FormatNumber(result.currencyBonus);
                localization.localCashPopup.RefreshString();
                bonusText.text = localization.cashPopup;
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
                    ShowMultiplierPopIn(multText, bonusText, result.currencyBonus, result.currencyMult, false);
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

    private void ShowMultiplierPopIn(TMP_Text multText, TMP_Text bonusText, double baseValue, double multiplier, bool isPoints)
    {
        if (multText == null || bonusText == null) return;

        // Update bonus text immediately when multiplier is revealed
        double newTotal = Math.Round(baseValue * multiplier);

        if (isPoints)
        {
            localization.localPointsPopup.Arguments[0] = FormatNumber(newTotal);
            localization.localPointsPopup.RefreshString();
            bonusText.text = localization.pointsPopup;
        }
        else
        {
            localization.localCashPopup.Arguments[0] = FormatNumber(newTotal);
            localization.localCashPopup.RefreshString();
            bonusText.text = localization.cashPopup;
        }

        if (multiplier > 1f)
        {
            if (isPoints)
                AudioManager.Instance.PlaySFX("point_mult");
            else
                AudioManager.Instance.PlaySFX("cash_mult");
        }
        else
        {
            if (isPoints)
                AudioManager.Instance.PlaySFX("no_point_mult");
            else
                AudioManager.Instance.PlaySFX("no_cash_mult");
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

    // ===== Geometry/utility =====

    // Catmull-Rom Smoothing helper
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
        if (Math.Abs(value) < 1e12) return value.ToString("N0");
        return value.ToString("0.00E+0");
    }

    public static string FormatMult(double value)
    {
        if (Math.Abs(value) < 1000) return value.ToString("N2");
        if (Math.Abs(value) < 1e10) return value.ToString("N0");
        return value.ToString("0.00E+0");
    }

    void GetReleaseBubbleParams(float aggression, out float startR, out float stepR, out float maxR)
    {
        // low aggression = small start/step/max; high = larger/faster search
        startR = Mathf.Lerp(0.35f, 0.60f, aggression);
        stepR = Mathf.Lerp(0.08f, 0.18f, aggression);
        maxR = Mathf.Lerp(1.10f, 3f, aggression);
    }

    bool ReleaseAutoClose()
    {
        if (rawPoints == null || rawPoints.Count < 3) return false;

        Vector2 tangent = ComputeRecentTangent(tipForwardLookback);
        Vector2 baseTip = rawPoints[rawPoints.Count - 1];

        GetReleaseBubbleParams(releaseAggression, out float r, out float step, out float rMax);

        while (r <= rMax)
        {
            Vector2 center = baseTip + tangent * (releaseFrontFactor * r);
            float r2 = r * r;

            int tailSkip = ComputeTailSkipByDistance(tipTailDist);
            int usableCount = Mathf.Max(0, rawPoints.Count - tailSkip);

            int bestIdx = -1;
            float bestD2 = float.MaxValue;

            for (int i = 0; i < usableCount; i++)
            {
                // guard: require minimum distance along the path from candidate -> end
                float arc = 0f;
                for (int j = i + 1; j < rawPoints.Count; j++)
                {
                    arc += Vector2.Distance(rawPoints[j], rawPoints[j - 1]);
                    if (arc >= minArcDistance) break;
                }
                if (arc < minArcDistance) continue;

                float d2 = ((Vector2)rawPoints[i] - center).sqrMagnitude;
                if (d2 <= r2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    bestIdx = i;
                }
            }

            if (bestIdx != -1)
            {
                if (TryComputeCandidateLoopArea(bestIdx, out float candArea) &&
                    candArea >= GetScreenWorldAreaThreshold())
                {
                    if (TrimLoopAtIndex(bestIdx))
                        return true;
                }
            }

            r += step;
        }

        return false;
    }

    // ===== Draw-time auto close =====
    void TryAutoCloseWithTip()
    {
        debugTipCenterValid = false;
        if (tipXform == null) return;
        if (rawPoints == null || rawPoints.Count < 3) return;

        // tangent & forward-offset center (offset proportional to radius)
        Vector2 tangent = ComputeRecentTangent(tipForwardLookback);
        Vector2 baseTip = (Vector2)tipXform.position;
        Vector2 tipCenter = tipUseForwardOffset
            ? (baseTip + tangent * (drawFrontFactor * tipRadius))
            : baseTip;

        // debug
        debugTipCenter = tipCenter;
        debugTipTangent = tangent;
        debugTipCenterValid = true;

        float r2 = tipRadius * tipRadius;

        // distance-only tail skip
        int tailSkip = ComputeTailSkipByDistance(tipTailDist);
        int usableCount = Mathf.Max(0, rawPoints.Count - tailSkip);
        if (usableCount < 2) return;

        int bestIdx = -1;
        float bestD2 = float.MaxValue;

        for (int i = 0; i < usableCount; i++)
        {
            // guard: minimum arc length from candidate to end
            float arc = 0f;
            for (int j = i + 1; j < rawPoints.Count; j++)
            {
                arc += Vector2.Distance(rawPoints[j], rawPoints[j - 1]);
                if (arc >= minArcDistance) break;
            }
            if (arc < minArcDistance) continue;

            float d2 = ((Vector2)rawPoints[i] - tipCenter).sqrMagnitude;
            if (d2 <= r2 && d2 < bestD2)
            {
                bestD2 = d2;
                bestIdx = i;
            }
        }

        if (bestIdx == -1) return;

        if (!TryComputeCandidateLoopArea(bestIdx, out float candArea) ||
    candArea < GetScreenWorldAreaThreshold())
        {
            return;
        }

        if (TrimLoopAtIndex(bestIdx))
        {
            CompleteLasso();
        }
    }


    Vector2 ComputeRecentTangent(int lookback)
    {
        if (rawPoints.Count < 2) return Vector2.right;
        lookback = Mathf.Min(Mathf.Max(1, lookback), rawPoints.Count - 1);
        Vector2 t = Vector2.zero;
        for (int i = 0; i < lookback; i++)
            t += rawPoints[rawPoints.Count - 1 - i] - rawPoints[rawPoints.Count - 2 - i];

        if (t.sqrMagnitude <= 1e-8f) return Vector2.right;
        return t.normalized;
    }

    bool TrimLoopAtIndex(int hitIndex)
    {
        int count = rawPoints.Count - hitIndex;
        if (count < 3) return false;

        var loop = new List<Vector2>(count + 1);
        for (int i = hitIndex; i < rawPoints.Count; i++)
            loop.Add(rawPoints[i]);

        PruneConsecutiveDuplicates(loop, 1e-8f);
        if (loop.Count < 3) return false;

        if ((loop[loop.Count - 1] - loop[0]).sqrMagnitude > 1e-6f)
            loop.Add(loop[0]);

        rawPoints = loop;

        if (tipXform) { Destroy(tipXform.gameObject); tipXform = null; }
        debugTipCenterValid = false;

        return true;
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
            lineRenderer = null; // clear reference before destroy
            Destroy(go);
        }
        else
        {
            // On success, we keep the line (it’s reparented to the group)
            lineRenderer = null; // but still clear our reference
        }

        // Reset state
        rawPoints.Clear();
        isDrawing = false;
        debugTipCenterValid = false;
    }

    // ===== Geometry/utility from your version =====

    List<Vector3> GenerateSmoothLasso(List<Vector3> controlPoints, int subdivisions)
    {
        var smoothPoints = new List<Vector3>();
        if (controlPoints == null || controlPoints.Count == 0) return smoothPoints;

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

    int ComputeTailSkipByDistance(float tailDist)
    {
        int endIdx = rawPoints.Count - 1;
        float acc = 0f;
        int skip = 0;
        for (int i = endIdx; i > 0; --i)
        {
            acc += Vector2.Distance(rawPoints[i], rawPoints[i - 1]);
            ++skip;
            if (acc >= tailDist) break;
        }
        return skip;
    }

    void PruneConsecutiveDuplicates(List<Vector2> pts, float minSqr)
    {
        for (int i = pts.Count - 2; i >= 0; i--)
            if ((pts[i + 1] - pts[i]).sqrMagnitude <= minSqr)
                pts.RemoveAt(i + 1);
    }

    float GetScreenWorldAreaThreshold(float factor = 0.004f)
    {
        float z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 bl = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z));
        Vector3 tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
        float screenWorldArea = Mathf.Abs(tr.x - bl.x) * Mathf.Abs(tr.y - bl.y);
        return screenWorldArea * factor;
    }

    float GetScreenWorldAreaThreshold()
{
    float z = Mathf.Abs(Camera.main.transform.position.z);
    Vector3 bl = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z));
    Vector3 tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
    float screenWorldArea = Mathf.Abs(tr.x - bl.x) * Mathf.Abs(tr.y - bl.y);
    return screenWorldArea * 0.003f;   // same scale you use in CompleteLasso (keep consistent)
}

bool TryComputeCandidateLoopArea(int hitIndex, out float area)
{
    area = 0f;
    int count = rawPoints.Count - hitIndex;
    if (count < 3) return false;

    var loop = new List<Vector2>(count + 1);
    for (int i = hitIndex; i < rawPoints.Count; i++) loop.Add(rawPoints[i]);

    PruneConsecutiveDuplicates(loop, 1e-8f);
    if (loop.Count < 3) return false;

    if ((loop[loop.Count - 1] - loop[0]).sqrMagnitude > 1e-6f)
        loop.Add(loop[0]);

    area = CalculatePolygonAreaNormalized(loop);
    return true;
}
}
