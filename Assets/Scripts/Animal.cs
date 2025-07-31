using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Animal : MonoBehaviour
{
    private GameManager gameManager;
    private Player player;
    private AnimalLevelManager levelManager;

    public int currencyToGive;
    public int pointsToGive;
    public float currencyMultToGive;
    public float pointsMultToGive;
    public bool isPredator;

    public float speed;
    public float currentSpeed;

    //movement parameters
    public bool isLassoed;
    public float leftEdgeX;
    public Vector3 startPos;
    protected Vector3 externalOffset = Vector3.zero;

    public float topLimitY;
    public float bottomLimitY;

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
        Vector3 nextPos = ComputeMove();
        nextPos += externalOffset;

        nextPos.y = ClampY(nextPos.y);

        transform.position = nextPos;
        externalOffset = Vector3.zero;
    }

    public void ApplyExternalOffset(Vector3 offset)
    {
        externalOffset += offset;
    }

    protected virtual Vector3 ComputeMove()
    {
        // Default behavior: move left across screen
        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }

    public void Update()
    {
        if (!isLassoed)
        {
            Move();
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


}