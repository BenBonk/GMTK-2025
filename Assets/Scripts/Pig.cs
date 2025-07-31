using UnityEngine;

public class Pig : Animal
{
    public float waveAmplitude = 1f; // Height of sine wave
    public float waveFrequency = 2f; // Wave cycles during movement

    private bool initialized = false;
    public override void Move()
    {
        if (!initialized)
        {
            AdjustStartYToFitWave();
            initialized = true;
        }

        traveled += speed * Time.deltaTime;
        float x = startPos.x - traveled;

        float horizontalDistance = startPos.x - leftEdgeX;
        float progress = Mathf.Clamp01(traveled / horizontalDistance);

        float y = startPos.y + Mathf.Sin(progress * Mathf.PI * 2 * waveFrequency) * waveAmplitude;

        transform.position = new Vector3(x, y, startPos.z);
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

        // Clamp original startPos.y into this range
        float clampedY = Mathf.Clamp(startPos.y, minY, maxY);
        startPos.y = clampedY;
    }
}
