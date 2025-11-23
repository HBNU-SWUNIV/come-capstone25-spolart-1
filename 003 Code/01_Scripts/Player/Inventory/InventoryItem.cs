using UnityEngine;

// 기본적으로 인벤토리 데이터를 담는 클래스

[System.Serializable]
public class InventoryItem
{
    public OreData oreData;
    public int quantity;        // 아이템 개수

    public InventoryItem(OreData data, int quantity) 
    {
        this.oreData = data;
        this.quantity = quantity;
    }

    public string Name => oreData.OreName;
    public Sprite Icon => oreData.OreIcon;
    public int Price => oreData.Price;
    public string Rarity => oreData.Rarity;
    public float Weight => oreData.Weight;
}
