using UnityEngine;

public class PortalEntrance : MonoBehaviour
{

    [Header("??? ????")]
    [SerializeField] private CameraTransition transition;
    private RoomFirstDungeonGenerator roomFirstDungeonGenerator;

    void Awake(){
        roomFirstDungeonGenerator = FindAnyObjectByType<RoomFirstDungeonGenerator>();
        transition = FindAnyObjectByType<CameraTransition>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") ) return;
        // if (exitPoint == null || transition == null) return;
        
        if (other.isTrigger){
            if (roomFirstDungeonGenerator.getindexCurrentFloor()+1 == roomFirstDungeonGenerator.Max_dungeonCount) {
                GameManager.Instance.EnterBoss();
            }else{
                Debug.Log("???");
            roomFirstDungeonGenerator.RunClearObject();
            
            // ???? ?��??? ++
            roomFirstDungeonGenerator.nextindexCurrentFloor();

            // ?��???? ???????
            var exitPoint = roomFirstDungeonGenerator.GetPlayerSpawnPos();

            // ???? ??????? ????
            roomFirstDungeonGenerator.SpawnObjects();

            

            // ???? ??? ???? ???��??? ?��???? ???????? ???
            transition.Play(new Vector3(exitPoint.x,exitPoint.y,0));


            

            Destroy(this);
            }
        }
    }


}