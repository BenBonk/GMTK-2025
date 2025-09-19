using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Animal : MonoBehaviour
{
    private GameManager gameManager;
    private Player player;
    private AnimalLevelManager levelManager;

    public AnimalData animalData;
    public bool isPredator;

    public float speed;
    public float currentSpeed;

    //movement parameters
    public bool isLassoed;
    public float leftEdgeX;
    public Vector3 startPos;
    protected Vector3 externalOffset = Vector3.zero;
    protected Vector3 pendingExternalOffset = Vector3.zero;

    public float topLimitY;
    public float bottomLimitY;

    protected float minimumSpacing = 0.15f;
    public float MinimumSpacing => minimumSpacing;
    private float repelForce = 4; 
    public virtual bool IsRepelImmune => false;

    // run animation parameters
    private float tiltAngle = 0f;
    public float tiltFrequency = 3f; // how fast the tilt cycles 
    public float maxTiltAmplitude = 20f; // degrees at full speed

    private float tiltProgress = 0f;
    private Vector3 previousPosition;
    public float actualSpeed { get; private set; } // total movement speed
    public bool legendary;
    public bool forceExit = false;
    [HideInInspector] public int bonusPoints;
    protected virtual void Awake()
    {
        SetVerticalLimits();
    }

    public virtual void Start()
    {
        levelManager = GameController.animalLevelManager; 
        gameManager = GameController.gameManager;
        player = GameController.player;
        //level = levelManager.GetLevel(name);
        try
        {
            if (GameController.boonManager.ContainsBoon(animalData.legendaryBoon.name))
            {
                legendary = true;
                ActivateLegendary();
            }
        }
        catch (Exception e)
        { //
        }
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 1f;

            // Set starting position slightly offscreen right
            startPos = new Vector3(rightEdge.x + 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }
        currentSpeed = speed;
    }
    

    public void Move()
    {
        ApplyRepelFromNearbyAnimals();

        overriddenByAttraction = false;

        Vector3 nextPos;
        if ((GameController.gameManager != null && GameController.gameManager.roundCompleted) || forceExit)
        {
            nextPos = LeaveScreen();
        }
        else if (TryPredatorAttractionOverride(out nextPos))
        {
            overriddenByAttraction = true; // for custom predator tilts to fall back to base tilt
        }
        else
        {
            nextPos = ComputeMove();
        }

        nextPos += externalOffset;

        nextPos.y = ClampY(nextPos.y);

        transform.position = nextPos;
        externalOffset = Vector3.zero;

        if (nextPos.x < leftEdgeX - 5)
        {
            Destroy(gameObject);
        }
    }



    public virtual void ApplyExternalOffset(Vector3 offset)
    {
        // Instead of applying immediately, find the strongest offset
        if (offset.magnitude > pendingExternalOffset.magnitude)
            pendingExternalOffset = offset;
    }

    protected virtual Vector3 ComputeMove()
    {
        // Default behavior: move left across screen
        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }

    public virtual Vector3 LeaveScreen()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        float leaveSpeed = 5f;
        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            leaveSpeed = 16f;
        }
        return transform.position + Vector3.left * leaveSpeed * Time.deltaTime;
    }

    public void Update()
    {
        if (!isLassoed)
        {
            externalOffset = pendingExternalOffset;
            pendingExternalOffset = Vector3.zero;
            Move();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!isLassoed)
        {
            actualSpeed = (transform.position - previousPosition).magnitude / Time.deltaTime;
            previousPosition = transform.position;
            ApplyRunTilt();
        }
    }

    private void SetVerticalLimits()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 top = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));
        Vector3 bottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));

        float halfHeight = GetComponent<SpriteRenderer>()?.bounds.extents.y ?? 0f;

        topLimitY = top.y - halfHeight;
        bottomLimitY = bottom.y + halfHeight;
    }

    protected float ClampY(float y)
    {
        return Mathf.Clamp(y, bottomLimitY, topLimitY);
    }

    protected virtual void ApplyRunTilt()
    {
        // Advance the tilt cycle
        tiltProgress += Time.deltaTime * tiltFrequency;

        // Use a safe denominator; if speed is ~0, treat as 1 to avoid divide-by-zero
        float denom = Mathf.Max(0.0001f, Mathf.Abs(speed));
        float speedFactor = Mathf.Clamp01(actualSpeed / denom);

        float amplitude = maxTiltAmplitude * speedFactor;
        float angle = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * amplitude;

        if (float.IsNaN(angle) || float.IsInfinity(angle)) angle = 0f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }


    private void ApplyRepelFromNearbyAnimals()
    {
        if (IsRepelImmune) return; // Skip if animal is immune

        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (var other in allAnimals)
        {
            if (other == this || other.isLassoed)
                continue;

            Vector3 toOther = other.transform.position - transform.position;
            float dist = toOther.magnitude;

            if (dist > 0f && dist < minimumSpacing)
            {
                Vector3 pushDir = -toOther.normalized;
                float strength = (minimumSpacing - dist) / minimumSpacing;
                Vector3 push = pushDir * strength * repelForce * Time.deltaTime;
                ApplyExternalOffset(push);
            }
        }
    }


    protected bool overriddenByAttraction = false;
    protected Animal attractTarget = null;
    [SerializeField] protected bool leftIsPositiveScaleX = true;

    public float attractBrakeRadius = 2.0f;  // start slowing down inside this radius
    public float attractArriveRadius = 0.25f; // consider 'arrived' inside this radius
    public float attractSmoothTime = 0.25f; // SmoothDamp time (you already have)
    public float attractStopThreshold = 0.02f; // consider stopped below this speed
    private float _attractSpeedVel = 0f;

    public bool SetAttractTarget(Animal a, bool force = false)
    {
        if (!force && attractTarget != null && attractTarget.gameObject.activeInHierarchy)
            return false;

        attractTarget = a;
        _attractSpeedVel = 0f; 
        return true;
    }

    public void ClearAttractTarget()
    {
        attractTarget = null;
    }

    protected virtual void FaceByX(float xDir)
    {
        // Default: positive scale.x = facing LEFT
        var sc = transform.localScale;
        float abs = Mathf.Abs(sc.x);
        if (leftIsPositiveScaleX)
            sc.x = (xDir >= 0f) ? -abs : abs;   
        else
            sc.x = (xDir >= 0f) ? abs : -abs; 
        transform.localScale = sc;
    }
    protected virtual bool TryPredatorAttractionOverride(out Vector3 nextPos)
    {
        nextPos = transform.position;

        if (!isPredator) return false;
        if (attractTarget == null) return false;

        if (!attractTarget.gameObject.activeInHierarchy)
        {
            attractTarget = null;
            return false;
        }

        Vector3 to = attractTarget.transform.position - transform.position;
        float dist = to.magnitude;

        // --- Compute a *ramped* target speed ---
        //  dist >= brakeRadius         -> full speed
        //  arriveRadius < dist < brake -> scaled speed
        //  dist <= arriveRadius        -> 0 (arrived)
        float t = 1f; // scaling 0..1
        if (dist <= attractArriveRadius)
            t = 0f;
        else if (dist < attractBrakeRadius)
            t = Mathf.InverseLerp(attractArriveRadius, attractBrakeRadius, dist); // 0..1

        float speedTarget = speed * t;

        // Smoothly approach speedTarget
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref _attractSpeedVel, attractSmoothTime);

        // Face horizontally toward the target
        if (Mathf.Abs(to.x) > 0.001f) FaceByX(to.x);

        // Move toward the target (with overshoot clamp)
        if (dist > 0.000001f && currentSpeed > 0f)
        {
            Vector3 dir = to / dist;
            float step = currentSpeed * Time.deltaTime;
            if (step > dist) step = dist;
            nextPos = transform.position + dir * step;
        }

        // If firmly inside arrive radius and basically stopped, hold position
        if (dist <= attractArriveRadius && currentSpeed <= attractStopThreshold)
            nextPos = transform.position;

        return true; // attraction owns movement this frame
    }




    public virtual void ActivateLegendary()
    {
        
    }

}