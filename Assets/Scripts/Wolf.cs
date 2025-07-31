using UnityEngine;

public class Wolf : Animal
{
    private bool hasTarget = false;
    private GameObject targetAnimal;
    private float targetDetectionRange = 5f; // Range within which the wolf can detect prey
    private float stoppingDistance = 1f; // Distance at which the wolf slows down 
    public float  acceleration = 0.5f; // how quickly to accelerate

    private float speedTarget;
    private float speedVelocity = 0f; // used internally by SmoothDamp
    public override void Move()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, acceleration);
        if (hasTarget && !targetAnimal.GetComponent<Animal>().isLassoed)
        {
            Vector3 direction = (targetAnimal.transform.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetAnimal.transform.position) <= stoppingDistance)
            {
                // If close enough to the target, stop moving
                speedTarget = 0f; // Stop moving towards the target
            }
            else
            {
                // If not close enough, continue moving towards the target
                speedTarget = speed;
            }

        }
        else
        {
            traveled += speed * Time.deltaTime;
            float x = startPos.x - traveled;

            transform.position = new Vector3(x, startPos.y, startPos.z);
            FindTarget();
        }
    }

    private void FindTarget()
    {
        // find the closest non-preadator animal to chase
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        float closestDistance = Mathf.Infinity;
        GameObject closestTarget = null;

        foreach (Animal animal in allAnimals)
        {
            if (animal == this)
                continue;

            // Skip if it's a predator
            if (animal.isPredator)
                continue;

            // Only consider animals to the left (ahead)
            if (animal.transform.position.x >= transform.position.x)
                continue;

            float distance = Vector3.Distance(transform.position, animal.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = animal.gameObject;
            }
        }

        if (closestTarget != null)
        {
            targetAnimal = closestTarget;
            hasTarget = true;
        }
    }
}