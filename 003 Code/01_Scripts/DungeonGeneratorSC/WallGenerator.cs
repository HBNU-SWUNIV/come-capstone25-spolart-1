using System;
using System.Collections.Generic;
using UnityEngine;

public static class WallGenerator 
{
    public static void CreateWalls(HashSet<Vector2Int> floorPositions, TilemapVisualizer tilemapVisualizer)
    {
        var basicWallPositions = FindWallsInDirections(floorPositions, Direction2D.cardinalDirectionsList);
        var cornerWallPositions = FindWallsInDirections(floorPositions, Direction2D.diagonalDirectionsList);
        CreateBasicWall(tilemapVisualizer, basicWallPositions);
        CreateConerWall(tilemapVisualizer, cornerWallPositions);
    }

    private static void CreateConerWall(TilemapVisualizer tilemapVisualizer, HashSet<Vector2Int> basicWallPositions)
    {
        foreach (var position in basicWallPositions)
        {
            tilemapVisualizer.PaintSingleBasicWall(position);
        }
    }

    private static void CreateBasicWall(TilemapVisualizer tilemapVisualizer, HashSet<Vector2Int> basicWallPositions)
    {
        foreach (var position in basicWallPositions)
        {
            tilemapVisualizer.PaintSingleBasicWall(position);
        }
    }

    private static HashSet<Vector2Int> FindWallsInDirections(HashSet<Vector2Int> floorPositions, List<Vector2Int> directionList){
        
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
        foreach (var Position in floorPositions) {
            foreach (var direction in directionList) {
                var neighbourPosition = Position + direction;
                if(floorPositions.Contains(neighbourPosition)==false){
                    wallPositions.Add(neighbourPosition);
                }
            }
        }
        return wallPositions;
    } 
}
