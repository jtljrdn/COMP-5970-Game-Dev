using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public GameObject enemyPrefab;
    float spawnRate = 2f;

    float minY = 1.5f;
    float maxY = 4f;

    float nextSpawnTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        bool spawnFromLeft = Random.value > 0.5f;
        float spawnX = spawnFromLeft ? -3f : 3f;
        float spawnY = Random.Range(minY, maxY);
        GameObject enemy = Instantiate(enemyPrefab, new Vector3(spawnX, spawnY, transform.position.z), Quaternion.Euler(0f, 0f, 180f));
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (spawnFromLeft)
        {
            enemyScript.moveSpeed = Random.Range(1f, 3f);
        }
        else
        {
            enemyScript.moveSpeed = Random.Range(-3f, -1f);
        }
    }
}
