using UnityEngine;

public class Horse : Animal
{
    public float decelDistance = 2.0f;
    public float StopOffsetMin = 1f;
    public float StopOffsetMax = 3f;
    private float stopOffsetFromEdge = 2f;
    // SmoothDamp time (used for both brake & accel in this minimal version)
    public float smoothTime = 0.35f;
    public float stopThreshold = 0.02f;
    public float pauseAtEdge = 0.75f;
    public float exitSpeedMultiplier = 1.0f;

    private enum State { Approach, StopPause, AccelerateExit }
    private State state = State.Approach;

    private float speedTarget;
    private float speedVelocity;
    private float leftScreenX;
    private float pauseTimer;


    public override void Start()
    {
        base.Start();
        leftScreenX = leftEdgeX + 1f;
        stopOffsetFromEdge = Random.Range(StopOffsetMin, StopOffsetMax);
    }

    public override void ActivateLegendary()
    {
        speed *= 0.35f;
        currentSpeed = speed;
    }

    protected override Vector3 ComputeMove()
    {
        if (!legendary)
            return base.ComputeMove();

        float stopX = leftScreenX + stopOffsetFromEdge;
        Vector3 pos = transform.position;

        switch (state)
        {
            case State.Approach:
                {
                    float distToStop = pos.x - stopX;
                    bool withinBrake = distToStop <= decelDistance;

                    speedTarget = withinBrake ? 0f : speed;
                    currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, smoothTime);

                    float predictedX = (pos + Vector3.left * currentSpeed * Time.deltaTime + externalOffset).x;
                    Vector3 next = pos + Vector3.left * currentSpeed * Time.deltaTime;

                    bool atLine = predictedX <= (stopX + 0.02f);
                    bool nearlyStill = (speedTarget == 0f && currentSpeed <= stopThreshold);

                    if (withinBrake && (atLine || nearlyStill))
                    {
                        currentSpeed = 0f;
                        speedVelocity = 0f;
                        pauseTimer = pauseAtEdge;
                        state = State.StopPause;
                    }

                    return next;
                }

            case State.StopPause:
                {
                    pauseTimer -= Time.deltaTime;
                    if (pauseTimer <= 0f)
                    {
                        speedTarget = speed * Mathf.Max(0f, exitSpeedMultiplier);
                        speedVelocity = 0f;
                        state = State.AccelerateExit;
                    }
                    return pos; // allow pushes while paused
                }

            case State.AccelerateExit:
                {
                    currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, smoothTime);
                    return pos + Vector3.left * currentSpeed * Time.deltaTime;
                }
        }

        return pos + Vector3.left * currentSpeed * Time.deltaTime;
    }
}