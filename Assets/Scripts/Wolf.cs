using UnityEngine;

public class Wolf : Animal
{
    private bool hasTarget = false;
    private GameObject targetAnimal;
    private float targetDetectionRange = 5f;
    private float stoppingDistance = 1f;

    public float acceleration = 0.5f;

    private float speedTarget;
    private float speedVelocity = 0f;

    public override void Start()
    {
        base.Start();
        speedTarget = speed;
    }

    protected override Vector3 ComputeMove()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, acceleration);

        if (hasTarget && targetAnimal != null && !targetAnimal.GetComponent<Animal>().isLassoed)
        {
            Vector3 dir = (targetAnimal.transform.position - transform.position).normalized;
            Vector3 nextPos = transform.position + dir * currentSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetAnimal.transform.position) <= stoppingDistance)
                speedTarget = 0f;
            else
                speedTarget = speed;

            return nextPos;
        }
        else
        {
            FindTarget();
            return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
        }
    }

    private void FindTarget()
    {
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
            targetAnimal = closest;
            hasTarget = true;
        }
    }
}


