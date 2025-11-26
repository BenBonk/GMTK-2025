using UnityEngine;

public class Puma : Animal
{
    [Header("Normal Hop Motion")]
    public float hopSpeed = 3.0f;
    public float hopHeight = 1.0f;
    public float hopDuration = 0.55f;

    [Header("Phase Durations")]
    public float pauseBeforeTiltUp = 0.15f;
    public float tiltUpDuration = 0.10f;
    public float pauseAfterTiltUp = 0.10f;
    public float tiltNeutralDuration = 0.18f;
    public float scanDuration = 0.30f;

    [Header("Tilt Angles")]
    public float tiltBackAngle = 18f;
    public float tiltForwardAngle = 18f;
    
    [Header("Scan & Leap")]
    public int hopsPerScan = 2;      // scan after this many completed hops
    public float detectionRange = 6f;     // only non-predators within this radius (to the left)
    public float leapSpeedMultiplier = 1.6f;   // speed during leap
    public float leapHeightMultiplier = 1.5f;   // vertical arc during leap
    public float maxLeapDistance = 5.0f;   // max distance to spend on a single leap
    public float arrivalRadius = 0.4f;   // end leap early when this close to target

    [Header("Scan Wobble")]
    public float prePounceAngle = 14f;
    public float scanWobbleAmplitude = 2.5f;
    public float scanWobbleFrequency = 3f;

    private enum Phase { Pause1, TiltUp, Pause2, Jump, TiltNeutral, Scan, Leap }
    private Phase phase = Phase.Pause1;

    // phase timing
    private float phaseTimer;
    private float phaseDuration;

    // normal hop state
    private float startYForHop;

    // tilt
    private float currentTilt = 0f;
    private float tiltStartAngle = 0f;
    private float tiltEndAngle = 0f;

    private int hopsCompleted = 0;

    // leap (distance-budgeted)
    private Animal leapTarget = null;   // live target to pursue
    private float leapBudgetStart = 0f; // planned distance to spend this leap
    private float leapBudget = 0f; // remaining distance
    private float prevHopYOffset = 0f; // delta-bob tracker
    private Vector3 fallbackLeapDir = Vector3.left;

    // scan wobble
    private float scanWobbleTime = 0f;

    // facing (assume sprite faces LEFT by default)
    private int facingSign = -1;

    // global-speed baselines (hop & timings only; leap left untouched)
    private float baseHopSpeed;
    private float baseHopDuration;
    private float basePauseBeforeTiltUp, baseTiltUpDuration, basePauseAfterTiltUp, baseTiltNeutralDuration, baseScanDuration;

    private float _lastScale = 1f; // for mid-phase timer rescale

    protected override void Awake()
    {
        base.Awake();
        FaceDirection(-1);

        baseHopSpeed = hopSpeed;
        baseHopDuration = hopDuration;

        basePauseBeforeTiltUp = pauseBeforeTiltUp;
        baseTiltUpDuration = tiltUpDuration;
        basePauseAfterTiltUp = pauseAfterTiltUp;
        baseTiltNeutralDuration = tiltNeutralDuration;
        baseScanDuration = scanDuration;
    }

    public override void Start()
    {
        base.Start();
        EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
    }

    protected override void ApplyEffectiveSpeedScale(float scale)
    {
        // Normal hop moves faster with global speed
        hopSpeed = baseHopSpeed * scale;

        // Shorten timing windows moderately as speed increases
        const float EXP_TIME = 0.6f;
        hopDuration = Mathf.Max(0.05f, baseHopDuration / Mathf.Pow(scale, EXP_TIME));
        pauseBeforeTiltUp = Mathf.Max(0.02f, basePauseBeforeTiltUp / Mathf.Pow(scale, EXP_TIME));
        tiltUpDuration = Mathf.Max(0.02f, baseTiltUpDuration / Mathf.Pow(scale, EXP_TIME));
        pauseAfterTiltUp = Mathf.Max(0.02f, basePauseAfterTiltUp / Mathf.Pow(scale, EXP_TIME));
        tiltNeutralDuration = Mathf.Max(0.02f, baseTiltNeutralDuration / Mathf.Pow(scale, EXP_TIME));
        scanDuration = Mathf.Max(0.05f, baseScanDuration / Mathf.Pow(scale, EXP_TIME));

        // Mid-phase rescale: if scale changed mid-phase, compress/expand the remaining time
        if (_lastScale > 0f && !Mathf.Approximately(scale, _lastScale))
        {
            float k = Mathf.Pow(scale / _lastScale, EXP_TIME);
            phaseTimer /= k; // preserves perceived progress when speed changes
        }

        _lastScale = scale;

    }

    protected override Vector3 ComputeMove()
    {
        float prog = PhaseProgress(); // 0..1 for timer-based phases
        Vector3 pos = transform.position;

        switch (phase)
        {
            case Phase.Jump:
                {
                    // Normal hop: move left with a simple vertical arc (no drift)
                    float yOffset = Mathf.Sin(prog * Mathf.PI) * hopHeight;
                    FaceDirection(-1);
                    pos += Vector3.left * hopSpeed * Time.deltaTime;
                    pos.y = startYForHop + yOffset;
                    break;
                }

            case Phase.Scan:
                // Idle; wobble handled in ApplyRunTilt()
                break;

            case Phase.Leap:
                {
                    // Vertical arc (taller for leap), driven by distance progress
                    float yOffset = Mathf.Sin(LeapProgress()) * (hopHeight * leapHeightMultiplier);

                    // Pursue target live position each frame
                    Vector3 dir = fallbackLeapDir;
                    if (leapTarget != null && !leapTarget.isLassoed)
                    {
                        Vector3 toTarget = leapTarget.transform.position - pos;
                        if (toTarget.sqrMagnitude > 0.0001f)
                        {
                            dir = toTarget.normalized;
                            fallbackLeapDir = dir;
                        }
                    }

                    float leapSpeed = hopSpeed * Mathf.Max(0.1f, leapSpeedMultiplier);
                    float stepDist = leapSpeed * Time.deltaTime;

                    // Move along pursuit direction
                    pos += dir * stepDist;

                    // Apply hop bob as DELTA
                    float bobDelta = yOffset - prevHopYOffset;
                    prevHopYOffset = yOffset;
                    pos.y += bobDelta;

                    // Face travel direction
                    if (Mathf.Abs(dir.x) > 0.01f) FaceDirection(dir.x >= 0f ? +1 : -1);

                    // Spend distance budget & check arrival
                    leapBudget -= stepDist;

                    bool arrived = false;
                    if (leapTarget != null && !leapTarget.isLassoed)
                    {
                        float dNow = Vector3.Distance(pos, leapTarget.transform.position);
                        if (dNow <= arrivalRadius) arrived = true;
                    }

                    if (arrived || leapBudget <= 0f)
                    {
                        // Finish leap, return to neutral tilt next
                        leapTarget = null;
                        leapBudget = 0f;
                        EnterPhase(Phase.TiltNeutral, tiltNeutralDuration);
                    }
                    break;
                }
        }

        // Timer-based transitions for non-leap phases
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
            AdvancePhase();

        return pos; // base Move() adds externalOffset, clamps Y, clears offset
    }

    protected override void ApplyRunTilt()
    {
        if (forceExit || (GameController.gameManager != null && GameController.gameManager.roundCompleted) || overriddenByAttraction)
        {
            base.ApplyRunTilt();
            return;
        }

        float desiredTilt = currentTilt;

        switch (phase)
        {
            case Phase.Pause1:
                desiredTilt = 0f;
                break;

            case Phase.TiltUp:
                desiredTilt = Mathf.Lerp(tiltStartAngle, tiltBackAngle, PhaseProgress());
                break;

            case Phase.Pause2:
                desiredTilt = tiltBackAngle;
                break;

            case Phase.Jump:
                desiredTilt = Mathf.Lerp(tiltBackAngle, -tiltForwardAngle, PhaseProgress());
                break;

            case Phase.Leap:
                // Interpolate based on leap distance progress, not a timer
                float lp = LeapProgress01();
                desiredTilt = Mathf.Lerp(tiltStartAngle, -prePounceAngle, lp);
                break;

            case Phase.TiltNeutral:
                desiredTilt = Mathf.Lerp(tiltStartAngle, 0f, PhaseProgress());
                break;

            case Phase.Scan:
                scanWobbleTime += Time.deltaTime;
                float baseAngle = Mathf.Lerp(tiltStartAngle, prePounceAngle, PhaseProgress());
                float wobble = Mathf.Sin(scanWobbleTime * Mathf.PI * 2f * scanWobbleFrequency) * scanWobbleAmplitude;
                desiredTilt = baseAngle + wobble;
                break;
        }

        currentTilt = desiredTilt;
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    // ---------- helpers ----------

    private void EnterPhase(Phase p, float duration)
    {
        phase = p;
        phaseDuration = Mathf.Max(0.0001f, duration);
        phaseTimer = phaseDuration;

        switch (phase)
        {
            case Phase.TiltUp:
                tiltStartAngle = currentTilt;
                tiltEndAngle = tiltBackAngle;
                break;

            case Phase.Jump:
                startYForHop = transform.position.y; // lock hop baseline
                tiltStartAngle = tiltBackAngle;
                tiltEndAngle = -tiltForwardAngle;
                break;

            case Phase.Scan:
                tiltStartAngle = currentTilt;
                tiltEndAngle = prePounceAngle;
                scanWobbleTime = 0f;
                break;

            case Phase.Leap:
                // Start from current tilt, rotate toward opposite of pre-pounce
                prevHopYOffset = 0f;
                tiltStartAngle = currentTilt;
                tiltEndAngle = -prePounceAngle;
                break;

            case Phase.TiltNeutral:
                tiltStartAngle = currentTilt;
                tiltEndAngle = 0f;
                break;

            default:
                tiltStartAngle = currentTilt;
                tiltEndAngle = currentTilt;
                break;
        }
    }

    private void AdvancePhase()
    {
        switch (phase)
        {
            case Phase.Pause1:
                EnterPhase(Phase.TiltUp, tiltUpDuration);
                break;

            case Phase.TiltUp:
                EnterPhase(Phase.Pause2, pauseAfterTiltUp);
                break;

            case Phase.Pause2:
                EnterPhase(Phase.Jump, hopDuration);
                break;

            case Phase.Jump:
                EnterPhase(Phase.TiltNeutral, tiltNeutralDuration);
                break;

            case Phase.TiltNeutral:
                {
                    // A hop is counted complete once neutral is reached
                    hopsCompleted++;

                    // Only enter Scan if it's time AND a valid target exists right now
                    bool timeToScan = (hopsPerScan > 0) && ((hopsCompleted % hopsPerScan) == 0);
                    if (timeToScan && HasAnyValidTargetLeft())
                        EnterPhase(Phase.Scan, scanDuration);
                    else
                        EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                    break;
                }

            case Phase.Scan:
                // Pick the CLOSEST live non-predator to the left *now*; leap if we have one
                if (PrepareLeapNowFromClosestTarget())
                    EnterPhase(Phase.Leap, /*timer unused*/ 1f);
                else
                    EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                break;

                // Note: Phase.Leap transitions are handled inside ComputeMove() (arrival/budget)
        }
    }

    /// Selects the closest non-predator to the LEFT within range *right now*,
    /// initializes leap budget and fallback direction. Returns true if ready to leap.
    private bool PrepareLeapNowFromClosestTarget()
    {
        leapTarget = null;
        leapBudgetStart = 0f;
        leapBudget = 0f;

        Vector3 myPos = transform.position;
        Animal[] all = FindObjectsOfType<Animal>();

        Animal best = null;
        float bestDist = float.MaxValue;

        foreach (var a in all)
        {
            if (a == null || a == this) continue;
            if (a.isPredator) continue;                       // only non-predators
            if (a.isLassoed) continue;                        // ignore immobilized
            if (a.transform.position.x >= myPos.x) continue;  // must be to the LEFT now

            float d = Vector3.Distance(a.transform.position, myPos);
            if (d <= detectionRange && d < bestDist)
            {
                bestDist = d;
                best = a;
            }
        }

        if (best == null) return false;

        leapTarget = best;

        // Distance budget from current distance (capped)
        leapBudgetStart = Mathf.Min(bestDist, Mathf.Max(0.01f, maxLeapDistance));
        leapBudget = leapBudgetStart;

        // Seed fallback direction
        Vector3 toTarget = best.transform.position - myPos;
        fallbackLeapDir = (toTarget.sqrMagnitude > 0.0001f) ? toTarget.normalized : Vector3.left;

        return true;
    }

    private bool HasAnyValidTargetLeft()
    {
        Vector3 myPos = transform.position;
        Animal[] all = FindObjectsOfType<Animal>();

        foreach (var a in all)
        {
            if (a == null || a == this) continue;
            if (a.isPredator) continue;                     // only non-predators
            if (a.isLassoed) continue;                      // ignore immobilized
            if (a.transform.position.x >= myPos.x) continue;// must be LEFT of puma

            if (Vector3.Distance(a.transform.position, myPos) <= detectionRange)
                return true;
        }
        return false;
    }

    private float PhaseProgress()
    {
        return 1f - Mathf.Clamp01(phaseTimer / Mathf.Max(0.0001f, phaseDuration));
    }

    private float LeapProgress()
    {
        if (leapBudgetStart <= 0f) return Mathf.PI; // safety
        float used = Mathf.Clamp01(1f - (leapBudget / leapBudgetStart));
        return used * Mathf.PI;
    }

    private float LeapProgress01()
    {
        if (leapBudgetStart <= 0f) return 1f;
        return Mathf.Clamp01(1f - (leapBudget / leapBudgetStart));
    }

    private void FaceDirection(int sign)
    {
        if (sign == facingSign) return;
        facingSign = sign;

        Vector3 sc = transform.localScale;
        float abs = Mathf.Abs(sc.x);
        sc.x = (sign < 0) ? abs : -abs;
        transform.localScale = sc;
    }
}
