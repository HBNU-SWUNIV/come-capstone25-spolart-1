using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public Inventory Inventory { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    // 슬롯 데이터
    public OreData Ore { get; private set; }
    public int Count { get; private set; }
    public bool IsEmpty => Ore == null || Count <= 0;

    public void Bind(Inventory inventory, int slotIndex)
    {
        Inventory = inventory;
        SlotIndex = slotIndex;

        if (Inventory != null)
            Inventory.InventoryChanged += RefreshFromData; // 데이터가 바뀌면 자동 갱신

        RefreshFromData();
    }

    private void OnDestroy()
    {
        if (Inventory != null)
            Inventory.InventoryChanged -= RefreshFromData;
    }

    /// <summary>런타임 데이터에서 현재 슬롯 내용을 읽어와 UI 갱신</summary>
    public void RefreshFromData()
    {
        if (Inventory == null || SlotIndex < 0)
        {
            SetLocal(null, 0);
            return;
        }

        var item = Inventory.GetAt(SlotIndex);
        if (item == null || item.oreData == null || item.quantity <= 0)
        {
            SetLocal(null, 0);
        }
        else
        {
            SetLocal(item.oreData, item.quantity);
        }
        RefreshUI();
    }

    // 내부 필드만 갱신
    private void SetLocal(OreData ore, int count)
    {
        Ore = ore;
        Count = Mathf.Max(0, count);
    }

    // (필요 시) 외부에서 이 슬롯의 내용을 "데이터"에 쓴다
    public void WriteToData(OreData ore, int count)
    {
        if (Inventory == null || SlotIndex < 0) return;
        Inventory.SetAt(SlotIndex, ore, count); // 이벤트로 RefreshFromData가 다시 호출됨
    }

    // UI 렌더
    private void RefreshUI()
    {
        if (icon != null)
        {
            if (!IsEmpty)
            {
                icon.enabled = true;
                icon.sprite = Ore.OreIcon;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        if (countText != null)
            countText.text = IsEmpty ? string.Empty : Count.ToString();
    }
}