using UnityEngine;
using UnityEngine.UI;
using System;

public class EmitterScript : MonoBehaviour
{
    public GameObject[] asteroids; //список астероидов
    public GameObject enemyShip; //вражеский корабль
    public GameObject bossShip; //финальный босс
    public GameObject powerUp; //бонус
    public Text text; //текст
    public float minDelay, maxDelay; //границы задержки времени появления вражеских объектов
    public int minX, maxX, minZ, maxZ; //границы области появления бонусов
    public int delayTime; //время задержки появления бонусов
    float nextLaunchTime; //время появления вражеского объекта
    float nextAppearTime = 10; //время появления бонуса
    int choice; //выбор вражеского объекта

    // Update is called once per frame
    void Update()
    {
        if (!GameControllerScript.instance.isStarted)
        {
            return;
        }

        if (GameControllerScript.instance.isBossLevel)
        {
            if (!GameControllerScript.instance.bossWasSpawned)
            {
                GameObject prefab = bossShip != null ? bossShip : enemyShip;
                GameObject boss = Instantiate(prefab, new Vector3(0, 0, transform.position.z), Quaternion.identity);
                EnemyScript bossScript = boss.GetComponent<EnemyScript>();
                if (bossScript != null)
                {
                    bossScript.isBoss = true;
                    bossScript.hitPoints = 20;
                    bossScript.damage = 3;
                }
                boss.transform.localScale *= 2f;
                GameControllerScript.instance.MarkBossSpawned();
            }
            return;
        }

        if (!GameControllerScript.instance.canSpawnEnemies)
        {
            return;
        }

        float positionZ = transform.position.z;
        //Условие запуска вражеского объекта
        if (Time.time > nextLaunchTime)
        {
            for (int i = 0; i < GameControllerScript.instance.GetEnemySpawnCount(); i++)
            {
                float positionX = UnityEngine.Random.Range(- transform.localScale.x / 2, transform.localScale.x / 2);
                choice = UnityEngine.Random.Range(0, 4);
                if (choice >= 0 && choice <= 2)
                    Instantiate(asteroids[choice], new Vector3(positionX, 0, positionZ), Quaternion.identity);                
                else
                    Instantiate(enemyShip, new Vector3(positionX, 0, positionZ), Quaternion.identity);
            }

            float delayMultiplier = GameControllerScript.instance.CurrentLevel >= 2 ? 0.65f : 1f;
            nextLaunchTime = Time.time + UnityEngine.Random.Range(minDelay, maxDelay) * delayMultiplier;
        }
        //Условие появления бонуса
        if (Time.time > nextAppearTime)
        {
            Instantiate(powerUp, new Vector3(UnityEngine.Random.Range(minX, maxX), 0, UnityEngine.Random.Range(minZ, maxZ)), Quaternion.identity);
            nextAppearTime = Time.time + delayTime;
        }
        // else
            // text.text = String.Format("До появления бонуса осталось {0} сек.", Convert.ToInt16(nextAppearTime - Time.time));
    }
}
