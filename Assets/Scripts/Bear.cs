using UnityEngine;

public class Bear : Animal
{
    public float pauseDuration = 3f;
    public float attractionRadius = 6f;
    public float attractionStrength = 0.5f;
    public float acceleration = 0.5f; // How quickly to adjust speed
    public float stopThreshold = 0.05f;

    private enum BearState { MovingToCenter, Pausing, Exiting }
    private BearState state = BearState.MovingToCenter;

    private float moveDuration = 0f;
    private float moveTimer = 0f;
    private Vector3 moveDirection;

    private float pauseTimer = 0f;
    private float speedVelocity = 0f;
    private float speedTarget;

    [Range(1f, 2f)] public float decelerationDurationMultiplier = 1.25f;

    public override void Start()
    {
        base.Start();

        Vector3 centerTarget = new Vector3(0f, transform.position.y, transform.position.z);
        Vector3 delta = centerTarget - transform.position;

        moveDirection = delta.normalized;

        float distance = delta.magnitude;

        // Estimate duration, and increase it slightly to allow deceleration
        moveDuration = (distance / speed) * decelerationDurationMultiplier;
        moveTimer = moveDuration;

        speedTarget = speed;
        currentSpeed = speed;
    }


    protected override Vector3 ComputeMove()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, acceleration);

        switch (state)
        {
            case BearState.MovingToCenter:
                return MoveToCenterWithDeceleration();

            case BearState.Pausing:
                return PauseAtCenter();

            case BearState.Exiting:
                return ExitLeft();
        }

        return transform.position;
    }

    private Vector3 MoveToCenterWithDeceleration()
    {
        moveTimer -= Time.deltaTime;

        // Decelerate when nearing end of move
        float normalizedTime = Mathf.Clamp01(moveTimer / moveDuration);
        speedTarget = speed * normalizedTime;

        AttractNearbyAnimals();

        if (moveTimer <= 0f || currentSpeed < stopThreshold)
        {
            state = BearState.Pausing;
            pauseTimer = pauseDuration;
            currentSpeed = 0f;
            speedVelocity = 0f;
            return transform.position;
        }

        Vector3 step = moveDirection * currentSpeed * Time.deltaTime;
        return transform.position + step;
    }

    private Vector3 PauseAtCenter()
    {
        pauseTimer -= Time.deltaTime;

        AttractNearbyAnimals();

        if (pauseTimer <= 0f)
        {
            state = BearState.Exiting;
            speedTarget = speed;
        }

        return transform.position;
    }

    private Vector3 ExitLeft()
    {
        AttractNearbyAnimals();
        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }

    private void AttractNearbyAnimals()
    {
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (var other in allAnimals)
        {
            if (other == this || other.isLassoed)
                continue;

            // Skip attracting other bears
            if (other is Bear)
                continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < attractionRadius)
            {
                Vector3 direction = (transform.position - other.transform.position).normalized;
                float force = (1f - dist / attractionRadius) * attractionStrength;
                Vector3 offset = direction * force * Time.deltaTime;
                Vector3 smoothedOffset = Vector3.Lerp(Vector3.zero, offset, 0.25f);
                other.ApplyExternalOffset(smoothedOffset);
            }
        }
    }
}




