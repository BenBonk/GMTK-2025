using System.Collections.Generic;
using UnityEngine;

public class TumbleweedMover2D : MonoBehaviour
{
    [Header("Horizontal motion")]
    [Tooltip("Base leftward speed in world units per second.")]
    public float baseSpeed = 3f;

    [Tooltip("Adds windy speed variation using Perlin noise.")]
    public float windGustStrength = 1.5f;

    [Tooltip("How fast gusts change over time.")]
    public float windGustFrequency = 0.4f;

    [Header("Vertical bounce")]
    [Tooltip("Simulated gravity for hops.")]
    public float gravity = 12f;

    [Tooltip("Energy kept after each ground bounce (0..1).")]
    [Range(0f, 1f)] public float bounceElasticity = 0.55f;

    [Tooltip("Random upward kick applied on each ground hit.")]
    public Vector2 bounceUpImpulseRange = new Vector2(4f, 7f);

    [Tooltip("Tiny horizontal kick each bounce to vary spin/pace.")]
    public Vector2 bounceSideImpulseRange = new Vector2(-0.5f, 0.5f);

    [Tooltip("Extra hover above ground to avoid clipping.")]
    public float groundClearance = 0.05f;

    [Header("Spin")]
    [Tooltip("Approx radius of the tumbleweed sprite in world units.")]
    public float radiusUnits = 0.5f;

    [Header("Screen margin")]
    [Tooltip("Margin outside view before destroying.")]
    public float offscreenMargin = 1.5f;

    [Header("Hit reaction (stop then ramp)")]
    public float hitPauseDuration = 0.35f;  
    public float hitRampDuration = 0.6f;   

    float pauseTimer = 0f;  
    float rampTimer = 0f;

    public GameObject poof;

    public Sprite[] tumbleSprites;

    // runtime
    float slowMoTimer = 0f;

    // internal state
    float vx;      // horizontal velocity (left is negative)
    float vy;      // vertical velocity
    float perlinT;
    Camera cam;

    // private only (no extra public fields)
    int seed = 0;
    float groundY;
    float zDepth;
    float spriteHalfWidth;
    Transform tr;
    SpriteRenderer sr;

    CircleCollider2D circle;

    void Awake()
    {
        tr = transform;
        sr = GetComponent<SpriteRenderer>();
        circle = GetComponent<CircleCollider2D>();

        sr.sprite = tumbleSprites[Random.Range(0, tumbleSprites.Length)];

        cam = Camera.main;
        if (!cam) return;

        zDepth = Mathf.Abs(cam.transform.position.z - tr.position.z);

        if (seed == 0) seed = Random.Range(int.MinValue, int.MaxValue);
        perlinT = Random.value * 1000f; // desync gusts per instance

        // random initial spin angle
        tr.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        // cache half width for nicer wrapping
        if (sr && sr.sprite) spriteHalfWidth = sr.bounds.extents.x;
        else spriteHalfWidth = Mathf.Max(0.1f, radiusUnits * 0.9f);

        ChooseSafeGroundYAndPlace();
    }

    void Update()
    {
        if (!cam) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) full stop while paused
        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            return; 
        }

        float speedScale = 1f;
        if (rampTimer > 0f)
        {
            rampTimer -= dt;
            float u = 1f - Mathf.Clamp01(rampTimer / hitRampDuration);
                                                                     
            speedScale = u * u * (3f - 2f * u);
        }

        // Horizontal: base + perlin gust
        perlinT += windGustFrequency * dt;
        float gust = (Mathf.PerlinNoise(seed * 0.001f, perlinT) - 0.5f) * 2f; // -1..1
        vx = -(baseSpeed + windGustStrength * gust);

        // Vertical: gravity
        vy -= gravity * dt;

        // Integrate position
        Vector3 p = tr.position;
        p.x += vx * dt;
        p.y += vy * dt;

        // Ground contact and bounce
        float minY = groundY + groundClearance;
        if (p.y <= minY)
        {
            p.y = minY;

            if (vy < 0f)
            {
                AudioManager.Instance.PlaySFX("tumbleweed_bounce");

                float upKick = Random.Range(bounceUpImpulseRange.x, bounceUpImpulseRange.y);
                vy = Mathf.Abs(vy) * bounceElasticity + upKick;

                float sideKick = Random.Range(bounceSideImpulseRange.x, bounceSideImpulseRange.y);
                p.x += sideKick * dt;
            }
        }

        tr.position = p;

        // Spin based on horizontal travel distance
        if (radiusUnits > 0.0001f)
        {
            float dThetaRad = (vx * dt) / radiusUnits;
            tr.Rotate(0f, 0f, dThetaRad * Mathf.Rad2Deg);
        }

        CheckHitActiveLasso();

        HandleScreenBounds();
    }

    void CheckHitActiveLasso()
    {
        var lc = GameController.gameManager.lassoController;
        if (lc == null || !lc.isDrawing) return;
        var lr = lc.lineRenderer;
        if (lr == null) return;

        int n = lr.positionCount;
        if (n < 2) return;

        // world-space circle from CircleCollider2D
        Vector3 wc = tr.TransformPoint((Vector3)circle.offset);
        Vector2 c = new Vector2(wc.x, wc.y);
        Vector3 s = tr.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        float r = Mathf.Max(0.0001f, circle.radius * scale);

        float contactR2 = r * r;

        var pts = new Vector3[n];
        lr.GetPositions(pts);

        for (int i = 0; i < n - 1; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[i + 1];

            float dist2 = PointSegmentDistSqr(c, a, b);
            if (dist2 < contactR2)
            {
                float t;
                Vector2 contact = ClosestPointOnSegment(c, a, b, out t);

                Vector3 spawn = new Vector3(contact.x, contact.y, transform.position.z);
                spawn.z = transform.position.z - 0.01f;
                var fx = Instantiate(poof, spawn, Quaternion.Euler(0f, 0f, 0f));
                var pr = fx.GetComponent<ParticleSystemRenderer>();
                if (pr != null)
                {
                    var twSr = sr ?? GetComponent<SpriteRenderer>();
                    if (twSr != null)
                    {
                        pr.sortingLayerID = twSr.sortingLayerID;
                        pr.sortingOrder = twSr.sortingOrder + 10;
                    }
                }

                // break the in-progress lasso as failed
                lc.FadeOutActiveLasso(downDistance: .1f, duration: 0.4f);
                AudioManager.Instance.PlaySFX("tumbleweed_break");

                // stop, then ramp back up
                pauseTimer = hitPauseDuration;
                rampTimer = hitRampDuration;
                return;
            }
        }
    }

    static float PointSegmentDistSqr(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float ab2 = Vector2.SqrMagnitude(ab);
        if (ab2 <= 1e-8f) return Vector2.SqrMagnitude(p - a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab2);
        Vector2 proj = a + ab * t;
        return Vector2.SqrMagnitude(p - proj);
    }

    static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        float ab2 = Vector2.Dot(ab, ab);
        if (ab2 <= 1e-8f) { t = 0f; return a; }
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab2);
        return a + ab * t;
    }


    void ChooseSafeGroundYAndPlace()
    {
        // Compute safe ground band so the highest hop stays on-screen
        Vector3 topW = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDepth));
        Vector3 bottomW = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDepth));
        float topY = topW.y;
        float bottomY = bottomW.y;

        float e = Mathf.Clamp01(bounceElasticity);
        float upMax = Mathf.Max(bounceUpImpulseRange.x, bounceUpImpulseRange.y);
        float oneMinusE = Mathf.Max(0.0001f, 1f - e);
        float vMax = upMax / oneMinusE;                  // worst-case peak launch speed
        float g = Mathf.Max(0.0001f, gravity);
        float Hmax = (vMax * vMax) / (2f * g);           // worst-case hop height

        float maxGroundY = topY - groundClearance - Hmax;
        float minGroundY = bottomY;

        if (maxGroundY <= minGroundY)
            groundY = minGroundY;                        // degenerate case: hug bottom
        else
            groundY = Random.Range(minGroundY, maxGroundY);

        // Start snapped on the chosen ground
        Vector3 p = tr.position;
        p.y = groundY + groundClearance;
        tr.position = p;
    }

    void HandleScreenBounds()
    {
        if (!cam) return;

        Vector3 leftW = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, zDepth));
        Vector3 rightW = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, zDepth));

        float leftLimit = leftW.x - offscreenMargin;
        float rightLimit = rightW.x + offscreenMargin;

        Vector3 p = tr.position;

        // keep from drifting too far right
        if (p.x - spriteHalfWidth > rightLimit)
            p.x = rightLimit - spriteHalfWidth;

        // if fully off the left, destroy
        if (p.x + spriteHalfWidth < leftLimit)
        {
            Destroy(gameObject);
            return;
        }

        tr.position = p;
    }
}


