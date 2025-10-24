using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TumbleweedMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float speedVariation = 0.5f;
    [SerializeField] private float bobAmount = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    
    private float actualMoveSpeed;
    private float bobOffset;

    private void Awake()
    {
        actualMoveSpeed = moveSpeed + Random.Range(-speedVariation, speedVariation);
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        MoveTumbleweed();
        RotateTumbleweed();
    }

    private void MoveTumbleweed()
    {
        float horizontalMovement = -actualMoveSpeed * Time.deltaTime;
        
        float bobMovement = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmount * Time.deltaTime;
        
        transform.position += new Vector3(horizontalMovement, bobMovement, 0f);
    }

    private void RotateTumbleweed()
    {
        float rotationAmount = -rotationSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
    }
}
