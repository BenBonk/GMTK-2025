using UnityEngine;

public class Dog : Animal
{
    public float zigZagAngle = 30f;           // Degrees from horizontal
    public float zigZagDuration = 1.0f;       // Time per zig or zag

    private int verticalDirection = 1;        // 1 = up-left, -1 = down-left
    private float zigTimer;

    public float pushRadius = 1.5f;
    public float pushStrength = 1f;

    [Range(0f, 1f)] public float xPushFactor = 0.25f;
    [Range(0f, 1f)] public float yPushFactor = 1.0f;

    public override void Start()
    {
        base.Start();
        SetVerticalLimits();
        SetStartingEdge();

        zigTimer = zigZagDuration;
    }

    protected override Vector3 ComputeMove()
    {
        // Flip direction after duration
        zigTimer -= Time.deltaTime;
        if (zigTimer <= 0f)
        {
            verticalDirection *= -1;
            zigTimer = zigZagDuration;
        }

        // Calculate diagonal direction
        float angleRad = zigZagAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(-Mathf.Cos(angleRad), verticalDirection * Mathf.Sin(angleRad), 0f);

        Vector3 step = direction.normalized * currentSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + step;

        // Apply vertical external offset BEFORE clamping
        newPos.y += externalOffset.y;
        newPos.y = Mathf.Clamp(newPos.y, bottomLimitY, topLimitY);
        externalOffset.y = 0f; // prevent double-adding in Animal.Move()

        // Push others
        PushNearbyAnimals();

        return newPos;
    }

    private void SetVerticalLimits()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 top = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));
        Vector3 bottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));

        float halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;

        topLimitY = top.y - halfHeight;
        bottomLimitY = bottom.y + halfHeight;
    }

    private void SetStartingEdge()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float halfHeight = sr != null ? sr.bounds.extents.y : 0.5f; // fallback if not found
        float range = halfHeight; // 1 dog height range

        bool startAtTop = Random.value > 0.5f;

        float yMin = startAtTop ? (topLimitY - range) : (bottomLimitY);
        float yMax = startAtTop ? (topLimitY) : (bottomLimitY + range);

        Vector3 pos = transform.position;
        pos.y = Random.Range(yMin, yMax);
        transform.position = pos;

        verticalDirection = startAtTop ? -1 : 1;
    }

    private void PushNearbyAnimals()
    {
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (Animal other in allAnimals)
        {
            if (other == this || other.isLassoed)
                continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < pushRadius)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;

                float force = (1f - (dist / pushRadius)) * pushStrength;
                Vector3 scaledDirection = new Vector3(direction.x * xPushFactor, direction.y * yPushFactor, 0f).normalized;
                Vector3 push = scaledDirection * force * Time.deltaTime;

                other.ApplyExternalOffset(push);
            }
        }
    }
}
