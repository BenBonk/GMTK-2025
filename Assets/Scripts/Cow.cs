using DG.Tweening;
using UnityEngine;

public class Cow : Animal
{
    [Header("Move & Pause Timing")]
    public float minMoveDuration = 2f;
    public float maxMoveDuration = 4f;
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.7f; // how smoothly to transition

    [Header("Speed Control")]
    public float stopThreshold = 0.05f; // consider "paused" when speed is below this

    private float currentSpeed;
    private float speedTarget;

    private float stateTimer = 0f;
    private bool isPaused = false;

    private float speedVelocity = 0f; // used internally by SmoothDamp

    private void Start()
    {
        base.Start();

        currentSpeed = speed;
        speedTarget = speed;
        stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
    }

    public override void Move()
    {

        // Smooth speed transition
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);

        stateTimer -= Time.deltaTime;

        if (isPaused)
        {
            // Paused: wait until timer finishes, then accelerate
            if (stateTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed;
                stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
            }
        }
        else
        {
            // Moving: apply horizontal movement
            float step = currentSpeed * Time.deltaTime;
            traveled += step;

            float x = startPos.x - traveled;
            float y = startPos.y;

            transform.position = new Vector3(x, y, startPos.z);

            // If move duration is up, start slowing down
            if (stateTimer <= 0f)
            {
                speedTarget = 0f;

                // Wait to actually enter pause state until we're nearly stopped
                if (Mathf.Abs(currentSpeed) < stopThreshold)
                {
                    isPaused = true;
                    stateTimer = Random.Range(minPauseDuration, maxPauseDuration);
                }
            }
        }
    }
}
