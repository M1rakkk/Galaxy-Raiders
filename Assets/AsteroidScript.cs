using UnityEngine;

public class AsteroidScript : MonoBehaviour
{
    public GameObject asteroidExplosion;
    public GameObject playerExplosion;
    public float rotationSpeed;
    public float minSpeed, maxSpeed;
    public float minSize, maxSize;
    float size;

    // Start is called before the first frame update
    void Start()
    {
        Rigidbody asteroid = GetComponent<Rigidbody>();
        asteroid.angularVelocity = Random.insideUnitSphere * rotationSpeed;
        float speed = Random.Range(minSpeed, maxSpeed);
        asteroid.velocity = new Vector3(0, 0, - speed);
        size = Random.Range(minSize, maxSize);
        asteroid.transform.localScale *= size;
    }

    // OnTriggerEnter is called in collision with object
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.tag == "Asteroid" || other.tag == "GameBoundary" || other.tag == "PowerUp")
            return;
        Destroy(gameObject);  
        GameObject explosion = Instantiate(asteroidExplosion, transform.position, Quaternion.identity);
        explosion.transform.localScale *= size;
        
        if (other.tag == "Enemy" || other.tag == "LazerShot" || other.tag == "LazerEnemyShot")
        {
            Destroy(other.gameObject);
        }

        if (other.tag == "LazerShot")
        {
            Destroy(other.gameObject);
            GameControllerScript.instance.increaseScore(1);
        }
    }
}
