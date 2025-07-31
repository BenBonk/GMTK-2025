using UnityEngine;

public class Goat : Animal
{
    public float minMoveDuration = 2f;
    public float maxMoveDuration = 4f;
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.7f;

    public float stopThreshold = 0.05f;

    public float maxAngleFromLeft = 45f; // Limit direction spread

    public float predatorSlowRadius = 2.5f;
    public float predatorSpeedMultiplier = 0.5f; // 0.5 = 50% speed

    private Vector3 moveDirection;
    private float stateTimer = 0f;
    private float speedTarget;
    private float speedVelocity = 0f;
    private bool isPaused = false;

    public override void Start()
    {
        base.Start();
        PickNewDirection();
        speedTarget = speed;
        currentSpeed = speed;
        stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
    }

    protected override Vector3 ComputeMove()
    {
        SlowNearbyPredators();
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);
        stateTimer -= Time.deltaTime;

        if (isPaused)
        {
            if (stateTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed;
                PickNewDirection();
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

        // Move in the chosen direction
        Vector3 nextPos = transform.position + moveDirection * currentSpeed * Time.deltaTime;

        return nextPos;
    }

    private void PickNewDirection()
    {
        // Choose a direction based on a cone angled left
        float angle = Random.Range(-maxAngleFromLeft, maxAngleFromLeft);
        float radians = angle * Mathf.Deg2Rad;
        moveDirection = new Vector3(-Mathf.Cos(radians), Mathf.Sin(radians), 0f).normalized;
    }

    private void SlowNearbyPredators()
    {
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (Animal other in allAnimals)
        {
            if (other == this || !other.isPredator || other.isLassoed)
                continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist <= predatorSlowRadius)
            {
                // Reduce current speed (but don’t let it go negative or too low)
                other.currentSpeed = Mathf.Min(other.currentSpeed, other.speed * predatorSpeedMultiplier);
            }
        }
    }
}
