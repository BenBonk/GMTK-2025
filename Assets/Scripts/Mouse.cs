using UnityEngine;

public class Mouse : Animal
{
    [Header("Targets & Edges")]
    public float screenEdgePadding = 0.25f;
    public float arrivalThreshold = 0.05f;

    [Header("Speed Recovery")]
    public float acceleration = 0.5f;

    public bool flipOnTurn = true;

    private enum State { RunToLeft, RunToRight, ExitLeft }
    private State state = State.RunToLeft;

    private float leftX, rightX, offLeftX;
    private Vector3 targetPos;

    private float speedTarget;
    private float speedVelocity;

    protected override void Awake()
    {
        base.Awake();
        FaceDirection(-1);
    }

    public override void Start()
    {
        base.Start();

        speedTarget = speed;
        currentSpeed = speed;

        var cam = Camera.main;
        if (cam != null)
        {
            float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 leftW = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z));
            Vector3 rightW = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z));

            float halfWidth = GetComponent<SpriteRenderer>()?.bounds.extents.x ?? 0f;

            leftX = leftW.x + halfWidth + screenEdgePadding;   // interior left
            rightX = rightW.x - halfWidth - screenEdgePadding;   // interior right
            offLeftX = leftW.x - halfWidth - 1.0f;                // off-screen threshold
        }

        EnterState(State.RunToLeft);
    }

    protected override Vector3 ComputeMove()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, acceleration);

        Vector3 pos = transform.position;

        switch (state)
        {
            case State.RunToLeft:
            case State.RunToRight:
                {
                    Vector3 toTarget = targetPos - pos;
                    float dist = toTarget.magnitude;

                    if (dist <= arrivalThreshold)
                    {
                        AdvanceState();
                    }
                    else
                    {
                        pos += toTarget.normalized * currentSpeed * Time.deltaTime;
                    }
                    break;
                }

            case State.ExitLeft:
                {
                    // March left; never change direction once exiting
                    pos += Vector3.left * currentSpeed * Time.deltaTime;

                    // Optional: self-despawn if (pos.x <= offLeftX)
                    break;
                }
        }

        return pos; // base class will add externalOffset, clamp Y, clear offset
    }

    // ---- state mgmt ----

    private void EnterState(State s)
    {
        state = s;

        switch (state)
        {
            case State.RunToLeft:
                targetPos = new Vector3(leftX, PickRandomEdgeY(), transform.position.z);
                FaceDirection(-1);
                break;

            case State.RunToRight:
                targetPos = new Vector3(rightX, PickRandomEdgeY(), transform.position.z);
                FaceDirection(+1);
                break;

            case State.ExitLeft:
                // Lock facing left; no more flips
                FaceDirection(-1);
                break;
        }
    }

    private void AdvanceState()
    {
        switch (state)
        {
            case State.RunToLeft:
                EnterState(State.RunToRight);
                break;

            case State.RunToRight:
                EnterState(State.ExitLeft);
                break;

            case State.ExitLeft:
                // keep going left forever (or until despawn)
                break;
        }
    }

    private float PickRandomEdgeY()
    {
        float pad = 0.02f;
        float yMin = bottomLimitY + pad;
        float yMax = topLimitY - pad;
        return Random.Range(yMin, yMax);
    }

    // Assumes sprite artwork faces LEFT by default
    private void FaceDirection(int sign)
    {
        if (!flipOnTurn) return;

        // sign: -1 = face left (default), +1 = face right (flip)
        Vector3 sc = transform.localScale;
        float abs = Mathf.Abs(sc.x);
        sc.x = (sign < 0) ? abs : -abs;
        transform.localScale = sc;
    }
}
