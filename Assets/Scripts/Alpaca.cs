using UnityEngine;

public class Alpaca : Animal
{
    [Header("Loop Controls (only these matter)")]
    [Range(0f, 1f)]
    public float driftSpeedRatio = 0.35f;   // fraction of currentSpeed used to drift the loop center left
    public float loopRadius = 3.5f;         // circle radius (world units)

    // internal
    private Vector3 loopCenter;
    private float theta;                     // loop phase (radians)
    private const float edgeMargin = 0.08f;  // small vertical margin from screen edges

    public override void Start()
    {
        base.Start();

        // randomize phase so multiple alpacas don't sync
        theta = Random.Range(0f, Mathf.PI * 2f);

        // start the center at current position; clamp so the whole loop fits vertically
        loopCenter = transform.position;
        loopCenter.y = Mathf.Clamp(
            loopCenter.y,
            bottomLimitY + loopRadius + edgeMargin,
            topLimitY - loopRadius - edgeMargin
        );
    }

    protected override Vector3 ComputeMove()
    {
        // (Nice feel) absorb vertical external push into the loop center so it doesn't "snap back"
        if (Mathf.Abs(externalOffset.y) > 0f)
        {
            loopCenter.y = ClampY(loopCenter.y + externalOffset.y);
            externalOffset.y = 0f; // prevent double-adding in Animal.Move()
        }

        // 1) Drift the loop center left
        float centerDrift = Mathf.Clamp01(driftSpeedRatio) * currentSpeed;
        loopCenter += Vector3.left * centerDrift * Time.deltaTime;

        // keep the full circle visible vertically
        loopCenter.y = Mathf.Clamp(
            loopCenter.y,
            bottomLimitY + loopRadius + edgeMargin,
            topLimitY - loopRadius - edgeMargin
        );

        // 2) Rotate around the center: tangential speed is the remainder of our speed budget
        float tangential = Mathf.Max(0.01f, (1f - Mathf.Clamp01(driftSpeedRatio)) * currentSpeed);
        float angularSpeed = tangential / Mathf.Max(0.001f, loopRadius);
        theta += angularSpeed * Time.deltaTime; // CCW; change sign for CW if you like

        // 3) Position on the circle
        float x = Mathf.Cos(theta) * loopRadius;
        float y = Mathf.Sin(theta) * loopRadius;

        return loopCenter + new Vector3(x, y, 0f); // base Move() adds remaining externalOffset (x) & clamps Y
    }
}
