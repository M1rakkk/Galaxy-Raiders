using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public float minSpeed, maxSpeed;
    public GameObject playerExplosion;
    public GameObject lazerShot;
    public Transform lazerGun;
    public float shotDelay;
    float nextShotTime;
    Rigidbody enemyShip;
    float speed;
    Vector3 lastPosition;

    // Start is called before the first frame update
    void Start()
    {
        enemyShip = GetComponent<Rigidbody>();
        speed = Random.Range(minSpeed, maxSpeed);
        enemyShip.velocity = new Vector3(0, 0, - speed);
        enemyShip.transform.rotation = Quaternion.Euler(0, 180, 0);
        lastPosition = enemyShip.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //Поиск противника
        GameObject player = GameObject.Find("Player");
        if (player == true)
        { 
            //Определяем вектор перемещений до противника и текущего перемещения
            Vector3 playerDirection = player.transform.position - enemyShip.transform.position;
            Vector3 currentDirection = enemyShip.transform.position - lastPosition;
            //Определение угла поворота до соперника
            float angle = Vector3.SignedAngle(playerDirection, currentDirection, Vector3.up);
            //Задание новой скорости
            enemyShip.velocity = playerDirection / playerDirection.magnitude * speed;
            //Поворот
            enemyShip.transform.Rotate(0, -angle, 0);
            //Условие для выстрела
            if (Time.time > nextShotTime)
            {
                GameObject lazerEnemyShot = Instantiate(lazerShot, lazerGun.position, Quaternion.identity);                
                lazerEnemyShot.GetComponent<Rigidbody>().velocity = playerDirection / playerDirection.magnitude * 50;
                angle = Vector3.SignedAngle(Vector3.back, playerDirection, Vector3.up);
                lazerEnemyShot.transform.Rotate(0, angle, 0);
                nextShotTime = Time.time + shotDelay;
            }                        
        }
        else
        {
            //Если соперник повержен то двигаемся вниз
            enemyShip.velocity = new Vector3(0, 0, -speed);
            enemyShip.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        //Переписываем текущую позицию для дальнейшего вычисления вектора собственного перемещения
        lastPosition = enemyShip.transform.position;
    }

    // OnTriggerEnter is called in collision with object
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.tag == "GameBoundary" || other.tag == "LazerEnemyShot" || other.tag == "PowerUp")
            return;
        Instantiate(playerExplosion, transform.position, Quaternion.identity);            
        Destroy(gameObject);
        
        if (other.tag == "LazerShot")
        {
            Destroy(other.gameObject);
            GameControllerScript.instance.increaseScore(1);
        }
    }
}
