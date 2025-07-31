using UnityEngine;

public class Pig : Animal
{
    public float waveAmplitude = 1f; // Height of sine wave
    public float waveFrequency = 2f; // Wave cycles during movement
    public override void Move()
    {
        traveled += speed * Time.deltaTime;
        float x = startPos.x - traveled;

        float horizontalDistance = startPos.x - leftEdgeX;
        float progress = Mathf.Clamp01(traveled / horizontalDistance);

        float y = startPos.y + Mathf.Sin(progress * Mathf.PI * 2 * waveFrequency) * waveAmplitude;

        transform.position = new Vector3(x, y, startPos.z);
    }
}
