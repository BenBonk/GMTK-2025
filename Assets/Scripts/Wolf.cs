using UnityEngine;

public class Wolf : Animal
{
    private bool hasTarget = false;
    private Animal targetAnimal;
    private float targetDetectionRange = 5f;
    private float stoppingDistance = 1f;

    public float acceleration = 0.5f;

    private float speedTarget;
    private float speedVelocity = 0f;

    private float baseSpeed;

    public override void Start()
    {
        base.Start();
        currentSpeed = speed * _effectiveSpeedScale;
    }

    protected override Vector3 ComputeMove()
    {
        // always target the effective scaled speed by default
        float desired = speed * _effectiveSpeedScale;

        // if we are at stopping distance, desired becomes 0 (only while close)
        bool chasing = hasTarget && targetAnimal != null && !targetAnimal.isLassoed;
        if (chasing)
        {
            float dist = Vector3.Distance(transform.position, targetAnimal.transform.position);
            if (dist <= stoppingDistance) desired = 0f;
        }

        // Smoothly move currentSpeed toward the desired (scaled) speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, desired, ref speedVelocity, acceleration);

        if (chasing)
        {
            Vector3 dir = (targetAnimal.transform.position - transform.position).normalized;
            if (Mathf.Abs(dir.x) > 0.01f) FaceByX(dir.x);

            float step = currentSpeed * Time.deltaTime;
            float dist = Vector3.Distance(transform.position, targetAnimal.transform.position);
            if (dist <= stoppingDistance) step = 0f; // stop at the target

            return transform.position + dir * step;
        }
        else
        {
            FindTarget();
            return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
        }
    }


    private void FindTarget()
    {
    //a
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        float closestDistance = Mathf.Infinity;
        GameObject closest = null;

        foreach (var a in allAnimals)
        {
            if (a == this || a.isPredator || a.transform.position.x >= transform.position.x)
                continue;

            float dist = Vector3.Distance(transform.position, a.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = a.gameObject;
            }
        }

        if (closest != null)
        {
            targetAnimal = closest.GetComponent<Animal>();
            hasTarget = true;
        }
    }
}


