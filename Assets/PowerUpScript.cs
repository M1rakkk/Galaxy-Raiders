using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class PowerUpScript : MonoBehaviour
{
    public float rotationSpeed;
    bool exists;

    // Start is called before the first frame update
    void Start()
    {
        Rigidbody powerUp = GetComponent<Rigidbody>();
        powerUp.angularVelocity = Vector3.forward * rotationSpeed;
    }

    // OnTriggerEnter is called in collision with object
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
            Destroy(gameObject);
    }
}