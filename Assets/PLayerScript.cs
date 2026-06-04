using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PLayerScript : MonoBehaviour
{
    [Header("Revive")]
    [SerializeField] float reviveInvulnerabilityDuration = 1.5f;

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

    [Header("Health")]
    public int maxHp = 100;
    public Slider hpSlider;
    int currentHp;

    float nextShotTime; //время основного выстрела
    float nextShotSmallTime; //время бокового выстрела
    Rigidbody playerShip; //объект корабля
    Vector3 startPosition; //начальная позиция для возврата из меню и revive
    Quaternion startRotation; //начальный поворот для возврата из меню и revive
    int shieldCount = 0; //количество щитов (бывш. hp)
    bool isReviveInvulnerable;
    bool reviveShieldActive;
    Coroutine reviveInvulnerabilityCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        playerShip = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        currentHp = maxHp;
        
        if (hpSlider == null && GameControllerScript.instance != null)
        {
            hpSlider = GameControllerScript.instance.playerHpSlider;
        }
        
        UpdateHpBar();
    }

    void UpdateHpBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }
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
            shieldCount ++;
            //Если не были установлены щиты, то включаем их 
            if (shieldCount == 1)
            {
                SetShieldScale(maxShieldSize, 2);
            }
            return;
        }

        if (IsReviveInvulnerable())
        {
            if (other.tag == "LazerEnemyShot")
            {
                Destroy(other.gameObject);
            }
            return;
        }

        int incomingDamage = 1;
        DamageSource source = other.GetComponent<DamageSource>();
        if (source != null)
        {
            incomingDamage = Mathf.Max(1, source.damage);
        }

        if (shieldCount > 0)
        {
            shieldCount--;
            if (shieldCount == 0)
            {
                SetShieldScale(0, 0);
            }
            Destroy(other.gameObject);
            return;
        }

        currentHp -= incomingDamage;
        UpdateHpBar();

        //Если жизни закончились
        if (currentHp <= 0)
        {
            Instantiate(playerExplosion, transform.position, Quaternion.identity);
            if (other.gameObject.tag != "Player") // Don't destroy yourself if colliding with something? Wait, other is the enemy/shot
            {
                Destroy(other.gameObject);
            }
            GameControllerScript.instance.ShowGameOver(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void Revive()
    {
        ResetPlayer();
        ActivateReviveInvulnerability();
    }

    public void ResetPlayer()
    {
        gameObject.SetActive(true);
        shieldCount = 0;
        currentHp = maxHp;
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
        isReviveInvulnerable = false;
        reviveShieldActive = false;

        if (reviveInvulnerabilityCoroutine != null)
        {
            StopCoroutine(reviveInvulnerabilityCoroutine);
            reviveInvulnerabilityCoroutine = null;
        }
    }

    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        UpdateHpBar();
    }

    bool IsReviveInvulnerable()
    {
        return isReviveInvulnerable;
    }

    void ActivateReviveInvulnerability()
    {
        if (reviveInvulnerabilityCoroutine != null)
        {
            StopCoroutine(reviveInvulnerabilityCoroutine);
            reviveInvulnerabilityCoroutine = null;
        }

        isReviveInvulnerable = true;
        reviveShieldActive = true;
        SetShieldScale(maxShieldSize, 2);

        reviveInvulnerabilityCoroutine = StartCoroutine(DisableReviveInvulnerabilityAfterDelay());
    }

    IEnumerator DisableReviveInvulnerabilityAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, reviveInvulnerabilityDuration));

        isReviveInvulnerable = false;
        reviveShieldActive = false;

        if (shieldCount <= 0)
        {
            SetShieldScale(0, 0);
        }

        reviveInvulnerabilityCoroutine = null;
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
