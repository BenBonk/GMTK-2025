using UnityEngine;

public class Chicken : Animal
{
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.6f;
    public float stopThreshold = 0.05f;

    public int movesBeforeExit = 3;
    public float chanceToExit = 0.3f;

    private Vector3 moveDirection;
    private float moveTimer;
    private float pauseTimer;
    private bool isPaused = false;

    private float speedTarget;
    private float speedVelocity;

    private int movesCompleted = 0;
    private bool exiting = false;
    public GameObject chickenEgg;

    public override void Start()
    {
        base.Start();
        speedTarget = speed;
        currentSpeed = speed;
        PickNewMoveTarget();
    }

    protected override Vector3 ComputeMove()
    {
        if (exiting)
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, speed, ref speedVelocity, pauseFadeDuration);
            return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
        }

        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);

        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed;

                if (movesCompleted >= movesBeforeExit && Random.value < chanceToExit)
                {
                    BeginExit();
                }
                else
                {
                    PickNewMoveTarget();
                }
            }

            return transform.position;
        }
        else
        {
            moveTimer -= Time.deltaTime;

            if (moveTimer <= 0f)
            {
                speedTarget = 0f;

                if (Mathf.Abs(currentSpeed) < stopThreshold)
                {
                    currentSpeed = 0f;
                    speedVelocity = 0f;
                    isPaused = true;
                    pauseTimer = Random.Range(minPauseDuration, maxPauseDuration);
                    movesCompleted++;
                }
            }

            return transform.position + moveDirection * currentSpeed * Time.deltaTime;
        }
    }

    private void PickNewMoveTarget()
    {
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z - transform.position.z);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        Vector3 target = new Vector3(
            Random.Range(bottomLeft.x, topRight.x),
            Random.Range(bottomLeft.y, topRight.y),
            transform.position.z
        );

        // Face right if moving right (sprite faces left by default)
        if (target.x > transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); // flip to right
        else
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); // face left (default)

        moveDirection = (target - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target);
        moveTimer = distance / speed;

        if (legendary && Random.Range(0,4)==0)
        {
            Instantiate(chickenEgg,transform.position, Quaternion.identity);
        }
    }

    private void BeginExit()
    {
        exiting = true;
        isPaused = false;
        speedTarget = speed;
        moveDirection = Vector3.left;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }
}


