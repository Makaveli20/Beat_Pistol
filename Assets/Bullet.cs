using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class Bullet : MonoBehaviour
{
    public float destroyAfterSeconds = 5f; // Time after which the bullet is destroyed automatically
    private SimpleShoot gunScript; // Reference to the shooting script to update the score

    void Start()
    {
        Destroy(gameObject, destroyAfterSeconds); // Destroy the bullet after a certain time to prevent clutter
        gunScript = FindObjectOfType<SimpleShoot>(); // Find the shooting script in the scene
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            Destroy(collision.gameObject); // Destroy the target
            Destroy(gameObject); // Destroy the bullet
            if (gunScript != null)
            {
                gunScript.AddScore(10); // Add score using the shooting script's method
            }
        }
    }
}*/
