using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TilemapVisualizer : MonoBehaviour
{
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase emptyTile;    /// 이거 추가
    
    [SerializeField] private GameObject monsterSpawn;
    [SerializeField] private GameObject oreSpawn;
    

    
    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {
        foreach (var position in floorPositions)
        {
            PaintSingleTile(position, true);
        }   
    }

    public void PaintSingleBasicWall(Vector2Int position)
    {
        PaintSingleTile(position, false);
    }

    private void PaintSingleTile(Vector2Int position, bool isFloor)
    {
        var tilePosition = new Vector3Int(position.x, position.y, 0);


        if (isFloor==true){
            floorTilemap.SetTile(tilePosition, floorTile); 
            wallTilemap.SetTile(tilePosition, emptyTile); 
        }
        else{
            wallTilemap.SetTile(tilePosition, wallTile);   
            floorTilemap.SetTile(tilePosition, floorTile); 
        }                                     
    }


    public void Clear()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }
}