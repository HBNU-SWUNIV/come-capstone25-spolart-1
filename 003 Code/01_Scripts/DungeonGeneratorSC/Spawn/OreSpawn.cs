using UnityEngine;

public class OreSpawn : MonoBehaviour
{
    public Transform[] Ore;
    public void Rand_SpawnOre(Vector2 position){
        int OreRand = Random.Range(1,32);
        switch(OreRand){
            case 1 : 
            Instantiate(Ore[0],new Vector3(position.x,position.y,0),Quaternion.identity).SetParent(this.transform); // 백금
            break;
            case >= 2 and <= 3:
            Instantiate(Ore[1],new Vector3(position.x,position.y,0),Quaternion.identity).SetParent(this.transform); // 금
            break;
            case >= 4 and <= 7:
            Instantiate(Ore[2],new Vector3(position.x,position.y,0),Quaternion.identity).SetParent(this.transform); // 은
            break;
            case >= 8 and <= 15:
            Instantiate(Ore[3],new Vector3(position.x,position.y,0),Quaternion.identity).SetParent(this.transform); // 구리
            break;
            case >= 16 and <= 31:
            Instantiate(Ore[4],new Vector3(position.x,position.y,0),Quaternion.identity).SetParent(this.transform); // 철
            break;
        }
    }
}
