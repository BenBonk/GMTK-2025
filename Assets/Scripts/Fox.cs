using UnityEngine;

public class Fox : Animal
{
    public float baseAmplitude = 3f;
    public float baseFrequency = 2f;
    public float amplitudeVariation = 1f;
    public float frequencyVariation = 0.5f;
    public float variationSpeed = 0.2f;

    public float speedRecoveryRate = 0.5f; 

    private bool initialized = false;
    private float waveProgress = 0f;
    private float variationProgress = 0f;

    private float speedVelocity = 0f; // for SmoothDamp
    private float startY;

    // speed system baselines
    private float baseSpeed;
    private float baseFreq;
    private float baseVariationSpeed;
    private float baseSpeedRecoveryRate;

    protected override void Awake()
    {
        base.Awake();
        baseSpeed = speed;
        baseFreq = baseFrequency;
        baseVariationSpeed = variationSpeed;
        baseSpeedRecoveryRate = speedRecoveryRate;
    }

    public override void Start()
    {
        base.Start();
        AdjustStartYToFitWave();
        currentSpeed = speed;
        initialized = true;
    }

    protected override void ApplyEffectiveSpeedScale(float scale)
    {
        // linear move speed
        speed = baseSpeed * scale;

        // keep the wave "density" similar as speed changes:
        // slightly increase frequency and variation speed with scale
        float freqMul = Mathf.Pow(scale, 0.4f);
        baseFrequency = baseFrequency * freqMul;    
        variationSpeed = baseVariationSpeed * freqMul;

        speedRecoveryRate = baseSpeedRecoveryRate / Mathf.Pow(scale, 0.5f);
    }

    public override void ActivateLegendary()
    {
        animalData = GameController.gameManager.foxThiefStolenStats;
    }

    protected override Vector3 ComputeMove()
    {
        // Smoothly return to base speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speed, ref speedVelocity, speedRecoveryRate);

        waveProgress += Time.deltaTime;
        variationProgress += variationSpeed * Time.deltaTime;

        float currentAmplitude = baseAmplitude + Mathf.Sin(variationProgress * Mathf.PI * 2f) * amplitudeVariation;
        float currentFrequency = baseFrequency + Mathf.Sin(variationProgress * Mathf.PI * 2f + Mathf.PI / 2f) * frequencyVariation;

        float yOffset = Mathf.Sin(waveProgress * Mathf.PI * 2f * currentFrequency) * currentAmplitude;

        Vector3 baseMove = transform.position + Vector3.left * currentSpeed * Time.deltaTime;

        float verticalOffset = externalOffset.y;
        baseMove.y = startY + yOffset + verticalOffset;

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

        float maxY = topLimit - baseAmplitude - amplitudeVariation;
        float minY = bottomLimit + baseAmplitude + amplitudeVariation;

        startY = Mathf.Clamp(transform.position.y, minY, maxY);
    }
}



