using UnityEngine;
using NavMeshPlus.Components;

public abstract class AbstractDungeonGenerator : MonoBehaviour
{
    public TilemapVisualizer tilemapVisualizer;
    public Vector2Int startPosition = Vector2Int.zero;

    public void GenerateDungeon(){
        tilemapVisualizer.Clear();
        RunProceduralGeneration();
    }

    public void Clear()
    {
        tilemapVisualizer.Clear();
    }

    public void SpawnObjects(){
        RunSpawnObjects();
    }
    public void ClearObjects(){
        RunClearObject();
    }

    public abstract void RunProceduralGeneration();
    public abstract void RunSpawnObjects();
    public abstract void RunClearObject();
}
