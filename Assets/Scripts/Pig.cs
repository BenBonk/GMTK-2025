using UnityEngine;

public class Pig : Animal
{
    [Header("Wave Motion")]
    public float waveAmplitude = 1f;   // Height of sine wave
    public float waveFrequency = 2f;   // Wave cycles per second

    private float baseAmplitude;
    private float baseFrequency;
    private float baseSpeed;

    private bool initialized = false;
    private float waveProgress = 0f;
    private float startY;
    private float _lastScale = 1f;

    [Header("Legendary")]
    public Sprite pigWithHat;

    protected override void Awake()
    {
        base.Awake();
        baseAmplitude = waveAmplitude;
        baseFrequency = waveFrequency;
        baseSpeed = speed;
    }

    public override void Start()
    {
        base.Start();
        currentSpeed = speed;
    }

    public override void ActivateLegendary()
    {
        if (Random.Range(0, 5) == 0)
        {
            GetComponent<SpriteRenderer>().sprite = pigWithHat;
            gameObject.tag = "PigWithHat";
        }
    }

    // Scale wave motion when global speed changes
    protected override void ApplyEffectiveSpeedScale(float scale)
    {
        speed = baseSpeed * scale;
        waveFrequency = baseFrequency * scale;
        waveAmplitude = baseAmplitude / Mathf.Pow(scale, 0.25f);
        if (_lastScale > 0f && !Mathf.Approximately(scale, _lastScale))
        {
            float k = scale / _lastScale;
            waveProgress /= k;
        }

        _lastScale = scale;
    }

    protected override Vector3 ComputeMove()
    {
        if (!initialized)
        {
            AdjustStartYToFitWave();
            initialized = true;
        }

        waveProgress += Time.deltaTime;

        float yOffset = Mathf.Sin(waveProgress * Mathf.PI * 2f * waveFrequency) * waveAmplitude;

        // Horizontal base movement
        Vector3 baseMove = transform.position + Vector3.left * currentSpeed * Time.deltaTime;

        // Respect vertical offset by adding it to the wave
        float verticalOffset = externalOffset.y;
        baseMove.y = startY + yOffset + verticalOffset;

        // Zero out the vertical offset so it isn’t double-counted later
        externalOffset.y = 0f;

        return baseMove;
    }

    private void AdjustStartYToFitWave()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 bottomWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 topWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;
        float topLimit = topWorld.y - halfHeight;
        float bottomLimit = bottomWorld.y + halfHeight;

        float maxY = topLimit - waveAmplitude;
        float minY = bottomLimit + waveAmplitude;

        startY = Mathf.Clamp(transform.position.y, minY, maxY);
    }
}

