using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PLayerScript : MonoBehaviour
{
    public float speed; //текущая скорость
    public float tilt; //текущий наклон
    public float xMin, xMax, zMin, zMax; //границы области движения корабля
    public GameObject playerExplosion; //взрыв
    public GameObject lazerShot; //основной выстрел
    public Transform lazerGun; //центральная пушка
    public GameObject lazerShotSmall; //боковой выстрел
    public Transform lazerGunLeft; //левая пушка
    public Transform lazerGunRight; //правая пушка
    public float shotDelay; //задержка по времени выстрела
    public int maxShieldSize; //размер щита
    public Text text; //текст
    public Slider hpBar; //шкала здоровья
    public int maxHealth = 100; //максимальное здоровье
    int currentHealth; //текущее здоровье
    float nextShotTime; //время основного выстрела
    float nextShotSmallTime; //время бокового выстрела
    Rigidbody playerShip; //объект корабля
    Vector3 startPosition; //начальная позиция для возврата из меню и revive
    Quaternion startRotation; //начальный поворот для возврата из меню и revive
    int hp = 0; //жизни

    // Start is called before the first frame update
    void Start()
    {
        playerShip = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        currentHealth = maxHealth;
        UpdateHpBar();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameControllerScript.instance.isStarted)
        {
            return;
        }
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        playerShip.velocity = new Vector3(moveHorizontal, 0, moveVertical) * speed;
        float restrictedX = Mathf.Clamp(playerShip.position.x, xMin, xMax);
        float restrictedZ = Mathf.Clamp(playerShip.position.z, zMin, zMax);
        playerShip.position = new Vector3(restrictedX, 0, restrictedZ);
        playerShip.rotation = Quaternion.Euler(tilt * playerShip.velocity.z, 0, - playerShip.velocity.x * tilt);
        //Условие центрального выстрела
        if (Time.time > nextShotTime && Input.GetButton("Fire1"))
        {
            Instantiate(lazerShot, lazerGun.position, Quaternion.identity);
            nextShotTime = Time.time + shotDelay;
        }
        //Условие боковых выстрелов
        if (Time.time > nextShotSmallTime && Input.GetButton("Fire2"))
        {
            GameObject leftShot = Instantiate(lazerShotSmall, lazerGunLeft.position, Quaternion.Euler(0, -45, 0));
            leftShot.GetComponent<Rigidbody>().velocity = new Vector3(-50, 0, 50);
            GameObject rightShot = Instantiate(lazerShotSmall, lazerGunRight.position, Quaternion.Euler(0, 45, 0));
            rightShot.GetComponent<Rigidbody>().velocity = new Vector3(50, 0, 50);
            nextShotSmallTime = Time.time + shotDelay / 2;
        }
    }

    // OnTriggerEnter is called in collision with object
    private void OnTriggerEnter(Collider other)
    {
        //Отсутствие взаимодействия
        if (other.tag == "GameBoundary" || other.tag == "LazerShot")
            return;

        //Если подобрали бонус
        if (other.tag == "PowerUp")
        {
            hp++;
            //Если не были установлены щиты, то включаем их 
            if (hp == 1)
            {
                SetShieldScale(maxShieldSize, 2);
            }
            return;
        }

        //Если подобрали аптечку
        if (other.tag == "MedKit")
        {
            Heal(20);
            return;
        }

        //Уничтожение выстрела противника при попадании
        if (other.tag == "LazerEnemyShot")
            Destroy(other.gameObject);
        else if (other.tag == "Enemy" || other.tag == "Asteroid")
            Destroy(other.gameObject);

        //Если были щиты, то они поглощают урон
        if (hp > 0)
        {
            hp--;
            if (hp == 0)
            {
                SetShieldScale(0, 0);
            }
            return;
        }

        //Если щитов нет, получаем урон
        TakeDamage(10);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHpBar();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Instantiate(playerExplosion, transform.position, Quaternion.identity);
            GameControllerScript.instance.ShowGameOver(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        UpdateHpBar();
    }

    void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.value = (float)currentHealth / maxHealth;
        }
    }

    public void Revive()
    {
        ResetPlayer();
    }

    public void ResetPlayer()
    {
        gameObject.SetActive(true);
        hp = 0;
        currentHealth = maxHealth;
        UpdateHpBar();

        if (playerShip == null)
        {
            playerShip = GetComponent<Rigidbody>();
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (playerShip != null)
        {
            playerShip.velocity = Vector3.zero;
            playerShip.angularVelocity = Vector3.zero;
        }

        SetShieldScale(0, 0);
    }

    void SetShieldScale(float outerShieldSize, float innerShieldSize)
    {
        GameObject[] spheres = GameObject.FindGameObjectsWithTag("Shield");
        if (spheres.Length > 0)
        {
            spheres[0].transform.localScale = new Vector3(outerShieldSize, outerShieldSize, outerShieldSize);
        }

        if (spheres.Length > 1)
        {
            spheres[1].transform.localScale = new Vector3(innerShieldSize, innerShieldSize, innerShieldSize);
        }
    }
}
