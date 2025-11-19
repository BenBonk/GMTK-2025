using System;
using System.Collections;
using UnityEngine;

public class MudPuddle : MonoBehaviour
{
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;


    private void OnTriggerStay2D(Collider2D other)
    {
        var a = other.GetComponent<Animal>();
        if (a && !a.isLassoed)
        {
            a.ModifySpeed("mud", slowMultiplier);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var a = other.GetComponent<Animal>();
        if (a)
        {
            a.RevertSpeed("mud");
        }
    }
}
