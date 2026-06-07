using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    public GameObject meteorPrefab;
    float spawnRate = 4f;

    float minY = -1.5f;
    float maxY = -3f;

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
            SpawnMeteor();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnMeteor()
    {
        bool spawnFromLeft = Random.value > 0.5f;
        float spawnX = spawnFromLeft ? -3f : 3f;
        float spawnY = Random.Range(minY, maxY);
        GameObject meteor = Instantiate(meteorPrefab, new Vector3(spawnX, spawnY, transform.position.z), Quaternion.Euler(0f, 0f, 180f));
        Meteor meteorScript = meteor.GetComponent<Meteor>();
        if (spawnFromLeft)
        {
            meteorScript.moveSpeed = Random.Range(1f, 1.5f);
        }
        else
        {
            meteorScript.moveSpeed = Random.Range(-1.5f, -1f);
        }
    }
}
