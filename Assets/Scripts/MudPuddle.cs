using System;
using UnityEngine;

public class MudPuddle : MonoBehaviour
{
    [Range(0f, 1f)]
    public float slowMultiplier = 0.4f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.GetComponent<Animal>())
        {
            return;
        }

        Animal animal = other.GetComponent<Animal>();
        if (animal != null && !animal.isLassoed)
        {
            float slowedSpeed = animal.speed * slowMultiplier;
            if (animal.currentSpeed > slowedSpeed)
            {
                animal.currentSpeed = slowedSpeed;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Animal>())
        {
            Animal animal = other.GetComponent<Animal>();
            animal.currentSpeed = animal.speed;   
        }
    }
}
