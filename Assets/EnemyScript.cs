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
    int damage = 10;

    // Start is called before the first frame update
    void Start()
    {
        enemyShip = GetComponent<Rigidbody>();
        speed = Random.Range(minSpeed, maxSpeed);
        enemyShip.velocity = new Vector3(0, 0, - speed);
        enemyShip.transform.rotation = Quaternion.Euler(0, 180, 0);
        lastPosition = enemyShip.transform.position;

        DamageSource source = GetComponent<DamageSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<DamageSource>();
        }
        source.damage = damage;
    }

    // Update is called once per frame
    void Update()
    {
        BossScript boss = GetComponent<BossScript>();
        if (boss != null)
        {
            // Boss movement/attacks are controlled by BossScript only.
            enemyShip.velocity = Vector3.zero;
            lastPosition = enemyShip.transform.position;
            return;
        }

        //Поиск противника
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.activeInHierarchy)
        { 
            //Определяем вектор перемещений до противника и текущего перемещения
            Vector3 playerDirection = player.transform.position - enemyShip.transform.position;
            if (playerDirection.sqrMagnitude < 0.0001f)
            {
                playerDirection = Vector3.back;
            }
            Vector3 currentDirection = enemyShip.transform.position - lastPosition;
            //Определение угла поворота до соперника
            float angle = Vector3.SignedAngle(playerDirection, currentDirection, Vector3.up);
            //Задание новой скорости
            enemyShip.velocity = playerDirection.normalized * speed;
            //Поворот
            enemyShip.transform.Rotate(0, -angle, 0);
            //Условие для выстрела
            if (Time.time > nextShotTime)
            {
                GameObject lazerEnemyShot = Instantiate(lazerShot, lazerGun.position, Quaternion.identity);                
                lazerEnemyShot.GetComponent<Rigidbody>().velocity = playerDirection.normalized * 50;
                float shotAngle = Vector3.SignedAngle(Vector3.back, playerDirection, Vector3.up);
                lazerEnemyShot.transform.Rotate(0, shotAngle, 0);

                DamageSource shotDamage = lazerEnemyShot.GetComponent<DamageSource>();
                if (shotDamage == null)
                {
                    shotDamage = lazerEnemyShot.AddComponent<DamageSource>();
                }
                shotDamage.damage = damage;

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
        if (GetComponent<BossScript>() != null)
        {
            return;
        }
        
        if (other.tag == "GameBoundary" || other.tag == "LazerEnemyShot" || other.tag == "PowerUp")
            return;
        Instantiate(playerExplosion, transform.position, Quaternion.identity);            
        Destroy(gameObject);
        
        if (other.tag == "LazerShot")
        {
            Destroy(other.gameObject);
            if (GameControllerScript.instance != null)
            {
                GameControllerScript.instance.RegisterEnemyKill(1);
            }
        }
    }

    public void ApplyDifficulty(float speedMultiplier, int damageAmount)
    {
        float mult = Mathf.Max(0.1f, speedMultiplier);
        minSpeed *= mult;
        maxSpeed *= mult;
        damage = Mathf.Max(1, damageAmount);

        if (enemyShip != null)
        {
            speed = Random.Range(minSpeed, maxSpeed);
        }

        DamageSource source = GetComponent<DamageSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<DamageSource>();
        }
        source.damage = damage;
    }
}
