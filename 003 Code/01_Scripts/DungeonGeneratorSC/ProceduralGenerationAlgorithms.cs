using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class ProceduralGenerationAlgorithms 
{
    // 단순 랜덤 워크 알고리즘
    public static HashSet<Vector2Int> SimpleRandomWalk(Vector2Int startposition, int walkLength) {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();

        path.Add(startposition);
        var previousPosition = startposition;

        for (int i = 0 ; i < walkLength ; i++) {
            var newPosition = previousPosition + Direction2D.GetRandomCardinalDirection();
            path.Add(newPosition);
            previousPosition = newPosition;
        }

        return path;
    }

    // 경계가 있는 랜덤 워크 알고리즘
    public static HashSet<Vector2Int> SimpleRandomWalkWithBounds(Vector2Int startposition, int walkLength, BoundsInt roomBounds)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();

        path.Add(startposition);
        var previousPosition = startposition;

        for (int i = 0; i < walkLength; i++)
        {
            var newPosition = previousPosition + Direction2D.GetRandomCardinalDirection();
            
            // 경계 내에 있는 경우에만 경로에 추가
            if (newPosition.x > roomBounds.xMin && newPosition.x < roomBounds.xMax &&
                newPosition.y > roomBounds.yMin && newPosition.y < roomBounds.yMax)
            {
                path.Add(newPosition);

                previousPosition = newPosition;
            }
        }

        return path;
    }

    // 복도 생성용 랜덤 워크
    public static List<Vector2Int> RandomWalkcorridor(Vector2Int startposition, int corridorLength){
        List<Vector2Int> corridor = new List<Vector2Int>();
        var direction = Direction2D.GetRandomCardinalDirection();
        var currentPosition = startposition;
        corridor.Add(currentPosition);

        for (int i = 0 ; i < corridorLength; i++) {
            currentPosition += direction;
            corridor.Add(currentPosition);
        }

        return corridor;
    }

    // 바이너리 스페이스 파티셔닝 알고리즘
    public static List<BoundsInt> BinarySpacePartitioning(BoundsInt spaceToSplit, int minWidth, int minHeight, int offset)
    {
        Queue<BoundsInt> roomsQueue = new Queue<BoundsInt>();
        List<BoundsInt> roomList = new List<BoundsInt>();
        roomsQueue.Enqueue(spaceToSplit);

        while (roomsQueue.Count > 0) 
        {
            var room = roomsQueue.Dequeue();
            if (room.size.y >= minHeight && room.size.x >= minWidth) 
            {
                if(Random.value < 0.5f)
                {
                    // 수직으로 분할
                    if(room.size.y >= minHeight * 2f)
                    {
                        SplitHorizontally(minHeight, roomsQueue, room, offset);
                    }
                    else if (room.size.x >= minWidth * 2f)
                    {
                        SplitVertically(minWidth, roomsQueue, room, offset);
                    }
                    else if (room.size.x >= minWidth && room.size.y >= minHeight)
                    {
                        roomList.Add(room);
                    }
                }
                else
                {
                    // 수평으로 분할
                    if (room.size.x >= minWidth * 2f)
                    {
                        SplitVertically(minWidth, roomsQueue, room, offset);
                    }
                    else if (room.size.y >= minHeight * 2f)
                    {
                        SplitHorizontally(minHeight, roomsQueue, room, offset);
                    }
                    else if (room.size.x >= minWidth && room.size.y >= minHeight)
                    {
                        roomList.Add(room);
                    }
                }
            }
        }
        return roomList;
    }

    // 수직 분할
    private static void SplitVertically(int minWidth, Queue<BoundsInt> roomsQueue, BoundsInt room, int offset)
    {
        // 최소 너비를 고려한 분할
        //var xSplit = Random.Range(minWidth, room.size.x- minWidth );
        var xSplit = Random.Range(3, room.size.x - 3);

        // 첫 번째 방 생성
        BoundsInt room1 = new BoundsInt(
            new Vector3Int(room.min.x + offset , room.min.y, room.min.z), 
            new Vector3Int(xSplit - offset * 2, room.size.y, room.size.z)
            );

        // 두 번째 방 생성
        BoundsInt room2 = new BoundsInt(
            new Vector3Int(room.min.x + xSplit + offset, room.min.y, room.min.z), 
            new Vector3Int(room.size.x - xSplit - offset * 2, room.size.y, room.size.z)
            );

        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    // 수평 분할
    private static void SplitHorizontally(int minHeight, Queue<BoundsInt> roomsQueue, BoundsInt room, int offset)
    {
        // 최소 높이를 고려한 분할
        //var ySplit = Random.Range(minHeight, room.size.y- minHeight );
        var ySplit = Random.Range(3, room.size.y - 3);

        // 첫 번째 방 생성
        BoundsInt room1 = new BoundsInt(
            new Vector3Int(room.min.x, room.min.y + offset, room.min.z), 
            new Vector3Int(room.size.x, ySplit - offset * 2, room.size.z)
            );

        // 두 번째 방 생성
        BoundsInt room2 = new BoundsInt(
            new Vector3Int(room.min.x, room.min.y + ySplit + offset, room.min.z),
            new Vector3Int(room.size.x, room.size.y - ySplit - offset * 2, room.size.z)
            );

        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }
}

// 2D 방향 관련 유틸리티 클래스
public static class Direction2D{
    // 기본 방향 (상하좌우)
    public static List<Vector2Int> cardinalDirectionsList = new List<Vector2Int> {
        new Vector2Int(0,1),  // 위
        new Vector2Int(1,0),  // 오른쪽
        new Vector2Int(0,-1), // 아래
        new Vector2Int(-1,0)  // 왼쪽
    };

    // 대각선 방향
    public static List<Vector2Int> diagonalDirectionsList = new List<Vector2Int> {
        new Vector2Int(1,1),   // 위-오른쪽
        new Vector2Int(1,-1),  // 오른쪽-아래
        new Vector2Int(-1,-1), // 아래-왼쪽
        new Vector2Int(-1,1)   // 왼쪽-위
    };

    // 8방향 (기본 + 대각선)
    public static List<Vector2Int> eightDirectionsList = new List<Vector2Int> {
        new Vector2Int(0,1),   // 위
        new Vector2Int(1,1),   // 위-오른쪽
        new Vector2Int(1,0),   // 오른쪽
        new Vector2Int(1,-1),  // 오른쪽-아래
        new Vector2Int(0,-1),  // 아래
        new Vector2Int(-1,-1), // 아래-왼쪽
        new Vector2Int(-1,0),  // 왼쪽
        new Vector2Int(-1,1)   // 왼쪽-위
    };

    // 랜덤 기본 방향 반환
    public static Vector2Int GetRandomCardinalDirection(){
        return cardinalDirectionsList[Random.Range(0, cardinalDirectionsList.Count)];
    }
}
