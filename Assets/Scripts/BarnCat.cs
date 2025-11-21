using UnityEngine;

public class BarnCat : Animal
{
    public float hopSpeed = 3.0f;
    public float hopHeight = 1.0f;
    public float hopDuration = 0.55f;

    public float pauseBeforeTiltUp = 0.15f;
    public float tiltUpDuration = 0.10f;
    public float pauseAfterTiltUp = 0.10f;
    public float tiltNeutralDuration = 0.18f;

    public int hopsPerLongRest = 3;
    public float longRestDuration = 2.5f;

    public float tiltBackAngle = 16f;
    public float tiltForwardAngle = 16f;

    public float verticalDriftSpeed = 0.25f;
    public float driftEdgeMargin = 0.05f;
    public float edgeBiasMargin = 0.5f;

    // baselines
    float baseHopSpeed, baseHopDuration;
    float basePauseBeforeTiltUp, baseTiltUpDuration, basePauseAfterTiltUp, baseTiltNeutralDuration;
    float baseLongRestDuration;

    const float EXP_SPEED = 0.5f; // hop distance grows
    const float EXP_TIME = 0.5f; // phase times shrink
    const float EXP_REST = 0.7f; // rests shrink a bit more

    private enum Phase { Pause1, TiltUp, Pause2, Jump, TiltNeutral, LongRest }
    private Phase phase = Phase.Pause1;

    private float phaseTimer;        
    private float phaseDuration;      
    private float startYForJump;      // baseline Y at the start of this hop
    private int hopsCompleted = 0;  // increments AFTER TiltNeutral completes

    // tilt
    private float currentTilt = 0f;
    private float tiltStartAngle = 0f;
    private float tiltEndAngle = 0f;

    // drift (normal mode)
    private int driftDir = 1;     // 1 = up, -1 = down
    private float driftOffset = 0f;    // accumulated drift relative to startYForJump

    // --- chase mode ---
    private bool isChasing = false;
    private Mouse chaseTarget = null;

    private int facingSign = -1; // -1 = face left, +1 = face right
    private float prevHopYOffset = 0f;
    private bool leavingScreen = false;

    protected override void Awake()
    {
        base.Awake();
        baseHopSpeed = hopSpeed;
        baseHopDuration = hopDuration;
        basePauseBeforeTiltUp = pauseBeforeTiltUp;
        baseTiltUpDuration = tiltUpDuration;
        basePauseAfterTiltUp = pauseAfterTiltUp;
        baseTiltNeutralDuration = tiltNeutralDuration;
        baseLongRestDuration = longRestDuration;
        FaceDirection(-1); // ensure we start facing left before first frame
    }

    public override void Start()
    {
        base.Start();
        ApplyEffectiveSpeedScale(_effectiveSpeedScale);
        EnterPhase(Phase.Pause1, pauseBeforeTiltUp); // start paused & neutral
    }

    protected override void ApplyEffectiveSpeedScale(float scale)
    {
        float speedMul = Mathf.Pow(scale, EXP_SPEED);
        float timeDiv = Mathf.Pow(scale, EXP_TIME);
        float restDiv = Mathf.Pow(scale, EXP_REST);

        hopSpeed = baseHopSpeed * speedMul;

        hopDuration = Mathf.Max(0.03f, baseHopDuration / timeDiv);
        pauseBeforeTiltUp = Mathf.Max(0.01f, basePauseBeforeTiltUp / timeDiv);
        tiltUpDuration = Mathf.Max(0.01f, baseTiltUpDuration / timeDiv);
        pauseAfterTiltUp = Mathf.Max(0.01f, basePauseAfterTiltUp / timeDiv);
        tiltNeutralDuration = Mathf.Max(0.01f, baseTiltNeutralDuration / timeDiv);

        longRestDuration = Mathf.Max(0.05f, baseLongRestDuration / restDiv);
    }

    protected override Vector3 ComputeMove()
    {
        // Validate chase target each frame
        if (isChasing && (chaseTarget == null || chaseTarget.isLassoed))
        {
            isChasing = false;
            chaseTarget = null;
        }

        float prog = PhaseProgress(); // 0..1 within current phase
        Vector3 pos = transform.position;

        switch (phase)
        {
            case Phase.Jump:
                {
                    float yOffset = Mathf.Sin(prog * Mathf.PI) * hopHeight;

                    if (isChasing && chaseTarget != null)
                    {
                        // Move directly toward the mouse
                        Vector3 dir = chaseTarget.transform.position - pos;
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            dir.Normalize();

                            // Flip when there is meaningful horizontal intent
                            if (Mathf.Abs(dir.x) > 0.05f) FaceDirection(dir.x >= 0f ? +1 : -1);

                            // Base pursuit step
                            pos += dir * hopSpeed * Time.deltaTime;

                            // Apply hop bob
                            float bobDelta = yOffset - prevHopYOffset;
                            prevHopYOffset = yOffset;
                            pos.y += bobDelta;
                        }
                        else
                        {
                            // If exactly on target, just apply delta bob in place
                            float bobDelta = yOffset - prevHopYOffset;
                            prevHopYOffset = yOffset;
                            pos.y += bobDelta;
                        }
                    }
                    else
                    {
                        // --- NORMAL MODE: move left with vertical drift during the hop ---
                        // choose/update drift
                        driftOffset += verticalDriftSpeed * driftDir * Time.deltaTime;

                        float nextY = startYForJump + yOffset + driftOffset;

                        // bounce at vertical limits
                        if (nextY >= topLimitY - driftEdgeMargin)
                        {
                            nextY = topLimitY - driftEdgeMargin;
                            driftDir = -1;
                            driftOffset = nextY - startYForJump - yOffset;
                        }
                        else if (nextY <= bottomLimitY + driftEdgeMargin)
                        {
                            nextY = bottomLimitY + driftEdgeMargin;
                            driftDir = 1;
                            driftOffset = nextY - startYForJump - yOffset;
                        }

                        // move left only in normal mode
                        FaceDirection(-1);
                        pos += Vector3.left * hopSpeed * Time.deltaTime;
                        pos.y = nextY;
                    }
                    break;
                }

            case Phase.LongRest:
                // During long rest, try to acquire a mouse
                if (TryAcquireMouseTarget())
                {
                    isChasing = true;
                    EnterPhase(Phase.Pause1, pauseBeforeTiltUp); // resume hopping cycle (no long rest while chasing)
                }
                break;

            default:
                // all other phases are full stop
                break;
        }

        // advance timer & transitions
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
            AdvancePhase();

        return pos; // Animal.Move() will add externalOffset, clamp Y, and clear the offset
    }

    protected override void ApplyRunTilt()
    {
        if (leavingScreen)
        {
            base.ApplyRunTilt();
            return;
        }
        float prog = PhaseProgress(); // 0..1
        float desiredTilt = currentTilt;

        switch (phase)
        {
            case Phase.Pause1:
                desiredTilt = 0f; // neutral
                break;

            case Phase.TiltUp:
                desiredTilt = Mathf.Lerp(tiltStartAngle, tiltBackAngle, prog);
                break;

            case Phase.Pause2:
                desiredTilt = tiltBackAngle; // hold back
                break;

            case Phase.Jump:
                // tilt back -> forward across the jump
                desiredTilt = Mathf.Lerp(tiltBackAngle, -tiltForwardAngle, prog);
                break;

            case Phase.TiltNeutral:
                desiredTilt = Mathf.Lerp(tiltStartAngle, 0f, prog);
                break;

            case Phase.LongRest:
                desiredTilt = 0f; // rest neutral
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
                startYForJump = transform.position.y; // lock hop baseline
                driftOffset = 0f;                   // normal-mode drift baseline
                driftDir = ChooseDriftDir();     // default drift direction (ignored while chasing)
                tiltStartAngle = tiltBackAngle;
                tiltEndAngle = -tiltForwardAngle;
                prevHopYOffset = 0f;                   // << reset delta-bob tracker
                break;

            case Phase.TiltNeutral:
                tiltStartAngle = currentTilt;
                tiltEndAngle = 0f;
                break;

            case Phase.LongRest:
                //hold neutral
                break;

            default:
                // pauses hold current angle
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
                hopsCompleted++;

                if (isChasing)
                {
                    // Never long-rest while chasing
                    EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                }
                else
                {
                    // Normal flow: time for a long rest?
                    if (hopsPerLongRest > 0 && (hopsCompleted % hopsPerLongRest) == 0)
                    {
                        // Try to acquire a mouse first
                        if (TryAcquireMouseTarget())
                        {
                            isChasing = true;
                            EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                        }
                        else
                        {
                            EnterPhase(Phase.LongRest, longRestDuration);
                        }
                    }
                    else
                    {
                        EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                    }
                }
                break;

            case Phase.LongRest:
                // long rest finished, reset loop
                EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
                break;
        }
    }

    private float PhaseProgress()
    {
        return 1f - Mathf.Clamp01(phaseTimer / Mathf.Max(0.0001f, phaseDuration));
    }

    private int ChooseDriftDir()
    {
        // During normal mode, bias drift away from edges
        float y = transform.position.y;
        if (y <= bottomLimitY + edgeBiasMargin) return 1;   // go up
        if (y >= topLimitY - edgeBiasMargin) return -1;  // go down
        return Random.value > 0.5f ? 1 : -1;                // random
    }

    private bool TryAcquireMouseTarget()
    {
        Mouse[] mice = FindObjectsOfType<Mouse>();
        Mouse best = null;
        float bestDx = float.MaxValue;

        foreach (var m in mice)
        {
            if (m == null || m.isLassoed) continue;

            // must be to the LEFT of the cat
            float dx = transform.position.x - m.transform.position.x;
            if (dx > 0f && dx < bestDx)
            {
                bestDx = dx;
                best = m;
            }
        }

        chaseTarget = best;
        return chaseTarget != null;
    }

    private void FaceDirection(int sign)
    {
        // sign: -1 = face left, +1 = face right
        if (sign == facingSign) return;
        facingSign = sign;

        Vector3 sc = transform.localScale;
        float abs = Mathf.Abs(sc.x);
        sc.x = (sign < 0) ? abs : -abs;
        transform.localScale = sc;
    }

    public override Vector3 LeaveScreen()
    {
        tiltFrequency = 8;
        maxTiltAmplitude = 6;
        currentSpeed = 3;
        leavingScreen = true;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        return transform.position + Vector3.left * 5 * Time.deltaTime;
    }
}
