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
        //Уничтожение выстрела
        if (other.tag == "LazerEnemyShot")
            Destroy(other.gameObject);
        //Если подобрали бонус
        if (other.tag == "PowerUp")
        {
            hp ++;
            //Если не были установлены щиты, то включаем их 
            if (hp == 1)
            {
                SetShieldScale(maxShieldSize, 2);
            }
            return;
        }
        hp--;
        //Если были щиты, то выключаем их
        if (hp == 0)
        {
            SetShieldScale(0, 0);
            Destroy(other.gameObject);
            return;
        }
        //Если жизни закончились
        if (hp < 0)
        {
            Instantiate(playerExplosion, transform.position, Quaternion.identity);
            Destroy(other.gameObject);
            GameControllerScript.instance.ShowGameOver(gameObject);
            gameObject.SetActive(false);
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
