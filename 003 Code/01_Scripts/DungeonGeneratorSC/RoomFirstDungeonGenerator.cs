using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NavMeshPlus.Components;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : AbstractDungeonGenerator
{
    [Header("던전 배열 조건")]
    public int gapX = 0;
    public int gapY = 0;
    public bool centeredOrigin = false;
    public bool topToBottom = false;
    public int fixedColumns = 0;
    private int PlayerSpawnRoomIndex = 0 ;
    private Vector2Int PlayerspawnPos;
    
    // 던전의 전체 타일과 방 리스트를 함께 저장하는 구조체
    public struct DungeonData
    {
        public HashSet<Vector2Int> allTiles; // 전체 던전 타일 (통로 포함)
        public List<HashSet<Vector2Int>> rooms; // 던전 내 방 리스트
        public List<DelaunayTriangulation.Edge> mstEdges; // 방을 연결하는 MST 간선 리스트 추가
        public int endRoomIndex;
        public int startRoomIndex;
    }

    private List<HashSet<Vector2Int>> dungeonList = new List<HashSet<Vector2Int>>();
    private List<List<HashSet<Vector2Int>>> Dungeon_roomList = new List<List<HashSet<Vector2Int>>>();
    private List<List<DelaunayTriangulation.Edge>> Dungeon_mstEdges = new List<List<DelaunayTriangulation.Edge>>(); // MST 간선 리스트 추가
    private List<int> endRoomIndexList = new List<int>();
    private List<int> startRoomIndexList = new List<int>();
    
    // 최소 방 개수와 최대 시도 횟수를 설정하는 변수 추가
    [Header("던전 생성 조건")]
    [SerializeField] [Tooltip("선택 되는 방의 최소 넓이")] private int minSelectedRoomWidth = 4 ;
    [SerializeField] [Tooltip("선택 되는 방의 최소 높이")] private int minSelectedRoomHeight = 4;
    [SerializeField] [Tooltip("던전이 될 방을 고르는 갯수")] private Vector2Int NumKeepRooms_MinMax =new Vector2Int(5,7);
    [Tooltip("던전의 최대 층 수")] public int Max_dungeonCount = 5;   
    [SerializeField] [Tooltip("방 안에 생길 수 있는 최대 광물 수")] private int maxOresPerRoom = 20;
    [SerializeField] [Tooltip("방 안에 생길 수 있는 최대 몬스터 수")] private int maxMonstersPerRoom = 10;
    private int startFloor = 0;    

    [Header("물리 충돌 방 조건")]
    [SerializeField] [Tooltip("물리충돌을 일으킬 방의 갯수")] private int numberOfRooms = 20;

    // 최종적으로 남길 방의 개수

    // 방 배치를 위한 물리 충돌 시뮬레이션 시간
    [SerializeField] [Tooltip("물리충돌을 일으킬 때 걸리는 시간")] private float placementTime = 2.0f;

    // 방의 최소/최대 크기
    [SerializeField] [Tooltip("물리충돌을 일으킬 방은 랜덤 크기인데 가장 작은 방의 크기")] private Vector2 minRoomSize = new Vector2(5f, 5f);
    [SerializeField] [Tooltip("물리충돌을 일으킬 방은 랜덤 크기인데 가장 큰 방의 크기")] private Vector2 maxRoomSize = new Vector2(15f, 15f);
    [SerializeField] private GameObject roomPrefab;

    //[Range(0, 10)]
    //public int offset = 1;
    public bool randomWalkRooms = false;
    private int indexCurrentFloor = 0;

    
    [Header("스포너")]
    [SerializeField] private GameObject monsterSpawn;
    [SerializeField] private GameObject oreSpawn;
    [SerializeField] private GameObject SpecialSpawn;
    [SerializeField] private Transform endStairsPrefab;
    [SerializeField] private Transform bossPortalPrefab;
    [SerializeField] private Transform bucketPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject nav;
    
    [SerializeField]
    protected SimpleRandomWalkSO randomWalkParameters;

    private List<GameObject> generatedRooms = new List<GameObject>();
    private List<GameObject> keptRooms = new List<GameObject>();
    private List<BoundsInt> finalRoomBounds = new List<BoundsInt>();

    // 던전 배치 진행 상황
    [SerializeField, Range(0f, 1f)] private float generatorProgress;

    private struct ShapeInfo
    {
        public List<Vector2Int> normalized;
        public int width;
        public int height;
    }

    private void Awake()
    {
        if (player == null)
        {
            GameObject findPlayer = GameObject.FindGameObjectWithTag("Player");
            if (findPlayer != null)
            {
                player = findPlayer.transform;
                Debug.Log("[RoomFirstDungeonGenerator] 플레이어 연결 성공");
            }
        }
    }

    private void Start()
    {
        startFloor = PlayerPrefs.GetInt("StartFloor");
        indexCurrentFloor = startFloor-1;
        GenerateDungeon();
    }

    private void SpawnPlayer()
    {
        // **수정 : 플레이어를 Dontdestoryonload로 파괴되지 않도록 변경, 스폰 시 플레이어 캐릭터 위치만 변경
        if (player != null)
        {
            player.position = new Vector3(PlayerspawnPos.x + 0.5f, PlayerspawnPos.y + 0.5f, 0);
        }
        else
        {
            Debug.Log("플레이어 연결 안 되어있음.");
        }
    }

    private static ShapeInfo Normalize(HashSet<Vector2Int> shape)
    {
        var info = new ShapeInfo { normalized = new List<Vector2Int>() };
        if (shape == null || shape.Count == 0) { info.width = 0; info.height = 0; return info; }

        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);

        foreach (var p in shape)
        {
            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;
            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
        }


        info.width = max.x - min.x + 1;
        info.height = max.y - min.y + 1;
        

        info.normalized.Capacity = shape.Count;
        foreach (var p in shape)
            info.normalized.Add(new Vector2Int(p.x - min.x, p.y - min.y));

        return info;
    }

    private List<DungeonData> arrangeDungeon(List<DungeonData> dungeons)
    {
        var arrangedDungeons = new List<DungeonData>();
        if (dungeons == null || dungeons.Count == 0) return arrangedDungeons;
        
        var infos = new List<ShapeInfo>(dungeons.Count);
        int cellW = 0, cellH = 0;
        
        foreach (var d in dungeons)
        {
            var info = Normalize(d.allTiles);
            infos.Add(info);
            if (info.width > cellW) cellW = info.width;
            if (info.height > cellH) cellH = info.height;
        }
        if (cellW <= 0 || cellH <= 0) return arrangedDungeons;
        
        int n = infos.Count;
        int cols = (fixedColumns > 0) ? fixedColumns : Mathf.CeilToInt(Mathf.Sqrt(n));
        int rows = Mathf.CeilToInt((float)n / cols);
        
        int stepX = cellW + gapX;
        int stepY = cellH + gapY;

        Vector2Int origin = Vector2Int.zero;
        if (centeredOrigin)
        {
            int totalW = cols * cellW + (cols - 1) * gapX;
            int totalH = rows * cellH + (rows - 1) * gapY;
            origin = new Vector2Int(-totalW / 2, -totalH / 2);
        }

        for (int i = 0; i < n; i++)
        {
            int r = i / cols;
            int c = i % cols;
            int rr = topToBottom ? (rows - 1 - r) : r;
        
            int cellBaseX = origin.x + c * stepX;
            int cellBaseY = origin.y + rr * stepY;
            
            var info = infos[i];

            int offX = (cellW - info.width) / 2;
            int offY = (cellH - info.height) / 2;
            int baseX = cellBaseX + offX;
            int baseY = cellBaseY + offY;

            var originalDungeon = dungeons[i];
            var newDungeonData = new DungeonData();

            newDungeonData.allTiles = new HashSet<Vector2Int>();
            foreach (var p in originalDungeon.allTiles)
            {
                newDungeonData.allTiles.Add(new Vector2Int(p.x + baseX, p.y + baseY));
            }

            newDungeonData.rooms = new List<HashSet<Vector2Int>>();
            foreach (var room in originalDungeon.rooms)
            {
                var newRoom = new HashSet<Vector2Int>();
                foreach (var p in room)
                {
                    newRoom.Add(new Vector2Int(p.x + baseX, p.y + baseY));
                }
                newDungeonData.rooms.Add(newRoom);
            }
            
            // MST 간선 데이터도 이동된 위치로 갱신
            newDungeonData.mstEdges = new List<DelaunayTriangulation.Edge>();
            if (originalDungeon.mstEdges != null)
            {
                foreach (var edge in originalDungeon.mstEdges)
                {
                    Vector2 newV0 = new Vector2(edge.v0.x + baseX, edge.v0.y + baseY);
                    Vector2 newV1 = new Vector2(edge.v1.x + baseX, edge.v1.y + baseY);
                    newDungeonData.mstEdges.Add(new DelaunayTriangulation.Edge(newV0, newV1));
                }
            }

            newDungeonData.endRoomIndex = originalDungeon.endRoomIndex;
            newDungeonData.startRoomIndex = originalDungeon.startRoomIndex;
            
            arrangedDungeons.Add(newDungeonData);
        }
        return arrangedDungeons;
    }
    
    public override void RunProceduralGeneration()
    {
        // 정해진 시간만큼 물리 충돌 
        StartCoroutine(PlaceRooms());
        
    }

    // 방 배치 함수
    private IEnumerator PlaceRooms()
    {

        Report(0f); // 시작

        if (Max_dungeonCount == 0) { Report(1f); yield break; }

        // 방 많이많이 생성
        for (int i = 0; i < numberOfRooms; i++)
        {
            Vector2 randomPosition = Random.insideUnitCircle * 50f + new Vector2(1000,1000);
            GameObject newRoom = Instantiate(roomPrefab, randomPosition, Quaternion.identity, this.transform);
            newRoom.name = "Room_" + i;

            float roomWidth = Random.Range(minRoomSize.x, maxRoomSize.x);
            float roomHeight = Random.Range(minRoomSize.y, maxRoomSize.y);
            newRoom.transform.localScale = new Vector3(roomWidth, roomHeight, 1f);

            Rigidbody2D rb = newRoom.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = newRoom.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0;
            rb.linearDamping = 10f;

            generatedRooms.Add(newRoom);

            if (numberOfRooms > 0) Report(0.15f * ((i + 1f) / numberOfRooms));
            yield return null; // 프레임 분산
        }

        // 물리 정렬 대기
        yield return new WaitForSeconds(placementTime);
        Report(0.20f);

        foreach (GameObject room in generatedRooms)
        {
            if (room != null)
            {
                Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }
        }
        
        dungeonList.Clear();
        Dungeon_roomList.Clear();
        Dungeon_mstEdges.Clear(); // 추가: MST 간선 목록 초기화
        endRoomIndexList.Clear();
        startRoomIndexList.Clear();

        var dungeonsToArrange = new List<DungeonData>();

        // 총 층 수 계산
        int loopCount = Mathf.Max(1, (Max_dungeonCount - startFloor) + 1);
        int loopIndex = 0;

        // 각 층 생성 루프
        for (int i = startFloor ; i < Max_dungeonCount; i++){
            SelectAndCleanUpRooms();

            finalRoomBounds = GetKeptRoomsBounds();
            

            DungeonData newDungeon;
            newDungeon = CreateDungeon();
            finalRoomBounds.Clear();

            dungeonsToArrange.Add(newDungeon);

            // 방 변형, 중앙으로 모으기 (재배치)
            foreach (var room in generatedRooms)
            {
                // 2. 위치와 크기를 변형하고 중앙으로 모으기
                float recenterFactor = 0.5f; // 중앙으로 모으는 비율
                Vector3 originalPosition = room.transform.position;
                Vector3 targetPosition = Vector3.Lerp(originalPosition, new Vector2(1000,1000), recenterFactor);
                room.transform.position = targetPosition + (Vector3)Random.insideUnitCircle * 2f; // 무작위 오프셋 추가
                
                float newWidth = Random.Range(minRoomSize.x, maxRoomSize.x);
                float newHeight = Random.Range(minRoomSize.y, maxRoomSize.y);
                room.transform.localScale = new Vector3(newWidth, newHeight, 1f);

                // 3. 물리 연산을 위해 bodyType을 Dynamic으로 변경
                Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }

            yield return new WaitForSeconds(placementTime/2);

            // 5. 다시 bodyType을 Static으로 변경하여 연산 중지
            foreach (GameObject room in generatedRooms)
            {
                if (room != null)
                {
                    Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Static;
                    }
                }
            }

            // 층 단위 진행도: 0.20 → 0.85 선형 분배
            //loopIndex++;
            //float t = 0.20f + 0.65f * (loopIndex / (float)loopCount);
            //Report(t);
        }

        Report(0.85f);

        SelectAndCleanUpRooms();
        finalRoomBounds = GetKeptRoomsBounds();

        DungeonData lastDungeon = CreateDungeon();
        finalRoomBounds.Clear();

        dungeonsToArrange.Add(lastDungeon);

        foreach (GameObject room in generatedRooms)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }
        Report(0.90f);

        var arrangedDungeons = arrangeDungeon(dungeonsToArrange);

        dungeonList = arrangedDungeons.Select(d => d.allTiles).ToList();
        Dungeon_roomList = arrangedDungeons.Select(d => d.rooms).ToList();
        Dungeon_mstEdges = arrangedDungeons.Select(d => d.mstEdges).ToList(); // 추가: MST 간선 목록 저장
        endRoomIndexList = arrangedDungeons.Select(d => d.endRoomIndex).ToList();
        startRoomIndexList = arrangedDungeons.Select(d => d.startRoomIndex).ToList();

        foreach (var item in dungeonList)
        {
            tilemapVisualizer.PaintFloorTiles(item);
            WallGenerator.CreateWalls(item, tilemapVisualizer);

            yield return null;
        }

        PlayerspawnPos = GetPlayerSpawnPos();
        SpawnObjects();
        SpawnPlayer();
        Report(0.98f);

        LoadSceneManager.Instance?.MarkGenerationComplete();
    }

    private void SelectAndCleanUpRooms()
    {
    // 1. 남길 방을 담을 리스트를 생성합니다.
    List<GameObject> roomsToKeep = new List<GameObject>();

    // 2. 크기 조건을 만족하는 방들만 선택 가능한 리스트에 담습니다.
    List<GameObject> availableRooms = generatedRooms.Where(r => r != null && 
                                                         r.transform.localScale.x >= minSelectedRoomWidth && 
                                                         r.transform.localScale.y >= minSelectedRoomHeight).ToList();
                                                         

    int rand = Random.Range(NumKeepRooms_MinMax.x, NumKeepRooms_MinMax.y+1);
    // 3. 남길 개수만큼 무작위로 방을 선택합니다.
    for (int i = 0; i < rand && availableRooms.Count > 0; i++)
    {
        int randomIndex = Random.Range(0, availableRooms.Count);
        GameObject selectedRoom = availableRooms[randomIndex];
        roomsToKeep.Add(selectedRoom);
        availableRooms.RemoveAt(randomIndex); // 중복 선택 방지를 위해 리스트에서 제거
    }
    
    // 4. 최종적으로 선택된 방을 제외한 나머지 방을 모두 제거합니다.
    // 기존에 생성된 모든 방을 대상으로 파괴를 진행합니다.
    // foreach (GameObject room in generatedRooms)
    // {
    //     if (room != null && !roomsToKeep.Contains(room))
    //     {
    //         Destroy(room);
    //     }
    // }
    keptRooms.Clear();
    // 5. 남은 방을 `keptRooms` 리스트에 저장합니다.
    keptRooms = roomsToKeep;
    Debug.Log(keptRooms.Count + "개의 방 배치 및 정리 완료!");
    }

    private List<BoundsInt> GetKeptRoomsBounds()
    {
        List<BoundsInt> roomBounds = new List<BoundsInt>();
        
        foreach (GameObject room in keptRooms)
        {
            Vector3Int position = new Vector3Int(
                Mathf.RoundToInt(room.transform.position.x),
                Mathf.RoundToInt(room.transform.position.y),
                0);

            Vector3Int size = new Vector3Int(
                Mathf.RoundToInt(room.transform.localScale.x),
                Mathf.RoundToInt(room.transform.localScale.y),
                1);

            roomBounds.Add(new BoundsInt(position, size));
            //Destroy(room);
        }

        return roomBounds;
    }

    public Vector2Int GetPlayerSpawnPos(){
        // if (Dungeon_roomList.Count <= indexCurrentFloor)
        // {
        //     Debug.LogError("Error: indexCurrentFloor is out of bounds for Dungeon_roomList.");
        //     return ;
        // }
        // var currentDungeonRooms = Dungeon_roomList[indexCurrentFloor];
        // if (currentDungeonRooms.Count <= 1)
        // {
        //     Debug.LogWarning("Warning: Not enough rooms to set a separate spawn and end room.");
        //     return;
        // }
        // PlayerSpawnRoomIndex = startRoomIndexList[indexCurrentFloor];
        int tm = indexCurrentFloor-(startFloor-1);
        Debug.Log("시작 방 인덱스 리스트 갯수" + startRoomIndexList.Count + ", 참조하려는 인덱스" + tm);
        var playerSpawnRoomTiles = Dungeon_roomList[indexCurrentFloor-(startFloor-1)][startRoomIndexList[indexCurrentFloor-(startFloor-1)]];
        
        // 플레이어 스폰 위치를 방의 첫 번째 타일이 아닌, 무작위 타일로 변경 (선택 사항)
        PlayerspawnPos = playerSpawnRoomTiles.First();
        
        // **수정 : 플레이어를 Dontdestoryonload로 파괴되지 않도록 변경, 스폰 시 플레이어 캐릭터 위치만 변경
        if (player != null)
        {
            player.position = new Vector3(PlayerspawnPos.x , PlayerspawnPos.y , 0);
        }
        else
        {
            Debug.Log("플레이어 연결 안 되어있음.");
        }

        return PlayerspawnPos;
    }

    public override void RunSpawnObjects()
    {     
        var currentDungeonRooms = Dungeon_roomList[indexCurrentFloor-(startFloor-1)];
        var endRoomTiles = Dungeon_roomList[indexCurrentFloor-(startFloor-1)][endRoomIndexList[indexCurrentFloor-(startFloor-1)]];

        // 엔드 계단 위치를 정중앙
        var endStairsPos = endRoomTiles.First();

        if (indexCurrentFloor+1==Max_dungeonCount){
            Instantiate(bossPortalPrefab, new Vector3(endStairsPos.x+1-0.5f, endStairsPos.y-0.5f, 0), Quaternion.identity).SetParent(SpecialSpawn.transform,true);

            Instantiate(bucketPrefab, new Vector3(endStairsPos.x-1-0.5f, endStairsPos.y-0.5f, 0), Quaternion.identity).SetParent(SpecialSpawn.transform,true);
        }else{
            Instantiate(endStairsPrefab, new Vector3(endStairsPos.x-0.5f, endStairsPos.y-0.5f, 0), Quaternion.identity).SetParent(SpecialSpawn.transform,true);
        }

        // 층에 맞는 몬스터 리스트에 추가
        if (monsterSpawn.GetComponent<MonsterSpawn>().availableMonsters.Count != 0)monsterSpawn.GetComponent<MonsterSpawn>().availableMonsters.Clear();
        foreach (var item in monsterSpawn.GetComponent<MonsterSpawn>().MonsterPrefabs)
        {
            if(item.gameObject.GetComponent<Monster>().monsterData.appearFloor.Contains(indexCurrentFloor)){
                monsterSpawn.GetComponent<MonsterSpawn>().availableMonsters.Add(item);
            }
        }

        for (int i = 0; i < currentDungeonRooms.Count; i++)
        {
            if (i == startRoomIndexList[indexCurrentFloor-(startFloor-1)] || i == endRoomIndexList[indexCurrentFloor-(startFloor-1)])
            {
                continue;
            }

            
            List<Vector2Int> roomTiles = currentDungeonRooms[i].ToList();
            // Shuffling Logic
            for (int j = 0; j < roomTiles.Count; j++)
            {
                Vector2Int temp = roomTiles[j];
                int randomIndex = Random.Range(j, roomTiles.Count);
                roomTiles[j] = roomTiles[randomIndex];
                roomTiles[randomIndex] = temp;
            }

            int spawnedOreCount = 0;
            int spawnedMonsterCount = 0;

            foreach (var position in roomTiles)
            {
                if (spawnedOreCount >= maxOresPerRoom && spawnedMonsterCount >= maxMonstersPerRoom)
                {
                    break; // 모두 생성했으면 루프를 종료합니다.
                }

                int rand = Random.Range(1, 101);

                if (1 <= rand && rand <= 10 && spawnedOreCount < maxOresPerRoom)
                {
                    oreSpawn.GetComponent<OreSpawn>().Rand_SpawnOre(new Vector2(position.x, position.y));
                    spawnedOreCount++;
                }
                else if (11 <= rand && rand <= 12 && spawnedMonsterCount < maxMonstersPerRoom)
                {
                    monsterSpawn.GetComponent<MonsterSpawn>().Rand_SpawnMonster(new Vector2(position.x, position.y), indexCurrentFloor);
                    spawnedMonsterCount++;
                }
            }
        }

        nav.GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    public override void RunClearObject()
    {
        for (int i = oreSpawn.transform.childCount - 1; i >= 0; i--)
            Destroy(oreSpawn.transform.GetChild(i).gameObject);
        for (int i = monsterSpawn.transform.childCount - 1; i >= 0; i--)
            Destroy(monsterSpawn.transform.GetChild(i).gameObject);
        // for (int i = SpecialSpawn.transform.childCount - 1; i >= 0; i--)
        //     Destroy(SpecialSpawn.transform.GetChild(i).gameObject); 

        // nav.GetComponent<NavMeshSurface>().RemoveData();
    }

    private DungeonData CreateDungeon()
    {
        //int rand = Random.Range(minRoomCount, maxRoomCount+1);
        

        var roomList = finalRoomBounds;
        // var roomList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, new Vector3Int
        //     (dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight, offset);

        //if (roomList.Count < rand ) return new DungeonData{allTiles = null, rooms = null};

        //RemoveRandomRooms(roomList,rand);
        
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        List<HashSet<Vector2Int>> roomsInDungeon = new List<HashSet<Vector2Int>>();

        

        
        Dictionary<Vector2Int, HashSet<Vector2Int>> Center_Floor_Set = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

        if (randomWalkRooms)
        {
            floor = CreateRoomsRandomly(roomList, Center_Floor_Set);
        }
        else
        {
            floor = CreateSimpleRooms(roomList, Center_Floor_Set);
        }

        (HashSet<Vector2Int> corridors, List<DelaunayTriangulation.Edge> mstEdges) = ConnectRooms(Center_Floor_Set); // 반환 값 수정
        
        roomsInDungeon = Center_Floor_Set.Values.ToList();

        int startindex = Random.Range(0,roomsInDungeon.Count);

    
        var currentDungeonRooms = roomsInDungeon;
        var currentMSTEdges = mstEdges;

  
        // 1. 방 연결 그래프 생성 (인접 리스트)
        int numRooms = currentDungeonRooms.Count;
        List<KeyValuePair<int, float>>[] adj = new List<KeyValuePair<int, float>>[numRooms];
        for (int i = 0; i < numRooms; i++)
        {
            adj[i] = new List<KeyValuePair<int, float>>();
        }

        // MST 간선의 끝점(Vector2)을 해당 방의 인덱스로 변환하여 그래프 구성
        foreach (var edge in currentMSTEdges)
        {
            int roomIndexA = -1;
            int roomIndexB = -1;
            
            // v0가 속한 방 인덱스 찾기
            for (int i = 0; i < numRooms; i++)
            {
                // Vector2를 Vector2Int로 변환하여 방 타일 목록과 비교 (정확도 확보)
                Vector2Int v0Int = new Vector2Int(Mathf.RoundToInt(edge.v0.x), Mathf.RoundToInt(edge.v0.y));
                if (currentDungeonRooms[i].Contains(v0Int))
                {
                    roomIndexA = i;
                    break;
                }
            }
            
            // v1이 속한 방 인덱스 찾기
            for (int i = 0; i < numRooms; i++)
            {
                // Vector2를 Vector2Int로 변환하여 방 타일 목록과 비교
                Vector2Int v1Int = new Vector2Int(Mathf.RoundToInt(edge.v1.x), Mathf.RoundToInt(edge.v1.y));
                if (currentDungeonRooms[i].Contains(v1Int))
                {
                    roomIndexB = i;
                    break;
                }
            }

            if (roomIndexA != -1 && roomIndexB != -1 && roomIndexA != roomIndexB)
            {
                // Edge의 weight는 Vector2.Distance(v0, v1) (방 간의 직선 통로 길이)
                adj[roomIndexA].Add(new KeyValuePair<int, float>(roomIndexB, edge.weight));
                adj[roomIndexB].Add(new KeyValuePair<int, float>(roomIndexA, edge.weight));
            }
        }
        

        // 2. Dijkstra 알고리즘을 사용하여 최단 경로 거리 계산 (O(V^2) 구현)
        // 우선순위 큐 없이, 배열 기반으로 매번 최소 거리를 가진 노드를 찾습니다.
        
        float[] distance = new float[numRooms];
        bool[] visited = new bool[numRooms];

        for (int i = 0; i < numRooms; i++)
        {
            distance[i] = float.MaxValue;
            visited[i] = false;
        }
        distance[startindex] = 0;

        for (int count = 0; count < numRooms; count++)
        {
            // 2-1. 최소 거리를 가진 미방문 노드 찾기
            float min_dist = float.MaxValue;
            int u = -1;

            for (int i = 0; i < numRooms; i++)
            {
                if (!visited[i] && distance[i] < min_dist)
                {
                    min_dist = distance[i];
                    u = i;
                }
            }

            // 모든 노드가 방문되었거나, 연결된 노드가 더 이상 없는 경우 종료
            if (u == -1) break;

            // 2-2. 현재 노드 방문 처리
            visited[u] = true;

            // 2-3. 인접 노드의 거리 갱신 (Relaxation)
            foreach (var neighbor in adj[u])
            {
                int v = neighbor.Key;
                float weight = neighbor.Value;

                // 미방문 노드이고, 현재 거리(distance[u] + weight)가 기존 거리(distance[v])보다 짧으면 갱신
                if (!visited[v] && distance[u] != float.MaxValue && distance[u] + weight < distance[v])
                {
                    distance[v] = distance[u] + weight;
                }
            }
        }
        
        // 3. 최단 경로 거리가 가장 긴 방을 End Stair 방으로 선택
        float maxDistance = -1f;
        int endRoomIndex = -1;

        for (int i = 0; i < numRooms; i++)
        {
            if (i == startindex)
            {
                continue;
            }

            // 연결되지 않은 방은 (float.MaxValue) 제외하고,
            // 연결된 방 중 거리가 최대인 방을 찾습니다.
            if (distance[i] != float.MaxValue && distance[i] > maxDistance)
            {
                maxDistance = distance[i];
                endRoomIndex = i;
            }
        }
        
        if (endRoomIndex == -1)
        {
            // 예외 처리: 엔드룸을 찾지 못한 경우 (fallback: 기존 직선거리 최대 방 선택 로직 재사용)
            Debug.LogWarning("Warning: Could not find end room based on shortest path. Falling back to max Euclidean distance.");
            
            maxDistance = 0f;
            endRoomIndex = -1;
            var playerSpawnRoomTiles = currentDungeonRooms[PlayerSpawnRoomIndex];
            
            for (int i = 0; i < currentDungeonRooms.Count; i++)
            {
                if (i == PlayerSpawnRoomIndex)
                {
                    continue;
                }

                var spawnRoomCenter = playerSpawnRoomTiles.First();
                var currentRoomCenter = currentDungeonRooms[i].First();
                float dist = Vector2Int.Distance(spawnRoomCenter, currentRoomCenter);

                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    endRoomIndex = i;
                }
            }
        }

        HashSet<Vector2Int> endRoom = new HashSet<Vector2Int>();
        List<Vector2Int> allRoomCenters = Center_Floor_Set.Keys.ToList();

        BoundsInt endRoomBound = new BoundsInt(new Vector3Int(roomsInDungeon[endRoomIndex].First().x - 4, roomsInDungeon[endRoomIndex].First().y - 4 , 0),
            new Vector3Int(10,10,1));

        endRoom.Add(new Vector2Int((int)endRoomBound.center.x,(int)endRoomBound.center.y));
        foreach (var item in endRoomBound.allPositionsWithin)
        {
            endRoom.Add(new Vector2Int(item.x, item.y));
        }
        floor.ExceptWith(roomsInDungeon[endRoomIndex]);
        floor.UnionWith(corridors);
        floor.UnionWith(endRoom);
        
        roomsInDungeon[endRoomIndex] = endRoom;


        Debug.Log("start="+startindex+", end="+ endRoomIndex);
        return new DungeonData
        {
            allTiles = floor,
            rooms = roomsInDungeon,
            mstEdges = mstEdges, // MST 간선 데이터 저장
            startRoomIndex = startindex,
            endRoomIndex = endRoomIndex
        };
    }

    public void RemoveRandomRooms(List<BoundsInt> roomList, int rand)
    {
        

        // 제거할 방의 개수를 계산합니다.
        int roomsToRemoveCount = roomList.Count - rand;
        
        // 제거할 방의 인덱스를 저장할 리스트를 생성합니다.
        List<int> indicesToRemove = new List<int>();
        
        // 제거할 방의 인덱스를 무작위로 뽑아냅니다.
        while (indicesToRemove.Count < roomsToRemoveCount)
        {
            int randomIndex = Random.Range(0, roomList.Count);
            if (!indicesToRemove.Contains(randomIndex))
            {
                indicesToRemove.Add(randomIndex);
            }
        }
        
        // 인덱스를 내림차순으로 정렬하여, 앞에서부터 제거할 때 인덱스 오류가 발생하지 않도록 합니다.
        indicesToRemove.Sort((a, b) => b.CompareTo(a));

        // 해당 인덱스의 방들을 원본 리스트에서 제거합니다.
        foreach (int index in indicesToRemove)
        {
            roomList.RemoveAt(index);
        }
    }
    
    private HashSet<Vector2Int> CreateRoomsRandomly(List<BoundsInt> roomList, Dictionary<Vector2Int, HashSet<Vector2Int>> Center_Floor_Set)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        for (int i = 0; i < roomList.Count; i++)
        {
            var roomBounds = roomList[i];
            var roomCenter = (Vector2Int)Vector3Int.RoundToInt(roomList[i].center);
            var roomFloor = RunRandomWalkWithBound(randomWalkParameters, roomCenter, roomBounds);
            floor.UnionWith(roomFloor);
            Center_Floor_Set[new Vector2Int((int)roomList[i].center.x, (int)roomList[i].center.y)] = roomFloor;
        }
        return floor;
    }

    protected HashSet<Vector2Int> RunRandomWalkWithBound(SimpleRandomWalkSO parameters, Vector2Int position, BoundsInt roomBounds)
    {
        var currentPosition = position;
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        
        for (int i = 0; i < parameters.iterations; i++)
        {
            var path = ProceduralGenerationAlgorithms.SimpleRandomWalkWithBounds(currentPosition, parameters.walkLength, roomBounds);
            floorPositions.UnionWith(path);
            
            if (parameters.startRandomlyEachIteration)
                currentPosition = floorPositions.ElementAt(Random.Range(0, floorPositions.Count));
        }
        return floorPositions;
    }

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomList, Dictionary<Vector2Int, HashSet<Vector2Int>> Center_Floor_Set)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomList)
        {
            HashSet<Vector2Int> roomfloor = new HashSet<Vector2Int>();
            for (int col = 0; col < room.size.x; col++)
            {
                for (int row = 0; row < room.size.y; row++)
                {
                    Vector2Int position = new Vector2Int(room.min.x + col, room.min.y + row);
                    floor.Add(position);
                    roomfloor.Add(position);
                }
            }
            Center_Floor_Set[new Vector2Int((int)room.center.x, (int)room.center.y)] = roomfloor;
        }
        return floor;
    }
    
    // ConnectRooms가 이제 (통로 타일, MST 간선 목록) 튜플을 반환합니다.
    private (HashSet<Vector2Int>, List<DelaunayTriangulation.Edge>) ConnectRooms(Dictionary<Vector2Int, HashSet<Vector2Int>> Center_Floor_Set)
    {
        List<Vector2Int> roomCenterList = new List<Vector2Int>(Center_Floor_Set.Keys);
        List<Vector2> vector2Points = new List<Vector2>();

        if (roomCenterList.Count > 0)
        {
            foreach (var roomCenter in roomCenterList)
            {
                if (Center_Floor_Set[roomCenter].Count == 0) continue; 
                
                List<Vector2Int> room_Floor = new List<Vector2Int>(Center_Floor_Set[roomCenter]);
                // int randomPosIdx = Random.Range(0, room_Floor.Count);
                vector2Points.Add(room_Floor[0]);
            }
        }

        if (vector2Points.Count < 2) return (new HashSet<Vector2Int>(), new List<DelaunayTriangulation.Edge>());
        
        var triagles = DelaunayTriangulation.CreateDelaunay(vector2Points);
        List<DelaunayTriangulation.Edge> edges = new List<DelaunayTriangulation.Edge>();

        foreach (var tri in triagles)
        {
            edges.Add(new DelaunayTriangulation.Edge(tri.a, tri.b));
            edges.Add(new DelaunayTriangulation.Edge(tri.b, tri.c));
            edges.Add(new DelaunayTriangulation.Edge(tri.c, tri.a));
        }

        List<DelaunayTriangulation.Edge> mst = DelaunayTriangulation.GenerateMST(vector2Points, edges);
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        foreach (var item in mst)
        {
            Vector2Int Room1 = new Vector2Int((int)item.v0.x, (int)item.v0.y);
            Vector2Int Room2 = new Vector2Int((int)item.v1.x, (int)item.v1.y);
            List<Vector2Int> newCorridor = CreateCorridor(Room1, Room2);
            List<Vector2Int> new3x3Corridor = IncreaseCorridorBrush3by3(newCorridor);
            corridors.UnionWith(new3x3Corridor);
        }
        return (corridors, mst); // MST 간선 목록을 반환합니다.
    }

    private List<Vector2Int> IncreaseCorridorBrush3by3(List<Vector2Int> corridor)
    {
        List<Vector2Int> newCorridor = new List<Vector2Int>();
        if (corridor.Count == 0) return newCorridor;
        
        for (int i = 1; i < corridor.Count; i++)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    newCorridor.Add(corridor[i - 1] + new Vector2Int(x, y));
                }
            }
        }
        return newCorridor;
    }

    private List<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        List<Vector2Int> corridor = new List<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);
        int randomvar = 0;

        while (position.y != destination.y || position.x != destination.x)
        {
            if (Random.Range(0, 10) + randomvar >= 5)
            {
                if (destination.y > position.y)
                {
                    position += Vector2Int.up;
                }
                else if (destination.y < position.y)
                {
                    position += Vector2Int.down;
                }
                else { randomvar -= 5; }
            }
            else
            {
                if (destination.x > position.x)
                {
                    position += Vector2Int.right;
                }
                else if (destination.x < position.x)
                {
                    position += Vector2Int.left;
                }
                else { randomvar += 5; }
            }
            corridor.Add(position);
        }
        return corridor;
    }

    public int getindexCurrentFloor(){
        return indexCurrentFloor;
    }

    public void nextindexCurrentFloor(){
        indexCurrentFloor++;
    }

    // 던전 배치 진행 상황 보고 함수
    private void Report(float t)
    {
        generatorProgress = Mathf.Clamp01(t);
        if (LoadSceneManager.Instance != null)
            LoadSceneManager.Instance.ReportGeneratorProgress(generatorProgress);
    }
}