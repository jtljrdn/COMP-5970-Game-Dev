using UnityEngine;
using System.Collections.Generic;

public class WorldSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject[] chunkPrefabs;
    public float chunkSize = 10f;
    public int radius = 2;

    private Dictionary<Vector2Int, GameObject> activeChunks = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshGrid();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshGrid();
    }

    void RefreshGrid()
    {
        Vector2Int playerCoord = WorldToGrid(player.position);

        HashSet<Vector2Int> desired = new();
        for (int x = playerCoord.x - radius; x <= playerCoord.x + radius; x++)
        {
            for (int y = playerCoord.y - radius; y <= playerCoord.y + radius; y++)
            {
                desired.Add(new Vector2Int(x, y));
            }
        }

        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeChunks)
        {
            if (!desired.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (Vector2Int coord in toRemove)
        {
            Destroy(activeChunks[coord]);
            activeChunks.Remove(coord);
        }

        foreach (Vector2Int coord in desired)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                PlaceChunk(coord);
            }
        }
    }

    int PickCompatiblePrefab(Vector2Int coord)
    {
        ChunkPorts required = ChunkPorts.None;
        ChunkPorts forbidden = ChunkPorts.None;

        if (activeChunks.TryGetValue(coord + Vector2Int.up, out GameObject northChunk))
        {
            ChunkData northData = northChunk.GetComponent<ChunkData>();
            if (northData.HasPort(ChunkPorts.South))
            {
                required |= ChunkPorts.North;
            }
            else
            {
                forbidden |= ChunkPorts.North;
            }
        }

        if (activeChunks.TryGetValue(coord + Vector2Int.down, out GameObject southChunk))
        {
            ChunkData southData = southChunk.GetComponent<ChunkData>();
            if (southData.HasPort(ChunkPorts.North))
            {
                required |= ChunkPorts.South;
            }
            else
            {
                forbidden |= ChunkPorts.South;
            }
        }


        if (activeChunks.TryGetValue(coord + Vector2Int.right, out GameObject eastChunk))
        {
            ChunkData eastData = eastChunk.GetComponent<ChunkData>();
            if (eastData.HasPort(ChunkPorts.West))
            {
                required |= ChunkPorts.East;
            }
            else
            {
                forbidden |= ChunkPorts.East;
            }
        }

        if (activeChunks.TryGetValue(coord + Vector2Int.left, out GameObject westChunk))
        {
            ChunkData westData = westChunk.GetComponent<ChunkData>();
            if (westData.HasPort(ChunkPorts.East))
            {
                required |= ChunkPorts.West;
            }
            else
            {
                forbidden |= ChunkPorts.West;
            }
        }

        List<int> candidates = new();
        for (int i = 0; i < chunkPrefabs.Length; i++)
        {
            ChunkData data = chunkPrefabs[i].GetComponent<ChunkData>();
            if (data == null)
            {
                continue;
            }
            if ((data.ports & required) != required)
            {
                continue;
            }
            if ((data.ports & forbidden) != ChunkPorts.None)
            {
                continue;
            }
            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No compatible chunk found for coord {coord} with required {required} and forbidden {forbidden}");
            return Random.Range(0, chunkPrefabs.Length);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    void PlaceChunk(Vector2Int coord)
    {
        int randomIndex = PickCompatiblePrefab(coord);
        GameObject chunk = Instantiate(chunkPrefabs[randomIndex], GridToWorld(coord), Quaternion.identity);
        activeChunks.Add(coord, chunk);
    }

    public Vector2Int WorldToGrid(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(x, y);
    }

    Vector3 GridToWorld(Vector2Int coord)
    {
        float x = coord.x * chunkSize;
        float y = coord.y * chunkSize;
        return new Vector3(x, y, 0);
    }

    public Dictionary<Vector2Int, GameObject> GetActiveChunks()
    {
        return activeChunks;
    }

    public bool IsChunkActive(Vector2Int coord)
    {
        return activeChunks.ContainsKey(coord);
    }
}
