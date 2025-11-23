using System.Collections.Generic;
using UnityEngine;

public class SpawnOre : MonoBehaviour
{
    // 광물 종류 열거형 선언
    public enum OreType { Iron, Cooper, Silver, Gold, Platinum };
    public List<OreData> oreDataList = new List<OreData>();

    [SerializeField] private GameObject orePrefab;

    private void Start()
    {
        SpawnOreObject();
    }

    public void SpawnOreObject()
    {
        //for (int i = 0; i < 1; i++) // oreDataList.Count
        //{
        //    var ore = SpawnOreFunc((OreType)i);
        //    var ore2 = SpawnOreFunc((OreType)i);

        //}
    }

    public Mineable SpawnOreFunc(OreType type)
    {
        var newOre = Instantiate(orePrefab).GetComponent<Mineable>();
        newOre.OreData = oreDataList[(int)(type)];
        return newOre;
    }
}
