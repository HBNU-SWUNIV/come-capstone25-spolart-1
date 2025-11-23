using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryState_Asset", menuName = "Game/Inventory State")]
public class InventoryState : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public OreData oreData; // 광물 데이터
        public int quantity;    // 해당 광물 수량
    }

    [Header("인벤토리 설정")]
    public int maxSlot = 20;
    public float maxWeight = 250f;

    [Header("런타임 상태")]
    public List<Item> items = new List<Item>(); // 현재 인벤토리에 담긴 아이템 목록
    public float currentWeight;

    public void RecalculateWeight()
    {
        float sum = 0f;
        foreach (var i in items)
        {
            if (i?.oreData != null)
            {
                sum += i.oreData.Weight * i.quantity;
            }
        }

        currentWeight = sum;
        Debug.Log("현재 무게:" + currentWeight);
    }

    public void Clear()
    {
        items.Clear();
        currentWeight = 0f;
    }
}
