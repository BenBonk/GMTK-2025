using UnityEngine;

public class Bunny : Animal
{
    [Header("Jump Motion")]
    public float hopSpeed = 3.2f;       // horizontal speed during jump
    public float hopHeight = 1.2f;      // arc height
    public float hopDuration = 0.55f;   // time from takeoff to landing

    [Header("Phase Durations")]
    public float pauseBeforeTiltUp = 0.15f;   // pause (no tilt)
    public float tiltUpDuration = 0.12f;      // tilt up while paused
    public float pauseAfterTiltUp = 0.10f;    // pause, holding tilt up
    public float pauseAfterJump = 0.15f;      // pause, holding tilt down
    public float tiltNeutralDuration = 0.18f; // tilt back to neutral while paused

    [Header("Tilt Angles")]
    public float tiltBackAngle = -18f;         // lean back (takeoff)
    public float tiltForwardAngle = -18f;      // lean forward (landing)

    private enum Phase { Pause1, TiltUp, Pause2, Jump, Pause3, TiltNeutral }
    private Phase phase = Phase.Pause1;

    private float phaseTimer;       // counts down
    private float phaseDuration;    // fixed per phase
    private float startYForJump;    // baseline for current jump arc

    // tilt interpolation
    private float currentTilt = 0f;
    private float tiltStartAngle = 0f;
    private float tiltEndAngle = 0f;

    private bool leavingScreen = false;

    public override void Start()
    {
        base.Start();
        // start at neutral, initial pause
        EnterPhase(Phase.Pause1, pauseBeforeTiltUp);
    }

    protected override Vector3 ComputeMove()
    {
        // Compute phase progress 
        float prog = PhaseProgress(); // 0..1

        // Base position for this frame
        Vector3 pos = transform.position;

        switch (phase)
        {
            case Phase.Jump:
                // vertical arc (smooth up/down)
                float yOffset = Mathf.Sin(prog * Mathf.PI) * hopHeight;
                pos += Vector3.left * hopSpeed * Time.deltaTime; // only move in Jump
                pos.y = startYForJump + yOffset;
                break;

            default:
                break;
        }

        // Advance timer & transition if done
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
            AdvancePhase();

        // Return pos; base Move() will add any externalOffset and clamp Y.
        return pos;
    }

    protected override void ApplyRunTilt()
    {
        if (leavingScreen)
        {
            base.ApplyRunTilt();
            return;
        }
        float prog = PhaseProgress(); // 0..1 within current phase

        float desiredTilt = currentTilt; // default: hold

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
                // tilt down over the duration of the jump
                desiredTilt = Mathf.Lerp(tiltBackAngle, -tiltForwardAngle, prog);
                break;

            case Phase.Pause3:
                desiredTilt = -tiltForwardAngle; // hold forward
                break;

            case Phase.TiltNeutral:
                desiredTilt = Mathf.Lerp(tiltStartAngle, 0f, prog);
                break;
        }

        currentTilt = desiredTilt;
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    private void EnterPhase(Phase p, float duration)
    {
        phase = p;
        phaseDuration = Mathf.Max(0.0001f, duration);
        phaseTimer = phaseDuration;

        // set up tilt interpolation endpoints for phases that animate rotation
        switch (phase)
        {
            case Phase.TiltUp:
                tiltStartAngle = currentTilt;          // usually 0
                tiltEndAngle = tiltBackAngle;
                break;

            case Phase.Jump:
                startYForJump = transform.position.y;  // lock jump baseline
                tiltStartAngle = tiltBackAngle;
                tiltEndAngle = -tiltForwardAngle;
                break;

            case Phase.TiltNeutral:
                tiltStartAngle = currentTilt;          // usually -tiltForwardAngle
                tiltEndAngle = 0f;
                break;

            default:
                // hold angle during pauses
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
                EnterPhase(Phase.Pause3, pauseAfterJump);
                break;

            case Phase.Pause3:
                EnterPhase(Phase.TiltNeutral, tiltNeutralDuration);
                break;

            case Phase.TiltNeutral:
                EnterPhase(Phase.Pause1, pauseBeforeTiltUp); // loop
                break;
        }
    }

    private float PhaseProgress()
    {
        // 0 at phase start, 1 at phase end
        return 1f - Mathf.Clamp01(phaseTimer / Mathf.Max(0.0001f, phaseDuration));
    }

    public override Vector3 LeaveScreen()
    {
        tiltFrequency = 6;
        maxTiltAmplitude = 8;
        currentSpeed = 3;
        leavingScreen = true;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        return transform.position + Vector3.left * 5 * Time.deltaTime;
    }
}

