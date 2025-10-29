using System;
using System.Collections;
using UnityEngine;

public class MudPuddle : MonoBehaviour
{
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponent<Animal>())
        {
            return;
        }

        Animal animal = other.GetComponent<Animal>();
        if (animal != null && !animal.isLassoed)
        {
            /*float slowedSpeed = animal.speed * slowMultiplier;
            if (animal.currentSpeed > slowedSpeed)
            {
                animal.currentSpeed = slowedSpeed;
            }*/
            animal.ModifySpeed("mud", slowMultiplier); 
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Animal>())
        {
            Animal animal = other.GetComponent<Animal>();
            if (animal!=null)
            {
                StartCoroutine(WaitResume(animal));   
            }
        }
    }

    IEnumerator WaitResume(Animal a)
    {
        yield return new WaitForSeconds(.5f);
        try
        {
            a.RevertSpeed("mud");
        }
        catch (Exception e)
        {
            //a
        }
    }
}
