using UnityEngine;

public class Dog : Animal
{
    [Header("Zig-Zag")]
    public float zigZagAngle = 30f;           // degrees from horizontal
    public float zigZagDuration = 1.0f;       // time per zig or zag
    private int verticalDirection = 1;        // 1 = up-left, -1 = down-left
    private float zigTimer;

    [Header("Push")]
    public float pushRadius = 1.5f;
    public float pushStrength = 1f;
    [Range(0f, 1f)] public float xPushFactor = 0.25f;
    [Range(0f, 1f)] public float yPushFactor = 1.0f;

    [Header("Legendary Hunt")]
    public float detectionRadius = 5f;        // steer zig-zag toward nearest predator (must be LEFT of dog)
    public float chaseRadius = 2.5f;      // predators inside this are forced to LeaveScreen()
    [Range(0f, 1f)]
    public float approachSteer = 0.65f;     // 0 = pure zig-zag, 1 = pure pursuit while “approaching”

    // Legendary state
    private Animal chaseTarget = null;        // current active target for straight pursuit

    public override void Start()
    {
        base.Start();
        // Spawner controls initial position; no custom spawn Y here.
        // Initialize zig timer & initial vertical dir
        verticalDirection = (Random.value > 0.5f) ? +1 : -1;
        zigTimer = Mathf.Max(0.01f, zigZagDuration);
        if (!legendary)
        {
            SetStartingEdge();
        }
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


    public override void ActivateLegendary()
    {
        speed = 4.5f;
        currentSpeed = speed;
    }

    protected override Vector3 ComputeMove()
    {
        // --- Always push nearby animals regardless of legendary state ---
        // (We’ll call this at the end after we compose the new position.)
        // We'll compute newPos first.

        if (!legendary)
        {
            // --------- Non-legendary: your original zig-zag ----------
            TickZigTimer();
            Vector3 dir = ZigDir();
            Vector3 step = dir * currentSpeed * Time.deltaTime;

            Vector3 newPos = transform.position + step;

            // Respect vertical external offset, then clamp
            newPos.y += externalOffset.y;
            newPos.y = Mathf.Clamp(newPos.y, bottomLimitY, topLimitY);
            externalOffset.y = 0f;

            PushNearbyAnimals();
            return newPos;
        }

        // ------------- Legendary behavior -------------
        // (A) Force ALL predators inside chase radius to exit — EVERY FRAME
        ForcePredatorsToLeaveInChaseRadius();

        // (B) Pick active chase target if any predator is currently inside chase radius (to the LEFT preferred, but we can accept any)
        Animal nearestChase = FindNearestPredatorInRadius(chaseRadius, requireLeft: false);
        if (nearestChase != null)
        {
            chaseTarget = nearestChase;
        }
        else
        {
            // If no one in chase radius, clear sticky target
            chaseTarget = null;
        }

        Vector3 pos = transform.position;

        if (chaseTarget != null && !chaseTarget.isLassoed && !chaseTarget.forceExit)
        {
            // (C) CHASE mode: go straight toward the active target (no zig-zag)
            Vector3 dir = (chaseTarget.transform.position - pos).normalized;
            Vector3 newPos = pos + dir * currentSpeed * Time.deltaTime;

            // Apply vertical external offset before clamp
            newPos.y += externalOffset.y;
            newPos.y = Mathf.Clamp(newPos.y, bottomLimitY, topLimitY);
            externalOffset.y = 0f;

            PushNearbyAnimals();
            return newPos;
        }
        else
        {
            // (D) APPROACH / FREE ZIG-ZAG
            // Try to “steer” toward the nearest predator to the LEFT within detection radius
            Animal detected = FindNearestPredatorInRadius(detectionRadius, requireLeft: true);

            TickZigTimer();
            Vector3 zig = ZigDir();

            Vector3 chosenDir = zig;
            if (detected != null)
            {
                Vector3 toPred = (detected.transform.position - pos).normalized;
                // Blend zig-zag with pursuit direction
                chosenDir = Vector3.Slerp(zig, toPred, Mathf.Clamp01(approachSteer)).normalized;
            }

            Vector3 newPos = pos + chosenDir * currentSpeed * Time.deltaTime;

            // Apply vertical external offset before clamp
            newPos.y += externalOffset.y;
            newPos.y = Mathf.Clamp(newPos.y, bottomLimitY, topLimitY);
            externalOffset.y = 0f;

            PushNearbyAnimals();
            return newPos;
        }
    }

    // ---- Helpers ----

    private void TickZigTimer()
    {
        zigTimer -= Time.deltaTime;
        if (zigTimer <= 0f)
        {
            verticalDirection *= -1;
            zigTimer = Mathf.Max(0.01f, zigZagDuration);
        }
    }

    // Left-diagonal unit vector based on current verticalDirection and angle
    private Vector3 ZigDir()
    {
        float a = zigZagAngle * Mathf.Deg2Rad;
        Vector3 d = new Vector3(-Mathf.Cos(a), verticalDirection * Mathf.Sin(a), 0f);
        return d.normalized;
    }

    /// Find nearest predator in radius; optionally require it to be left of the dog.
    private Animal FindNearestPredatorInRadius(float radius, bool requireLeft)
    {
        Animal[] all = FindObjectsOfType<Animal>();
        Vector3 myPos = transform.position;

        Animal best = null;
        float bestDist = float.MaxValue;

        foreach (var a in all)
        {
            if (a == null || a == this) continue;
            if (!a.isPredator) continue;
            if (a.isLassoed) continue;

            if (requireLeft && a.transform.position.x >= myPos.x)
                continue;

            float d = Vector3.Distance(a.transform.position, myPos);
            if (d <= radius && d < bestDist)
            {
                bestDist = d;
                best = a;
            }
        }
        return best;
    }

    /// Force any predators currently inside chaseRadius to exit (every frame while legendary).
    private void ForcePredatorsToLeaveInChaseRadius()
    {
        Animal[] all = FindObjectsOfType<Animal>();
        Vector3 myPos = transform.position;

        foreach (var a in all)
        {
            if (a == null || a == this) continue;
            if (!a.isPredator) continue;
            if (a.isLassoed) continue;

            float d = Vector3.Distance(a.transform.position, myPos);
            if (d <= chaseRadius)
            {
                a.forceExit = true; // Animal.Move() will route to LeaveScreen()
            }
        }
    }

    private void PushNearbyAnimals()
    {
        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (Animal other in allAnimals)
        {
            if (other == this || other.isLassoed || other.IsRepelImmune)
                continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < pushRadius)
            {
                Vector3 toOther = (other.transform.position - transform.position);
                Vector3 dir = toOther.normalized;

                float force = (1f - (dist / pushRadius)) * pushStrength;

                // emphasize vertical push, soften horizontal
                Vector3 scaled = new Vector3(dir.x * xPushFactor, dir.y * yPushFactor, 0f).normalized;
                Vector3 push = scaled * force * Time.deltaTime;

                other.ApplyExternalOffset(push);
            }
        }
    }
}
