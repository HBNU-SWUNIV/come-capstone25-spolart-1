//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class corridorFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
//{
//    public int corridorLength = 14, corridorCount = 5;
//    [Range(0.1f, 1)]
//    public float roomPercent = 0.8f;
    
//    protected override void RunProceduralGeneration()
//    {
//        corridorFirstGeneration(); 
//    }

//    private void corridorFirstGeneration()
//    {
//        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
//        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

//        List<List<Vector2Int>> corridors = CreateCorridors(floorPositions, potentialRoomPositions);

//        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);

//        List<Vector2Int> deadEnds = FindAllDeadEnds(floorPositions);

//        CreateRoomsAtDeadEnd(deadEnds, roomPositions);

//        floorPositions.UnionWith(roomPositions);

//        for (int i = 0; i < corridors.Count; i++)
//        {
//            //corridors[i] = IncreaseCorridorSizeByOne(corridors[i]);
//            corridors[i] = IncreaseCorridorBrush3by3(corridors[i]);
//            floorPositions.UnionWith(corridors[i]);
//        }
        
//        tilemapVisualizer.PaintFloorTiles(floorPositions);
//        WallGenerator.CreateWalls(floorPositions, tilemapVisualizer);
//    }

//    private void CreateRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> roomFloors)
//    {
//        foreach (var position in deadEnds) {
//            if (roomFloors.Contains(position) == false) {
//                var room = RunRandomWalk(randomWalkParameters, position);
//                roomFloors.UnionWith(room);
//            }
//        }
//    }

//    private List<Vector2Int> FindAllDeadEnds(HashSet<Vector2Int> floorPositions)
//    {
//        List<Vector2Int> deadEnds = new List<Vector2Int>();
//        foreach (var position in floorPositions) {
//            int neighboursCount = 0;
//            foreach (var direction in Direction2D.cardinalDirectionsList) {
//                if (floorPositions.Contains(position + direction))
//                    neighboursCount++;
//            }
//            if (neighboursCount == 1) 
//                deadEnds.Add(position);
//        }   
//        return deadEnds;
//    }

//    private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
//    {
//        HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
//        int roomToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * roomPercent);

//        // 해쉬셋을 랜덤으로 정렬! .OrderBy(x => Guid.NewGuid()) 이거 자체가 랜덤으로 해쉬셋을 정렬하는 문법이라고 보면되는듯
//        // .Take(N) 앞에서 N개만큼의 리스트만 가져옴 
//        List<Vector2Int> roomToCreate = potentialRoomPositions.OrderBy(x => Guid.NewGuid()).Take(roomToCreateCount).ToList();

//        foreach (var roomPosition in roomToCreate) {
//            var roomFloor = RunRandomWalk(randomWalkParameters, roomPosition);
//            roomPositions.UnionWith(roomFloor);
//        }
//        return roomPositions;
//    }

//    private List<List<Vector2Int>> CreateCorridors(HashSet<Vector2Int> floorPositions, HashSet<Vector2Int> potentialRoomPositions)
//    {
//        var currentPosition = startPosition;
//        potentialRoomPositions.Add(currentPosition);
//        List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();


//        for (int i = 0; i < corridorCount; i++) {
//            var corridor = ProceduralGenerationAlgorithms.RandomWalkcorridor(currentPosition, corridorLength);
//            corridors.Add(corridor);
//            currentPosition = corridor[corridor.Count - 1];
//            potentialRoomPositions.Add(currentPosition);
//            floorPositions.UnionWith(corridor);
//        }

//        return corridors;
//    }

//    private List<Vector2Int> IncreaseCorridorSizeByOne(List<Vector2Int> corridor )
//    {
//        List<Vector2Int> newCorridor = new List<Vector2Int>();
//        Vector2Int previousDirection = Vector2Int.zero;
//        for (int i = 1; i < corridor.Count; i++) 
//        { 
//            Vector2Int directionFromCell = corridor[i] - corridor[i - 1];
//            if (previousDirection != Vector2Int.zero &&     // 처음시작하는 부분인지
//                directionFromCell != previousDirection)     // 방향이 바뀌었는지 == 코너인지
//            { 
//                // handle corner
//                for (int x = -1; x < 2; x++) 
//                {
//                    for (int y = -1; y < 2; y++) 
//                    {
//                        newCorridor.Add(corridor[i-1] + new Vector2Int(x,y)); // 코너위치에 3x3칸의 브러쉬로 그리기
//                    }
//                }
//            }
//            else 
//            {
//                //Add a single cell in the direction + 90 degrees 
//                //진행방향의 +90도 위치에 한칸 더 그리기
//                Vector2Int newCorriderTileOffset = GetDirection90From(directionFromCell);
//                newCorridor.Add(corridor[i - 1]);
//                newCorridor.Add(corridor[i - 1] + newCorriderTileOffset);
//            }
//        }
//        return newCorridor;
//    }

//    private Vector2Int GetDirection90From(Vector2Int directionFromCell)
//    {
//        if (directionFromCell == Vector2Int.up)
//            return Vector2Int.right;
//        if (directionFromCell == Vector2Int.right)
//            return Vector2Int.down;
//        if (directionFromCell == Vector2Int.down)
//            return Vector2Int.left;
//        if (directionFromCell == Vector2Int.left)
//            return Vector2Int.up;
//        return Vector2Int.zero;
//    }

//    private List<Vector2Int> IncreaseCorridorBrush3by3(List<Vector2Int> corridor)
//    {
//        List<Vector2Int> newCorridor = new List<Vector2Int>();

//        for (int i = 1; i < corridor.Count; i++)
//        {
//            for (int x = -1; x < 2; x++)
//            {
//                for (int y = -1; y < 2; y++)
//                {
//                    newCorridor.Add(corridor[i - 1] + new Vector2Int(x, y)); // 코너위치에 3x3칸의 브러쉬로 그리기
//                }
//            }
//        }
//        return newCorridor;
//    }
//}