using UnityEngine;

[System.Flags]
public enum ChunkPorts
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3
}

public class ChunkData : MonoBehaviour
{
    public ChunkPorts ports;

    public bool HasPort(ChunkPorts port)
    {
        return (ports & port) != 0;
    }
}
