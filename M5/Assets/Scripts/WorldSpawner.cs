using UnityEngine;
using System.Collections.Generic;

public class WorldSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject[] chunkPrefabs;

    public Vector3 scrollAxis = Vector3.forward;

    public int chunksAhead = 10;

    public float despawnBuffer = 5f;

    public float laneWidth = 2f;

    public int maxLanes = 3;

    private bool isFirstSpawn = true;

    private int currentLane = 0;

    private readonly Queue<ActiveChunk> activeChunks = new();
    private float frontier;
    private int lastPrefabIndex = -1;

    private struct ActiveChunk
    {
        public GameObject go;
        public float start;
        public float end;
    }

    void Start()
    {
        frontier = PlayerDistance();
        RefreshChunks();
    }

    void Update()
    {
        RefreshChunks();
    }

    void RefreshChunks()
    {
        float playerDist = PlayerDistance();

        while (ChunksAheadOf(playerDist) < chunksAhead)
        {
            SpawnNext();
        }

        while (activeChunks.Count > 0 && activeChunks.Peek().end < playerDist - despawnBuffer)
        {
            Destroy(activeChunks.Dequeue().go);
        }
    }

    int ChunksAheadOf(float playerDist)
    {
        int count = 0;
        foreach (var c in activeChunks)
        {
            if (c.end > playerDist) count++;
        }
        return count;
    }

    void SpawnNext()
    {
        int index = PickPrefab();

        int dir = isFirstSpawn ? 0 : Random.Range(-1, 2);
        currentLane = Mathf.Clamp(currentLane + dir, -maxLanes, maxLanes);
        float lateral = currentLane * laneWidth;

        Vector3 pos = scrollAxis.normalized * frontier + LateralAxis * lateral;
        GameObject go = Instantiate(chunkPrefabs[index], pos, Quaternion.identity);
        isFirstSpawn = false;

        float length = Mathf.Max(GetChunkLength(go), 0.01f);

        activeChunks.Enqueue(new ActiveChunk
        {
            go = go,
            start = frontier,
            end = frontier + length
        });

        frontier += length;
    }

    int PickPrefab()
    {
        if (chunkPrefabs.Length == 1) return 0;

        int index;
        do { index = Random.Range(0, chunkPrefabs.Length); }
        while (index == lastPrefabIndex);

        lastPrefabIndex = index;
        return index;
    }

    float GetChunkLength(GameObject go)
    {
        if (go.TryGetComponent(out WorldChunk chunk))
            return chunk.length;

        // Fallback
        Bounds b = new(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            b.Encapsulate(r.bounds);
        return Vector3.Scale(b.size, scrollAxis.normalized).magnitude;
    }

    float PlayerDistance() => Vector3.Dot(player.position, scrollAxis.normalized);
    private Vector3 LateralAxis => Vector3.Cross(Vector3.up, scrollAxis.normalized);
}