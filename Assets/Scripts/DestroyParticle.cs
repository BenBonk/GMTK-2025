using UnityEngine;

public class DestroyParticle : MonoBehaviour
{
    public float timeToDestroy = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, timeToDestroy);   
    }
}
