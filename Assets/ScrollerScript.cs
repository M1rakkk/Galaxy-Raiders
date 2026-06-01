using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollerScript : MonoBehaviour
{
    public float speed;
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        float offset = Mathf.Repeat(speed * Time.time, 200);
        startPosition.z = -offset;
        transform.position = startPosition;
    }
}
