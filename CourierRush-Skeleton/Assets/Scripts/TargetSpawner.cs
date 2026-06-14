using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TargetSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject target;
    public GameObject package;
    public WorldSpawner worldSpawner;

    float minSpawnDistance = 10f;
    float collectDistance = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(SpawnItemInitial), 0.1f);
    }

    void SpawnItemInitial()
    {
        SpawnItem(true, true);
    }

    void SpawnItem(bool ignoreDistance = false, bool isPackage = false)
    {
        List<Vector3> candidates = new();

        GameObject itemToSpawn = isPackage ? package : target;
        GameObject itemToDespawn = isPackage ? target : package;

        foreach (var kvp in worldSpawner.GetActiveChunks())
        {
            GameObject chunkObject = kvp.Value;
            if (chunkObject == null)
            {
                continue;
            }
            if (!ignoreDistance && Vector3.Distance(player.position, chunkObject.transform.position) < minSpawnDistance)
            {
                continue;
            }

            Tilemap road = chunkObject.transform.Find("Road")?.GetComponent<Tilemap>();
            if (road == null)
            {
                continue;
            }

            foreach (Vector3Int cellPosition in road.cellBounds.allPositionsWithin)
            {
                if (road.HasTile(cellPosition))
                {
                    Vector3 worldPos = road.GetCellCenterWorld(cellPosition);
                    candidates.Add(worldPos);
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No candidates found for target spawn.");
            return;
        }

        itemToSpawn.transform.position = candidates[Random.Range(0, candidates.Count)];
        itemToSpawn.SetActive(true);
        if (itemToDespawn != null)
        {
            itemToDespawn.SetActive(false);
        }
        Debug.Log($"Spawned {(isPackage ? "package" : "target")} at {itemToSpawn.transform.position}");
        Debug.Log($"Despawned {(isPackage ? "target" : "package")} at {itemToDespawn.transform.position}");
    }
    // Update is called once per frame
    void Update()
    {
        if (target == null || player == null || worldSpawner == null || package == null)
        {
            return;
        }

        GameObject currentTarget = target.activeSelf ? target : package;

        CarController carController = player.GetComponent<CarController>();
  
        float distance = Vector3.Distance(player.position, currentTarget.transform.position);

        if (distance < collectDistance)
        {
            if (carController.hasPackage == false)
            {
                carController.PickUpPackage();
            }
            else
            {
                carController.DeliverPackage();
            }
            SpawnItem(false, !carController.hasPackage);
            return;
        }

        Vector2Int targetCoord = worldSpawner.WorldToGrid(currentTarget.transform.position);

        if (!worldSpawner.IsChunkActive(targetCoord))
        {
            SpawnItem(false, !carController.hasPackage);
        }
    }
}
