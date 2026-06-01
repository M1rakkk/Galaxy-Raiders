using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundaryScript : MonoBehaviour
{
    // OnTriggerExit is called when object cross boundaries
    private void OnTriggerExit(Collider other)
    {
        Destroy(other.gameObject);
    }
}
