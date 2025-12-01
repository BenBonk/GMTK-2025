using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class LassoController : MonoBehaviour
{
    [HideInInspector] public LineRenderer lineRenderer;
    public GameObject lassoPrefab;
    private BoonManager boonManager;


    [Header("Lasso Particles")]
    public ParticleSystem drawParticles;                
    [Range(0f, 5f)] public float particlesPerMeter = 0.4f; // density
    [Min(0)] public int particlesCap = 60;               // hard cap
    [Min(0f)] public float particleOutwardSpeed = 1.5f;  // initial speed outward
    [Range(0f, 45f)] public float particleOutwardJitterDeg = 8f; // random spread
    [Range(0f, 1f)] public float particleOutwardBias = 0.7f; // 0=random, 1=fully outward
    [Min(1)] public int burstMin = 1;                  // particles per burst (min)
    [Min(1)] public int burstMax = 1;                  // particles per burst (max)
    [Range(0.5f, 2f)] public float spacingJitter = 1.3f; 
    [Header("Tether Particles")]
    public bool burstOnTether = true;
    [Range(0f, 5f)] public float tetherParticlesPerMeterScale = 0.6f;
    [Min(0)] public int tetherParticlesCap = 40;


    public int smoothingSubdivisions; // Higher = smoother
    public float pointDistanceThreshold; // Minimum distance between points
    public float closeThreshold = 0.5f; // Release within this distance to close
    private List<Vector2> rawPoints = new List<Vector2>();
    [HideInInspector] public bool isDrawing = false;
    public GameObject feedbackTextGroupPrefab;
    public Sprite farmbotIcon;

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

    public static readonly HashSet<LineRenderer> ActiveLines = new HashSet<LineRenderer>();

    // runtime
    Transform tipXform;

    // feedback colors/audio
    public float feedbackDelay = 0.7f;    // delay between each text popup
    public Color pointBonusColor;
    public Color negativePointBonusColor;
    public Color cashBonusColor;
    public Color negativeCashBonusColor;
    public Color positiveMultColor;
    public Color negativeMultColor;
    public bool canLasso;
    private PauseMenu pauseMenu;
    private SteamIntegration steamIntegration;

    LocalizationManager localization;

    void Awake()
    {
        smoothingSubdivisions = Mathf.Max(1, smoothingSubdivisions);
    }

    private void Start()
    {
        steamIntegration = GameController.steamIntegration;
        pauseMenu = GameController.pauseMenu;
        localization = GameController.localizationManager;
        boonManager = GameController.boonManager;
    }

    void Update()
    {
        if (!canLasso || pauseMenu.isOpen) return;
        if (isDrawing && (Input.GetMouseButtonDown(1)))
        {
            DestroyLassoExit(true);
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

    public static void Unregister(LineRenderer lr)
    {
        if (lr) ActiveLines.Remove(lr);
    }


    void StartLasso()
    {
        isDrawing = true;
        GameObject newLasso = Instantiate(lassoPrefab, Vector3.zero, Quaternion.identity);
        lineRenderer = newLasso.GetComponent<LineRenderer>();
        ActiveLines.Add(lineRenderer);
        rawPoints.Clear();
        lineRenderer.positionCount = 0;

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

            /*
            if (drawParticles != null)
            {
                // Move particle system to mouse position
                drawParticles.transform.position = mouseWorld;

                // Align particle direction with lasso forward vector
                if (rawPoints.Count > 1)
                {
                    Vector2 forward = (rawPoints[rawPoints.Count - 1] - rawPoints[rawPoints.Count - 2]).normalized;
                    float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

                    // Rotate particle system
                    drawParticles.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                }

                // Make sure it’s playing
                if (!drawParticles.isPlaying)
                    drawParticles.Play();
            }*/
        }

        // debug tip
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
        bool alreadyClosed = explicitlyClosed || Vector2.Distance(start, end) <= GetReleaseSnapWorld(releaseSnapPercent);

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
        float areaThreshold = GetScreenWorldAreaThreshold();

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

        var visualPoints = new List<Vector3>(smoothClosed);
        lineRenderer.positionCount = visualPoints.Count;
        lineRenderer.SetPositions(visualPoints.ToArray());

        if (TryGetCactiInside(out var cactiInside))
        {
            FadeOutActiveLasso();
            StartCoroutine(AnimateCactusHit(cactiInside));
            AudioManager.Instance.PlaySFX("cactus_break");
            return;
        }
        // Compute bottom-of-lasso and bottom-center
        float zDepth = Mathf.Abs(Camera.main.transform.position.z - lineRenderer.transform.position.z);
        Vector3 screenBottomCenter = new Vector3(Screen.width / 2f, 0, zDepth);
        Vector3 bottomCenterWorld = Camera.main.ScreenToWorldPoint(screenBottomCenter);
        bottomCenterWorld.z = lineRenderer.transform.position.z;

        // “bottom of the lasso” = lowest Y point
        int bottomIdx = 0;
        float minY = float.MaxValue;
        for (int i = 0; i < smoothClosed.Count; i++)
        {
            if (smoothClosed[i].y < minY)
            {
                minY = smoothClosed[i].y;
                bottomIdx = i;
            }
        }
        Vector3 bottomOfLassoWorld = smoothClosed[bottomIdx];
        bottomOfLassoWorld.z = bottomCenterWorld.z; // keep same Z as loop
                                                    // Make a separate tether 
        GameObject tetherGO = new GameObject("LassoTether");
        var tether = tetherGO.AddComponent<LineRenderer>();
        tether.useWorldSpace = false; // use local space since we're parenting

        // Copy styling from the main lasso
        CopyLineRendererStyle(lineRenderer, tether);

        // Group and reparent
        Vector3 groupPosition = CalculateCentroidOfLasso();
        GameObject group = new GameObject("LassoGroup");
        group.transform.position = groupPosition;
        lineRenderer.transform.SetParent(group.transform);
        tether.transform.SetParent(group.transform, false);

        // Convert to local positions relative to the group
        Vector3 localBottomOfLasso = group.transform.InverseTransformPoint(bottomOfLassoWorld);
        Vector3 localBottomCenter = group.transform.InverseTransformPoint(bottomCenterWorld);

        // Two-point segment from bottom of lasso to bottom-center of screen
        tether.positionCount = 2;
        tether.SetPosition(0, localBottomOfLasso);
        tether.SetPosition(1, localBottomCenter);

        // Push tether slightly behind the lasso in Z so it renders underneath
        Vector3 tetherPos = tether.transform.position;
        tetherPos.z = lineRenderer.transform.position.z + 0.01f;
        tether.transform.position = tetherPos;

        PrepareParticlesForBurst(clear: true);

        // Burst along the loop
        BurstParticlesAlongLoop(smoothClosed);

        // Burst along the tether
        if (burstOnTether)
        {
            BurstParticlesAlongSegment(
                bottomOfLassoWorld,
                bottomCenterWorld,
                groupPosition,                      
                tetherParticlesPerMeterScale,       
                tetherParticlesCap                 
            );
        }

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
                
                var captureResult = GameController.captureManager.MakeCapture(lassoedObjects.ToArray());
                Destroy(group);
                ShowCaptureFeedback(captureResult);
            });

        DestroyLassoExit(false);
    }


    public static List<SpriteRenderer> CreateBoonIcons(Transform anchor, IEnumerable<Sprite> sprites , float boonIconScale = 1f, float boonIconSpacing = 0.9f, float offsetX = 0, float offsetY = 0.9f)
    {
        var list = new List<SpriteRenderer>();
        if (sprites == null) return list;

        // materialize to count once
        var arr = sprites.Where(s => s != null).ToArray();
        int n = arr.Length;
        if (n == 0) return list;

        // Center the icons horizontally around anchor + offset.
        // If n=1 -> sits exactly at offset.x
        // If n=2 -> gap centered on offset.x
        // If n=3 -> middle at offset.x, etc.
        float totalSpan = (n - 1) * boonIconSpacing;
        float startX = offsetX - totalSpan * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var sp = arr[i];
            var go = new GameObject($"BoonIcon_{i}");
            go.transform.SetParent(anchor, worldPositionStays: false);

            float x = startX + i * boonIconSpacing;
            go.transform.localPosition = new Vector3(x, offsetY, 0f);
            go.transform.localScale = Vector3.one * boonIconScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 6;

            var c = sr.color; c.a = 1f; sr.color = c;

            list.Add(sr);
        }
        return list;
    }

    private IEnumerator ShowFeedbackSequence((double pointBonus, double pointMult, double currencyBonus, double currencyMult, HashSet<Sprite> boonSprites) result)
    {
        float zDepth = Mathf.Abs(Camera.main.transform.position.z);

        float xSpread = 0.12f; // max % of screen width
        // pick a random in [0,1], square it for bias, reapply sign
        float u = Random.value;            // 0–1
        float power = 1.4f; // closer to 1.0 = less bias, 2.0 = stronger bias
        float biased = Mathf.Pow(u, power);
        float sign = Random.value < 0.5f ? -1f : 1f;
        float randX = sign * biased * Screen.width * xSpread;

        Vector3 bottomCenter = new Vector3(Screen.width / 2f, 100f, zDepth); // fixed height above bottom
        Vector3 screenPos = new Vector3(bottomCenter.x + randX, bottomCenter.y, zDepth);

        Vector3 baseWorld = Camera.main.ScreenToWorldPoint(screenPos);
        baseWorld.z = 0f;

        // === tilt toward left/right ===
        float tiltMax = 10f;
        float normalizedX = randX / (Screen.width * xSpread);
        float angle = Mathf.Lerp(0f, tiltMax, Mathf.Abs(normalizedX));

        List<GameObject> createdGroups = new();
        bool boonsPlaced = false;

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
            group.transform.rotation = Quaternion.Euler(0, 0, normalizedX < 0 ? angle : -angle);

            var bonusText = group.transform.Find("BonusText")?.GetComponent<TMP_Text>();
            var multText = bonusText.transform.Find("MultiplierText")?.GetComponent<TMP_Text>();

            bool round10 = IsRoundToNearestActive();
            double shownBasePts = ShownPointsBase(result.pointBonus, result.pointMult, round10); // may round if mult ~ 1

            if (bonusText != null)
            {
                localization.localPointsPopup.Arguments[0] = FormatNumber(shownBasePts);
                localization.localPointsPopup.RefreshString();
                bonusText.text = localization.pointsPopup;
                bonusText.color = result.pointBonus >= 0 ? pointBonusColor : negativePointBonusColor;
                var bonusMat = bonusText.fontMaterial;
                bonusMat.SetColor("_GlowColor", bonusText.color);
                bonusMat.SetFloat("_GlowPower", result.pointBonus >= 0 ? .02f : 0f);
            }

            if (TutorialManager._instance != null)
                TutorialManager._instance.pointsThisRound += shownBasePts;
            else
                GameController.gameManager.pointsThisRound += shownBasePts;

            double finalPtsTotal = FinalPointsTotal(result.pointBonus, result.pointMult, round10);
            finalPtsTotal = SnapFinalToStep(finalPtsTotal, 10.0);


            if (multText != null)
            {
                if (Math.Abs(result.pointMult - 1f) > 0.01f)
                {
                    multText.text = $"x{FormatMult(result.pointMult)}";
                    multText.color = result.pointMult > 1f ? positiveMultColor : negativeMultColor;
                    var multMat = multText.fontMaterial;
                    multMat.SetColor("_GlowColor", multText.color);

                    float clampedMult = Mathf.Clamp((float)result.pointMult, 0f, 10f);
                    float t = clampedMult / 10f;
                    t = Mathf.SmoothStep(0f, 1f, t);
                    float glowPower = Mathf.Lerp(0.05f, 0.2f, t);
                    multMat.SetFloat("_GlowPower", glowPower);

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


            if (finalPtsTotal > 1 && IsRoundToNearestActive())
            {
                result.boonSprites.Add(farmbotIcon);
            }


            string storedValue = FBPP.GetString("highestPointsPerLasso");
            double currentHighest = 0;
            if (!string.IsNullOrEmpty(storedValue))
            {
                double.TryParse(storedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out currentHighest);
            }

            if (finalPtsTotal > currentHighest)
            {
                FBPP.SetString("highestPointsPerLasso", finalPtsTotal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if (finalPtsTotal >= 10000000 && !steamIntegration.IsThisAchievementUnlocked("Point Insanity"))
            {
                steamIntegration.UnlockAchievement("Point Insanity");
            }
            else if (finalPtsTotal >= 1000000 && !steamIntegration.IsThisAchievementUnlocked("Point Madness"))
            {
                steamIntegration.UnlockAchievement("Point Madness");
            }
            else if (finalPtsTotal >= 100000 && !steamIntegration.IsThisAchievementUnlocked("Point Fever"))
            {
                steamIntegration.UnlockAchievement("Point Fever");
            }

            //a
            group.transform.localScale = Vector3.zero;

            if (result.currencyBonus == 0 && result.boonSprites.Count > 0)
            {
                CreateBoonIcons(group.transform, result.boonSprites,1f,0.9f);
            }

            Sequence pop = DOTween.Sequence();
            pop.Append(group.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            pop.Append(group.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));
            pop.OnComplete(() =>
            {
                bonusPointsShown = true;

                if (!multPointsShown && multText != null)
                {
                    multPointsShown = true;
                    ShowMultiplierPopIn(multText, bonusText, shownBasePts, result.pointMult, true);
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
            group.transform.rotation = Quaternion.Euler(0, 0, normalizedX < 0 ? angle : -angle);

            var bonusText = group.transform.Find("BonusText")?.GetComponent<TMP_Text>();
            var multText = bonusText.transform.Find("MultiplierText")?.GetComponent<TMP_Text>();

            bool round5 = IsRoundToNearestActive();
            double shownBaseCash = ShownCashBase(result.currencyBonus, result.currencyMult, round5); // may round if mult ~ 1

            if (bonusText != null)
            {
                localization.localCashPopup.Arguments[0] = FormatNumber(shownBaseCash);
                localization.localCashPopup.RefreshString();
                bonusText.text = localization.cashPopup;
                bonusText.color = result.currencyBonus >= 0 ? cashBonusColor : negativeCashBonusColor;
                var bonusMat = bonusText.fontMaterial;
                bonusMat.SetColor("_GlowColor", bonusText.color);
                bonusMat.SetFloat("_GlowPower", result.pointBonus >= 0 ? .02f : 0f);
            }

            if (result.currencyBonus > 0)
                AudioManager.Instance.PlaySFX("cash");
            else
                AudioManager.Instance.PlaySFX("no_cash");

            GameController.player.playerCurrency += shownBaseCash;
            double finalCashTotal = FinalCashTotal(result.currencyBonus, result.currencyMult, round5);
            finalCashTotal = SnapFinalToStep(finalCashTotal, 5.0);

            if (multText != null)
            {
                if (Math.Abs(result.currencyMult - 1f) > 0.01f)
                {
                    multText.text = $"x{FormatMult(result.currencyMult)}";
                    multText.color = result.currencyMult > 1f ? positiveMultColor : negativeMultColor;
                    var multMat = multText.fontMaterial;
                    multMat.SetColor("_GlowColor", multText.color);

                    float clampedMult = Mathf.Clamp((float)result.currencyMult, 0f, 10f);
                    float t = clampedMult / 10f;
                    t = Mathf.SmoothStep(0f, 1f, t);
                    float glowPower = Mathf.Lerp(0.05f, 0.2f, t);
                    multMat.SetFloat("_GlowPower", glowPower);

                    multText.gameObject.SetActive(false); // Hide initially
                }
                else
                {
                    multCashShown = true;
                    multText.gameObject.SetActive(false);
                }
            }

            if (finalCashTotal > 1 && IsRoundToNearestActive())
            {
                result.boonSprites.Add(farmbotIcon);
            }

            if (finalCashTotal > FBPP.GetFloat("highestCashPerLasso"))
            {
                FBPP.SetFloat("highestCashPerLasso", (float)finalCashTotal);
            }
      

            if (result.boonSprites.Count > 0)
            {
                CreateBoonIcons(group.transform, result.boonSprites);
            }

            group.transform.localScale = Vector3.zero;
            Sequence pop = DOTween.Sequence();
            pop.Append(group.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            pop.Append(group.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));
            pop.OnComplete(() =>
            {
                bonusCashShown = true;

                if (!multCashShown && multText != null)
                {
                    // multiplier pop-in exists, so update after
                    multCashShown = true;
                    ShowMultiplierPopIn(multText, bonusText, shownBaseCash, result.currencyMult, false);
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

            foreach (SpriteRenderer sr in group.GetComponentsInChildren<SpriteRenderer>())
                sr.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad);

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
            if (!animal.CanBeLassoed) continue;

            var col = animal.GetComponent<Collider2D>();
            Vector2 point = col ? (Vector2)col.bounds.center : (Vector2)animal.transform.position;

            if (IsPointInPolygon(point, rawPoints) && !animal.isLassoed)
            {
                animal.isLassoed = true;
                list.Add(animal.gameObject);
            }
        }

        var eggs = FindObjectsOfType<Lassoable>(false);
        foreach (var egg in eggs)
        {
            var col = egg.GetComponent<Collider2D>();
            Vector2 point = col ? (Vector2)col.bounds.center : (Vector2)egg.transform.position;

            if (egg.CompareTag("NonAnimalLassoable") && IsPointInPolygon(point, rawPoints))
            {
                list.Add(egg.gameObject);
                Debug.Log($"Lassoed: {egg.gameObject.name}");
            }
        }

        return list;
    }

    bool TryGetCactiInside(out List<GameObject> cactiInside)
    {
        cactiInside = null;
        if (rawPoints == null || rawPoints.Count < 3) return false;

        var all = GameObject.FindGameObjectsWithTag("Cactus");
        if (all == null || all.Length == 0) return false;

        var list = new List<GameObject>();
        foreach (var c in all)
        {
            var col = c.GetComponent<Collider2D>();
            Vector2 p = col ? (Vector2)col.bounds.center : (Vector2)c.transform.position;

            if (IsPointInPolygon(p, rawPoints))
                list.Add(c);
        }

        if (list.Count == 0) return false;
        cactiInside = list;
        return true;
    }

    IEnumerator AnimateCactusHit(List<GameObject> cacti)
    {
        if (cacti == null || cacti.Count == 0) yield break;

        // timings
        float toRed = 0.15f;
        float shakeDur = 0.25f;
        float backDur = 0.25f;

        // small 2D shake strength
        Vector3 shakeStrength = new Vector3(0.15f, 0.15f, 0f);

        // run all animations in parallel
        var seqs = new List<Sequence>();
        foreach (var go in cacti)
        {
            if (!go) continue;

            // collect all sprite renderers under this cactus
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs == null || srs.Length == 0) continue;

            // optional: temporarily bump sort order so they pop in front
            var originalOrders = new int[srs.Length];
            for (int i = 0; i < srs.Length; i++)
            {
                originalOrders[i] = srs[i].sortingOrder;
                srs[i].sortingOrder = originalOrders[i] + 5;
            }

            // build one seq per cactus that:
            // 1) flashes its sprites toward red,
            // 2) shakes the transform,
            // 3) returns sprites to original color,
            // 4) restores sorting order.
            var seq = DOTween.Sequence();

            // cache original colors
            var origColors = new Color[srs.Length];
            for (int i = 0; i < srs.Length; i++) origColors[i] = srs[i].color;

            // target "reddish" keeping alpha
            void TintTowardRed(SpriteRenderer sr)
            {
                Color oc = sr.color;
                // push hue toward red without nuking alpha
                Color tgt = new Color(1f, oc.g * 0.35f, oc.b * 0.35f, oc.a);
                seq.Join(sr.DOColor(tgt, toRed));
            }

            foreach (var sr in srs) TintTowardRed(sr);

            // shake the cactus transform while it is red
            seq.Join(go.transform.DOShakePosition(
                shakeDur,
                strength: shakeStrength,
                vibrato: 20,
                randomness: 90f,
                snapping: false,
                fadeOut: true
            ));

            // return to original colors
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                var oc = origColors[i];
                seq.Append(sr.DOColor(oc, backDur));
            }

            // finally restore sorting order
            seq.OnComplete(() =>
            {
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i]) srs[i].sortingOrder = originalOrders[i];
                }
            });

            seqs.Add(seq);
        }

        // wait for the longest sequence to complete
        float total = toRed + shakeDur + backDur;
        yield return new WaitForSeconds(total);
    }


    public void ShowCaptureFeedback((double pointBonus, double pointMult, double currencyBonus, double currencyMult, HashSet<Sprite> boonSprites) result)
    {
        StartCoroutine(ShowFeedbackSequence(result));
    }

    private void ShowMultiplierPopIn(TMP_Text multText, TMP_Text bonusText, double baseValue, double multiplier, bool isPoints)
    {
        if (multText == null || bonusText == null) return;

        bool farmbot = IsRoundToNearestActive();

        // Update bonus text immediately when multiplier is revealed
        double finalTotal = isPoints
                ? FinalPointsTotal(baseValue, multiplier, farmbot)  
                : FinalCashTotal(baseValue, multiplier, farmbot);
        finalTotal = SnapFinalToStep(finalTotal, isPoints ? 10.0 : 5.0);

        double delta = finalTotal - baseValue;
        if (isPoints) AwardPoints(delta);
        else AwardCash(delta);

        if (isPoints)
        {
            localization.localPointsPopup.Arguments[0] = FormatNumber(finalTotal);
            localization.localPointsPopup.RefreshString();
            bonusText.text = localization.pointsPopup;

        }
        else
        {
            localization.localCashPopup.Arguments[0] = FormatNumber(finalTotal);
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

        // Scale size based on multiplier
        float minScale = 1.0f;
        float maxScale = 1.5f;
        float maxMult = 50f;

        float clampedMult = Mathf.Clamp((float)multiplier, 1f, maxMult);
        float t = (clampedMult - 1f) / (maxMult - 1f);
        float targetScale = Mathf.Lerp(minScale, maxScale, t);

        multText.transform.localScale = Vector3.one * targetScale;

        Sequence multPop = DOTween.Sequence();
        multPop.Append(multText.transform.DOScale(targetScale * 1.2f, 0.1f).SetEase(Ease.OutBack));
        multPop.Join(multText.transform.DOLocalRotate(new Vector3(0, 0, -15f), 0.05f));
        multPop.Append(multText.transform.DOLocalRotate(new Vector3(0, 0, 15f), 0.1f));
        multPop.Append(multText.transform.DOLocalRotate(Vector3.zero, 0.05f));
        multPop.Append(multText.transform.DOScale(targetScale, 0.1f).SetEase(Ease.OutCubic));
    }

    // ===== farmbot stuff =====
    private const double EPS = 1e-12;
    private static bool HasActiveMultiplier(double mult)
        => Math.Abs(mult - 1.0) > EPS;
    private static double FarmbotRoundUp(double value, double step)
    {
        if (value < 1.0) return value; // do not round values < 1
        double k = Math.Floor(value / step);
        double baseVal = k * step;
        double remainder = value - baseVal;

        if (remainder <= EPS) return baseVal;
        if (remainder < 1.0 - EPS) return baseVal;
        return (k + 1.0) * step;
    }

    private static double FinalPointsTotal(double baseValue, double mult, bool isFarmBot)
    {
        double total = baseValue * mult;
        return isFarmBot ? FarmbotRoundUp(total, 10.0) : total;
    }

    private static double FinalCashTotal(double baseValue, double mult, bool isFarmBot)
    {
        double total = baseValue * mult;
        return isFarmBot ? FarmbotRoundUp(total, 5.0) : total;
    }

    private static double ShownPointsBase(double baseValue, double mult, bool abilityRound10s)
    {
        return (!HasActiveMultiplier(mult) && abilityRound10s)
            ? FarmbotRoundUp(baseValue, 10.0)
            : baseValue;
    }

    private static double ShownCashBase(double baseValue, double mult, bool abilityRound5s)
    {
        return (!HasActiveMultiplier(mult) && abilityRound5s)
            ? FarmbotRoundUp(baseValue, 5.0)
            : baseValue;
    }

    private bool IsRoundToNearestActive() => GameController.gameManager.farmerID == 4;

    private void AwardPoints(double amount)
    {
        if (TutorialManager._instance != null) TutorialManager._instance.pointsThisRound += amount;
        else GameController.gameManager.pointsThisRound += amount;
    }

    private void AwardCash(double amount)
    {
        GameController.player.playerCurrency += amount;
    }

    private static double SnapFinalToStep(double value, double step)
    {
        if (value < 1.0) return value;

        return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
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
                // require minimum distance along the path from candidate -> end
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
            // minimum arc length from candidate to end
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

    public void DestroyLassoExit(bool discardLineObject)
    {
        if (tipXform != null)
        {
            Destroy(tipXform.gameObject);
            tipXform = null;
        }

        // FIX: unregister while we still have a valid reference
        if (lineRenderer != null) Unregister(lineRenderer);

        if (discardLineObject && lineRenderer != null)
        {
            var go = lineRenderer.gameObject;
            lineRenderer = null;
            Destroy(go);
        }
        else
        {
            lineRenderer = null;
        }

        rawPoints.Clear();
        isDrawing = false;
        debugTipCenterValid = false;
    }


    public void FadeOutActiveLasso(float downDistance = 0.1f, float duration = 0.4f)
    {
        if (lineRenderer == null) return;

        // stop draw particles, kill tip
        if (drawParticles) drawParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (tipXform != null) { Destroy(tipXform.gameObject); tipXform = null; }

        // snapshot current world positions
        int n = lineRenderer.positionCount;
        if (n <= 0) { DestroyLassoExit(true); return; }

        var worldPts = new Vector3[n];
        lineRenderer.GetPositions(worldPts);  // these are in world space because useWorldSpace is true

        // capture original gradient (we will preserve ALL keys and just scale alpha)
        Gradient original = lineRenderer.colorGradient;
        var origColorKeys = original.colorKeys; // preserve as-is
        var origAlphaKeys = original.alphaKeys; // we will scale these

        // unregister this active line so nothing else hits it during fade
        Unregister(lineRenderer);

        // immediately clear controller state so gameplay continues
        var lr = lineRenderer;
        lineRenderer = null;
        isDrawing = false;
        rawPoints.Clear();
        debugTipCenterValid = false;

        // tween 0 -> 1; on update: move points down and fade alpha
        float t = 0f;
        var seq = DG.Tweening.DOTween.Sequence();

        // we keep useWorldSpace = true; no parenting, no transform changes (no teleport)
        // movement ease
        Ease moveEase = Ease.InOutCubic;

        // update function: apply offset and fade
        void ApplyFrame(float v)
        {
            // move
            float eased = DG.Tweening.Core.Easing.EaseManager.Evaluate(moveEase, null, v, 1f, 0f, 0f); // ease(0..1)
            float dy = -Mathf.Abs(downDistance) * eased;

            for (int i = 0; i < n; i++)
            {
                var p = worldPts[i];
                worldPts[i] = new Vector3(p.x, p.y + dy, p.z);
            }
            if (lr != null) lr.SetPositions(worldPts);

            // fade: scale alpha keys by (1 - v), preserve times
            float aScale = 1f - v;
            var fadedAlpha = new GradientAlphaKey[origAlphaKeys.Length];
            for (int i = 0; i < origAlphaKeys.Length; i++)
            {
                fadedAlpha[i] = new GradientAlphaKey(origAlphaKeys[i].alpha * aScale, origAlphaKeys[i].time);
            }
            var g = new Gradient();
            g.SetKeys(origColorKeys, fadedAlpha);
            if (lr != null) lr.colorGradient = g;
        }

        seq.Join(DOTween.To(() => t, v => { t = v; ApplyFrame(t); }, 1f, duration));

        // ensure it stays on top visually if needed (optional small nudge up in sorting)
        var lrRenderer = lr != null ? lr.GetComponent<Renderer>() : null;
        if (lr != null)
        {
            lr.sortingOrder += 1; // optional: keep above ground
        }

        // cleanup
        seq.OnComplete(() =>
        {
            if (lr) Destroy(lr.gameObject);
        });
    }




    // ===== Geometry/utility =====

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

    float GetScreenWorldAreaThreshold()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 bl = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z));
        Vector3 tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
        float screenWorldArea = Mathf.Abs(tr.x - bl.x) * Mathf.Abs(tr.y - bl.y);
        return screenWorldArea * 0.002f;
    }

    [SerializeField]
    private float releaseSnapPercent = 0.02f;
    private float GetReleaseSnapWorld(float percent)
    {
        float z = Mathf.Abs(Camera.main.transform.position.z - lineRenderer.transform.position.z);

        Vector3 leftW = Camera.main.ScreenToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 rightW = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0f, z));
        float worldWidth = Mathf.Abs(rightW.x - leftW.x);
        return worldWidth * Mathf.Clamp01(percent);
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

    void CopyLineRendererStyle(LineRenderer src, LineRenderer dst)
    {
        dst.material = src.material;
        dst.widthMultiplier = src.widthMultiplier;
        dst.widthCurve = src.widthCurve;
        dst.numCornerVertices = src.numCornerVertices;
        dst.numCapVertices = src.numCapVertices;
        dst.textureMode = src.textureMode;
        dst.alignment = src.alignment;
        dst.colorGradient = src.colorGradient;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
    }

    void BurstParticlesAlongLoop(IList<Vector3> loop)
    {
        if (drawParticles == null || particlesPerMeter <= 0f) return;
        if (loop == null || loop.Count < 2) return;

        var main = drawParticles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // clear existing
        drawParticles.Clear(false);
        if (!drawParticles.isPlaying) drawParticles.Play(false);

        //total length
        float length = 0f;
        for (int i = 1; i < loop.Count; i++)
            length += Vector3.Distance(loop[i - 1], loop[i]);

        int targetCount = Mathf.Min(particlesCap, Mathf.CeilToInt(length * particlesPerMeter));
        if (targetCount <= 0 || length <= 1e-5f) return;

        // --- build distances 
        int bMin = Mathf.Max(1, burstMin);
        int bMax = Mathf.Max(bMin, burstMax);

        float expectedSpacing = length / Mathf.Max(1, targetCount);
        float jitter = Mathf.Clamp(spacingJitter, 0.5f, 2f);

        List<float> emitDistances = new List<float>(targetCount);
        float next = UnityEngine.Random.Range(expectedSpacing / jitter, expectedSpacing * jitter);

        while (emitDistances.Count < targetCount && next <= length)
        {
            // push one burst event
            emitDistances.Add(next);
            next += UnityEngine.Random.Range(expectedSpacing / jitter, expectedSpacing * jitter);
        }
        if (emitDistances.Count == 0) return;

        // centroid to decide "outward" 
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < loop.Count; i++) centroid += loop[i];
        centroid /= loop.Count;

        // place bursts
        int distIdx = 0;
        float traveled = 0f;

        for (int i = 0; i < loop.Count - 1 && distIdx < emitDistances.Count; i++)
        {
            Vector3 a = loop[i];
            Vector3 b = loop[i + 1];
            Vector3 seg = b - a;
            float segLen = seg.magnitude;
            if (segLen < 1e-4f) { continue; }

            Vector3 dir = seg / segLen;            // tangent
            Vector3 n = new Vector3(-dir.y, dir.x, 0f); // left normal
            if (Vector3.Dot(n, (a - centroid)) < 0f) n = -n; // flip to outward

            // place all emits 
            while (distIdx < emitDistances.Count && emitDistances[distIdx] <= traveled + segLen)
            {
                float dAlong = emitDistances[distIdx] - traveled;
                Vector3 p = a + dir * dAlong;

                int burstSize = UnityEngine.Random.Range(bMin, bMax + 1);
                int remaining = targetCount - (distIdx); 
                burstSize = Mathf.Min(burstSize, remaining);

                for (int k = 0; k < burstSize; k++)
                {
                    // random 2D unit vector
                    Vector2 rv = UnityEngine.Random.insideUnitCircle.normalized;
                    Vector3 randDir = new Vector3(rv.x, rv.y, 0f);

                    // bias toward outward normal
                    float t = Mathf.Clamp01(particleOutwardBias);
                    Vector3 outDir = (n * t + randDir * (1f - t));
                    if (outDir.sqrMagnitude < 1e-6f) outDir = n;
                    outDir.Normalize();

                    // small angular wobble
                    if (particleOutwardJitterDeg > 0f)
                    {
                        float ang = UnityEngine.Random.Range(-particleOutwardJitterDeg, particleOutwardJitterDeg) * Mathf.Deg2Rad;
                        outDir = Rotate2D(outDir, ang);
                    }

                    float speed = particleOutwardSpeed * UnityEngine.Random.Range(0.85f, 1.15f);

                    var ep = new ParticleSystem.EmitParams
                    {
                        position = p,
                        velocity = outDir * speed,
                        applyShapeToPosition = false
                    };
                    drawParticles.Emit(ep, 1);
                }

                distIdx++;
            }

            traveled += segLen;
        }
    }

    void BurstParticlesAlongSegment(Vector3 a, Vector3 b, Vector3 outwardRef, float densityScale, int cap)
    {
        if (!drawParticles || particlesPerMeter <= 0f) return;

        float segLen = Vector3.Distance(a, b);
        if (segLen <= 1e-5f) return;

        int target = Mathf.Min(cap, Mathf.CeilToInt(segLen * particlesPerMeter * Mathf.Max(0f, densityScale)));
        if (target <= 0) return;

        Vector3 dir = (b - a) / segLen;
        Vector3 n = new Vector3(-dir.y, dir.x, 0f); // left normal

        // Flip normal to point away from outwardRef (midpoint heuristic)
        Vector3 mid = (a + b) * 0.5f;
        if (Vector3.Dot(n, (mid - outwardRef)) < 0f) n = -n;

        float spacing = segLen / target;
        float d = 0f;

        for (int i = 0; i < target; i++)
        {
            Vector3 p = a + dir * d;

            // random unit direction (mostly random, biased outward)
            Vector2 rv = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 rand = new Vector3(rv.x, rv.y, 0f);
            Vector3 outDir = Vector3.Lerp(rand, n, particleOutwardBias).normalized;

            if (particleOutwardJitterDeg > 0f)
                outDir = Rotate2D(outDir, UnityEngine.Random.Range(-particleOutwardJitterDeg, particleOutwardJitterDeg) * Mathf.Deg2Rad);

            var ep = new ParticleSystem.EmitParams
            {
                position = p,
                velocity = outDir * particleOutwardSpeed,
                applyShapeToPosition = false
            };
            drawParticles.Emit(ep, 1);

            d += spacing;
        }
    }

    static Vector3 Rotate2D(Vector3 v, float radians)
    {
        float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
        return new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
    }

    void PrepareParticlesForBurst(bool clear)
    {
        if (!drawParticles) return;
        var main = drawParticles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // stays in world
        if (clear) drawParticles.Clear(false);
        if (!drawParticles.isPlaying) drawParticles.Play(false);
    }

}
