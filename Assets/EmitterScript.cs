using UnityEngine;
using UnityEngine.UI;
using System;

public class EmitterScript : MonoBehaviour
{
    public GameObject[] asteroids; //список астероидов
    public GameObject enemyShip; //вражеский корабль
    public GameObject bossShip; //финальный босс
    public GameObject powerUp; //бонус
    public GameObject healthPack; //аптечка
    public Text text; //текст
    public float minDelay, maxDelay; //границы задержки времени появления вражеских объектов
    public int minX, maxX, minZ, maxZ; //границы области появления бонусов
    public int delayTime; //время задержки появления бонусов
    float nextLaunchTime; //время появления вражеского объекта
    float nextAppearTime = 10; //время появления бонуса
    int choice; //выбор вражеского объекта
    bool bossSpawned = false;

    // Update is called once per frame
    void Update()
    {
        if (!GameControllerScript.instance.isStarted)
        {
            return;
        }

        if (GameControllerScript.instance.IsInSpawnPause())
        {
            return;
        }

        if (GameControllerScript.instance.IsBossTime())
        {
            TrySpawnBoss();
            return;
        }

        float positionZ = transform.position.z;
        float positionX = UnityEngine.Random.Range(- transform.localScale.x / 2, transform.localScale.x / 2);
        //Условие запуска вражеского объекта
        if (Time.time > nextLaunchTime)
        {
            int spawnsThisTick = GameControllerScript.instance.GetEnemySpawnsPerTick();

            for (int i = 0; i < spawnsThisTick; i++)
            {
                positionX = UnityEngine.Random.Range(-transform.localScale.x / 2, transform.localScale.x / 2);
                choice = UnityEngine.Random.Range(0, 4);
                if (choice >= 0 && choice <= 2)
                {
                    Instantiate(asteroids[choice], new Vector3(positionX, 0, positionZ), Quaternion.identity);
                }
                else
                {
                    GameObject enemy = Instantiate(enemyShip, new Vector3(positionX, 0, positionZ), Quaternion.identity);
                    EnemyScript script = enemy.GetComponent<EnemyScript>();
                    if (script != null)
                    {
                        script.ApplyDifficulty(GameControllerScript.instance.GetEnemySpeedMultiplier(), GameControllerScript.instance.GetEnemyDamage());
                    }
                }
            }

            nextLaunchTime = Time.time + UnityEngine.Random.Range(
                GameControllerScript.instance.GetMinSpawnDelay(),
                GameControllerScript.instance.GetMaxSpawnDelay()
            );
        }
        //Условие появления бонуса
        if (Time.time > nextAppearTime)
        {
            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(minX, maxX), 0, UnityEngine.Random.Range(minZ, maxZ));
            
            // 30% шанс на аптечку, если она назначена, иначе всегда щит
            if (healthPack != null && UnityEngine.Random.value < 0.3f)
            {
                Instantiate(healthPack, spawnPos, Quaternion.identity);
            }
            else
            {
                Instantiate(powerUp, spawnPos, Quaternion.identity);
            }
            
            nextAppearTime = Time.time + delayTime;
        }
        // else
            // text.text = String.Format("До появления бонуса осталось {0} сек.", Convert.ToInt16(nextAppearTime - Time.time));
    }

    void TrySpawnBoss()
    {
        if (bossSpawned)
        {
            return;
        }
        if (bossShip == null)
        {
            return;
        }

        bossSpawned = true;

        Vector3 spawnPosition = new Vector3(0f, 0f, transform.position.z);
        GameObject boss = Instantiate(bossShip, spawnPosition, Quaternion.identity);

        EnemyScript enemyScript = boss.GetComponent<EnemyScript>();
        if (enemyScript != null)
        {
            enemyScript.ApplyDifficulty(GameControllerScript.instance.GetEnemySpeedMultiplier(), GameControllerScript.instance.GetEnemyDamage());
        }
    }
}
