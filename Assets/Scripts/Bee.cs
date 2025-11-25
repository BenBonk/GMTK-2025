using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Bee : MonoBehaviour
{
    public bool flip = false;
    // ---------- Patrol ----------
    [Header("Patrol")]
    public float speed = 3f;
    public float waveAmplitude = 1f;
    public float waveFrequency = 2f;
    public float tiltFrequency = 3f;
    public float maxTiltAmplitude = 20f;

    // ---------- Targeting ----------
    [Header("Targeting")]
    public float detectRadius = 3.0f;
    public float minDetectRadius = 0.75f;
    public bool includePredators = true;      // if false, skip Animal.isPredator
    public float retargetCooldown = 0.6f;
    [Range(0f, 180f)] public float frontConeAngle = 60f; // vision cone in front
    [Tooltip("Time after spawn before targeting is allowed")]
    public float spawnTargetDelay = 0.5f;

    // ---------- Hover & Attack ----------
    [Header("Hover & Attack")]
    public float hoverHeight = 1.25f;
    public float hoverSpeed = 6f;
    public float alignRotSpeed = 720f;
    public Vector2 diveDelayRange = new Vector2(0.25f, 0.6f);
    public float pullBackDistance = 0.35f;
    public float pullBackTime = 0.25f;
    public float diveSpeed = 12f;
    public float diveAccel = 35f;

    // ---------- Stinger ----------
    [Header("Stinger Orientation")]
    [Tooltip("Local-space direction pointing out of the stinger tip in the sprite art")]
    public Vector2 localStingerDir = new Vector2(-1f, 0f); // left in art

    // ---------- After Sting ----------
    [Header("After Sting")]
    public bool destroyAfterSting = false;
    public float recoverUpKick = 4f;

    [Serializable] public class StingEvent : UnityEngine.Events.UnityEvent<Animal> { }
    public StingEvent OnSting;
    public GameObject beePoof;

    // ---------- State ----------
    private enum BeeState { Patrol, Stalking, PreDive, PullBack, Diving, Recover }
    private BeeState state = BeeState.Patrol;

    // patrol internals
    private float currentSpeed;
    private float waveProgress, startY;
    private bool initialized;

    // timers
    private float spawnTimer;
    private float retargetTimer;

    // target/points
    private Animal target;
    private Vector3 hoverPoint;

    // pullback
    private Vector3 pullStart, pullEnd, pullDir;
    private float pullTimer;

    // dive
    private float diveVel;      // also reused as the pre-dive delay countdown holder
    private float recoverVy;

    // tilt calc
    private float tiltProgress;
    private Vector3 previousPos;

    // camera clamp
    private float topLimitY, bottomLimitY;

    // facing
    private bool facingRight = true;

    public static event Action<Animal> OnAnyBeeSting;


    // ---------- Unity ----------
    private void Awake()
    {
        ComputeVerticalLimits();
    }

    private void Start()
    {
        var cam = Camera.main;
        if (cam)
        {
            var midY = Screen.height * 0.5f;
            var leftEdge  = cam.ScreenToWorldPoint(new Vector3(0, midY, cam.nearClipPlane));
            var rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, midY, cam.nearClipPlane));

            Vector3 startPos;

            if (!flip)
                startPos = new Vector3(leftEdge.x - 1f, transform.position.y, transform.position.z);   // L → R
            else
                startPos = new Vector3(rightEdge.x + 1f, transform.position.y, transform.position.z);  // R → L

            transform.position = startPos;
        }

        currentSpeed = speed;
        spawnTimer = spawnTargetDelay;

        startY = ClampY(transform.position.y, margin: waveAmplitude);
        previousPos = transform.position;

        SetFacingRight(!flip);   // new: if flipped, start facing left
    }


    private void Update()
    {
        if (spawnTimer > 0f) spawnTimer -= Time.deltaTime;
        if (retargetTimer > 0f) retargetTimer -= Time.deltaTime;

        Vector3 next = transform.position;

        switch (state)
        {
            case BeeState.Patrol:
                TryAcquireTarget();
                next = PatrolMove();
                break;

            case BeeState.Stalking:
                if (TargetInvalidOrLassoed()) { LoseTarget(); next = PatrolMove(); break; }
                next = StalkMove();
                break;

            case BeeState.PreDive:
                if (TargetInvalidOrLassoed()) { LoseTarget(); next = PatrolMove(); break; }
                next = PreDiveMove();
                break;

            case BeeState.PullBack:
                if (TargetInvalidOrLassoed()) { LoseTarget(); next = PatrolMove(); break; }
                next = PullBackMove();
                break;

            case BeeState.Diving:
                if (TargetInvalidOrLassoed()) { LoseTarget(); next = PatrolMove(); break; }
                next = DiveMove();
                break;

            case BeeState.Recover:
                next = RecoverMove();
                break;
        }

        // clamp Y to camera view
        next.y = Mathf.Clamp(next.y, bottomLimitY, topLimitY);

        // apply motion
        transform.position = next;
    }

    private void LateUpdate()
    {
        // compute actual speed magnitude for tilt scaling
        float actualSpeed = (transform.position - previousPos).magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
        previousPos = transform.position;

        switch (state)
        {
            case BeeState.Patrol:
            case BeeState.Stalking:
                // base "run tilt": sine wobble scaled by speed ratio
                tiltProgress += Time.deltaTime * tiltFrequency;
                float denom = Mathf.Max(0.0001f, Mathf.Abs(speed));
                float speedFactor = Mathf.Clamp01(actualSpeed / denom);
                float amplitude = maxTiltAmplitude * speedFactor;
                float angle = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * amplitude;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                break;

            case BeeState.PreDive:
            case BeeState.PullBack:
            case BeeState.Diving:
                // aim gradually at target
                AlignStingerToTarget(Time.deltaTime);
                break;

            case BeeState.Recover:
                // gentle wobble while recovering
                tiltProgress += Time.deltaTime * tiltFrequency;
                float angleR = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * (maxTiltAmplitude * 0.5f);
                transform.rotation = Quaternion.Euler(0f, 0f, angleR);
                break;
        }
    }

    // ---------- Movement ----------
    private Vector3 PatrolMove()
    {
        if (!initialized)
        {
            AdjustStartYToFitWave();
            initialized = true;
        }

        waveProgress += Time.deltaTime;
        float yOffset = Mathf.Sin(waveProgress * Mathf.PI * 2f * waveFrequency) * waveAmplitude;

        Vector3 dir = flip ? Vector3.left : Vector3.right;

        Vector3 next = transform.position + dir * currentSpeed * Time.deltaTime;
        next.y = startY + yOffset;

        return next;
    }


    private Vector3 StalkMove()
    {
        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        float targetTopY = GetTopY(target);
        Vector3 desired = new Vector3(tp.x, targetTopY + hoverHeight, p.z);

        // move toward hover point
        p = Vector3.MoveTowards(p, desired, hoverSpeed * Time.deltaTime);

        // flip ONLY if target is behind while stalking
        float xToTarget = tp.x - transform.position.x;
        bool targetIsRight = xToTarget > 0f;
        bool currentlyRight = transform.localScale.x > 0f;
        if (targetIsRight != currentlyRight)
            SetFacingRight(targetIsRight);

        bool nearX = Mathf.Abs(p.x - desired.x) <= 0.05f;
        bool nearY = Mathf.Abs(p.y - desired.y) <= 0.05f;
        if (nearX && nearY)
        {
            hoverPoint = desired;
            state = BeeState.PreDive;

            // flip back right before hover to keep aiming stable
            SetFacingRight(true);

            // store hover delay in diveVel temporarily
            diveVel = UnityEngine.Random.Range(diveDelayRange.x, diveDelayRange.y);
        }

        return p;
    }

    private Vector3 PreDiveMove()
    {
        Vector3 p = hoverPoint;

        // countdown hover delay (stored in diveVel)
        diveVel -= Time.deltaTime;
        if (diveVel <= 0f)
        {
            state = BeeState.PullBack;
            pullTimer = pullBackTime;
            pullDir = -(target.transform.position - transform.position).normalized;
            pullStart = transform.position;
            pullEnd = pullStart + pullDir * pullBackDistance;
            SetFacingRight(true); // keep attack states facing right
        }

        return p;
    }

    private Vector3 PullBackMove()
    {
        pullTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(pullTimer / pullBackTime);
        Vector3 p = Vector3.Lerp(pullStart, pullEnd, t);

        if (pullTimer <= 0f)
        {
            state = BeeState.Diving;
            diveVel = 0f;
            SetFacingRight(true);
            AudioManager.Instance.PlaySFX("bee_attack");
        }

        return p;
    }

    private Vector3 DiveMove()
    {
        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        Vector2 aimDir = (tp - p).normalized;

        // accelerate toward target up to diveSpeed
        diveVel = Mathf.Min(diveSpeed, diveVel + diveAccel * Time.deltaTime);
        p += (Vector3)aimDir * (diveVel * Time.deltaTime);

        // hit test: bee pivot inside collider/bounds
        bool hit = false;
        var col = target.GetComponent<Collider2D>();
        if (col)
        {
            Vector2 closest = col.ClosestPoint(p);
            hit = ((Vector2)p - closest).sqrMagnitude <= 1e-8f;
        }
        else
        {
            var sr = target.GetComponent<SpriteRenderer>();
            if (sr) hit = sr.bounds.Contains(p);
        }

        if (hit)
        {
            OnAnyBeeSting?.Invoke(target);
            if (destroyAfterSting)
            {
                AudioManager.Instance.PlaySFX("bee_hit");
                Instantiate(beePoof, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
            else
            {
                state = BeeState.Recover;
                recoverVy = recoverUpKick;
            }
        }

        return p;
    }

    private Vector3 RecoverMove()
    {
        Vector3 p = transform.position;
        p.y += recoverVy * Time.deltaTime;
        recoverVy += -12f * Time.deltaTime;

        if (recoverVy <= 0f)
        {
            state = BeeState.Patrol;
            retargetTimer = retargetCooldown;

            // face right for patrol, re-anchor wave and fade amplitude in
            SetFacingRight(true);
            startY = ClampY(p.y, margin: waveAmplitude);
            waveProgress = 0f;
            StartCoroutine(RestoreWaveAmplitude());
        }

        return p;
    }

    // ---------- Targeting ----------
    private void TryAcquireTarget()
    {
        if (spawnTimer > 0f) return;        // <-- spawn delay enforced
        if (retargetTimer > 0f) return;

        Animal best = null;
        float bestScore = float.PositiveInfinity;

        Vector2 forward = VisionForward();  // <-- cone faces horizontal based on sprite flip
        float maxSqr = detectRadius * detectRadius;
        float minSqr = minDetectRadius * minDetectRadius;

        var all = FindObjectsOfType<Animal>();
        foreach (var a in all)
        {
            if (!IsCandidate(a)) continue;

            Vector2 to = a.transform.position - transform.position;
            float d2 = to.sqrMagnitude;
            if (d2 > maxSqr || d2 < minSqr) continue;

            float angle = Vector2.Angle(forward, to);
            if (angle > frontConeAngle * 0.5f) continue;

            // prioritize same Y-level, then proximity
            float yDiff = Mathf.Abs(a.transform.position.y - transform.position.y);
            float score = d2 + (yDiff * yDiff * 2f);

            if (score < bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        if (best != null)
        {
            target = best;
            state = BeeState.Stalking;
            AudioManager.Instance.PlaySFX("bee_notice");
        }
    }

    private bool IsCandidate(Animal a)
    {
        if (a == null || a.gameObject == null) return false;
        if (!a.gameObject.activeInHierarchy) return false;
        if (a.isLassoed) return false;
        if (!includePredators && a.isPredator) return false;
        return true;
    }

    private bool TargetInvalidOrLassoed()
    {
        return target == null || !target.gameObject.activeInHierarchy || target.isLassoed || (!includePredators && target.isPredator);
    }

    private void LoseTarget()
    {
        target = null;
        state = BeeState.Patrol;
        retargetTimer = retargetCooldown;
        diveVel = 0f;
        recoverVy = 0f;

        // face right and re-anchor patrol wave
        SetFacingRight(true);
        startY = ClampY(transform.position.y, margin: waveAmplitude);
        waveProgress = 0f;
        StartCoroutine(RestoreWaveAmplitude());
    }

    // ---------- Rotation / Aiming ----------
    private void AlignStingerToTarget(float dt)
    {
        if (!target) return;

        Vector2 aimDir = ((Vector2)(target.transform.position - transform.position)).normalized;
        if (aimDir.sqrMagnitude < 1e-6f) return;

        Vector2 stingerWorld = GetWorldStingerDir();
        Quaternion want = Quaternion.FromToRotation(stingerWorld, aimDir) * transform.rotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, alignRotSpeed * dt);
    }

    private Vector2 GetWorldStingerDir()
    {
        // local stinger dir aware of X flip (so aiming matches visual)
        Vector2 dir = localStingerDir.normalized;
        if (transform.localScale.x < 0f) dir.x = -dir.x;
        return ((Vector2)(transform.rotation * (Vector3)dir)).normalized;
    }

    // Cone should face horizontally, independent of tilt.
    private Vector2 VisionForward()
    {
        // If flip = true, forward is left even at rest
        return flip ? Vector2.left : Vector2.right;
    }


    private void SetFacingRight(bool right)
    {
        var sc = transform.localScale;
        sc.x = Mathf.Abs(sc.x) * (right ? 1f : -1f);
        transform.localScale = sc;
        facingRight = right;
    }

    // ---------- Visual polish ----------
    private IEnumerator RestoreWaveAmplitude()
    {
        float t = 0f;
        float originalAmp = waveAmplitude;
        waveAmplitude = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            waveAmplitude = Mathf.Lerp(0f, originalAmp, t / 0.5f);
            yield return null;
        }
        waveAmplitude = originalAmp;
    }


    // ---------- Helpers ----------
    private void ComputeVerticalLimits()
    {
        var cam = Camera.main;
        if (!cam) { topLimitY = float.PositiveInfinity; bottomLimitY = float.NegativeInfinity; return; }

        // distance along camera forward
        float zDist = Mathf.Abs(Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward));

        var sr = GetComponent<SpriteRenderer>();
        float halfH = sr ? sr.bounds.extents.y : 0f;

        Vector3 topWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist));
        Vector3 botWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDist));
        topLimitY = topWorld.y - halfH;
        bottomLimitY = botWorld.y + halfH;
    }

    private float ClampY(float y, float margin = 0f)
    {
        return Mathf.Clamp(y, bottomLimitY + margin, topLimitY - margin);
    }

    private float GetTopY(Animal a)
    {
        var sr = a.GetComponent<SpriteRenderer>();
        if (sr) return sr.bounds.max.y;
        return a.transform.position.y;
    }

    private void AdjustStartYToFitWave()
    {
        startY = ClampY(transform.position.y, margin: waveAmplitude);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        // min radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, minDetectRadius);

        // vision cone (horizontal, independent of tilt)
        Vector3 fwd = VisionForward();
        float half = frontConeAngle * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(+half, Vector3.forward);
        Quaternion rightRot = Quaternion.AngleAxis(-half, Vector3.forward);
        Vector3 L = leftRot * fwd * detectRadius;
        Vector3 R = rightRot * fwd * detectRadius;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + L);
        Gizmos.DrawLine(transform.position, transform.position + R);
    }
#endif
}
