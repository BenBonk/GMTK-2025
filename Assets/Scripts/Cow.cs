using UnityEngine;

public class Cow : Animal
{
    [Header("Move & Pause Timing")]
    public float minMoveDuration = 2f;
    public float maxMoveDuration = 4f;
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.7f;

    [Header("Speed Control")]
    public float stopThreshold = 0.05f;

    private float speedTarget;
    private float speedVelocity = 0f;
    private float stateTimer = 0f;
    private bool isPaused = false;

    public override void Start()
    {
        base.Start();
        currentSpeed = speed;
        speedTarget = speed;
        stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
    }

    protected override Vector3 ComputeMove()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);
        stateTimer -= Time.deltaTime;

        if (isPaused)
        {
            if (stateTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed;
                stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                speedTarget = 0f;

                if (Mathf.Abs(currentSpeed) < stopThreshold)
                {
                    isPaused = true;
                    stateTimer = Random.Range(minPauseDuration, maxPauseDuration);
                }
            }
        }

        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }
}

