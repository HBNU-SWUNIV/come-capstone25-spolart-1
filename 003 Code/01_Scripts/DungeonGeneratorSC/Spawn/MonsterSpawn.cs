using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawn : MonoBehaviour
{
    public Transform[] MonsterPrefabs;
    public List<Transform> availableMonsters = new List<Transform>();
    public Transform monsterRoot;

    public void Rand_SpawnMonster(Vector2 position, int curFloor)
    {
        var worldPos = new Vector3(position.x, position.y, 0f);


        int rand = Random.Range(0, availableMonsters.Count);
        Debug.Log(rand);
        if (availableMonsters.Count!=0){
            var t = Instantiate(availableMonsters[rand], worldPos, Quaternion.identity, monsterRoot);
            // ȸ������ (0, 0, 0)���� ���� ����
            t.localEulerAngles = Vector3.zero;
            t.localScale = Vector3.one;
        }
    }
}
