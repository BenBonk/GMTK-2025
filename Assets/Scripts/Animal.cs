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
        if (GameController.boonManager.ContainsBoon(animalData.legendaryBoon.name))
        {
            legendary = true;
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

        Vector3 nextPos;
        if (GameController.gameManager!= null && GameController.gameManager.roundCompleted)
        {
            nextPos = LeaveScreen();
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

        // Normalize actual speed relative to base speed
        float speedFactor = Mathf.Clamp01(actualSpeed / speed);

        // Calculate the tilt angle using sine wave
        float amplitude = maxTiltAmplitude * speedFactor;
        float angle = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * amplitude;

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


}