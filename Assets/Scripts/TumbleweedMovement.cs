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

    [Header("Screen wrap")]
    [Tooltip("Margin outside view before wrapping/destroying.")]
    public float offscreenMargin = 1.5f;

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

    void Awake()
    {
        tr = transform;
        sr = GetComponent<SpriteRenderer>();

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

        HandleScreenBounds();
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


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize chosen ground line when running
        Gizmos.color = Color.yellow;
        float gy = Application.isPlaying ? groundY : 0f;
        Vector3 a = new Vector3(-1000f, gy + groundClearance, 0f);
        Vector3 b = new Vector3(1000f, gy + groundClearance, 0f);
        Gizmos.DrawLine(a, b);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Abs(radiusUnits));
    }
#endif
}


