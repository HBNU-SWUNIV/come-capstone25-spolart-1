using System.Collections.Generic;
using UnityEngine;

// 내부 데이터 저장은 SO Runtime 인스턴스 사용
public class Inventory : MonoBehaviour
{
    // 단축 프로퍼티(직접 사용 금지: 항상 EnsureState() 통해 접근)
    private InventoryState S => InventoryStateHandle.Runtime;

    private void Start()
    {
        // 초기 갱신
        if (EnsureState())
        {
            RecalculateWeight();     // 무게 재계산
            InventoryChanged?.Invoke(); // UI에게 "바뀌었다" 알림
        }
    }

    public float CurrentWeight
    {
        get
        {
            if (!EnsureState()) return 0f;
            return S.currentWeight;
        }
    }

    public delegate void OnInventoryChanged();
    public event OnInventoryChanged InventoryChanged; // UI 업데이트용 이벤트

    // ---------- 공통 보조 ----------

    /// Runtime 상태 보장 + 리스트 고정길이화
    private bool EnsureState()
    {
        if (InventoryStateHandle.Runtime == null)
        {
            Debug.LogError("[Inventory] Runtime state is null. Ensure InventoryStateHandle is initialized before use.");
            return false;
        }

        if (S.items == null)
            S.items = new List<InventoryState.Item>();

        // maxSlot 보정 (필요 시 기본값)
        if (S.maxSlot <= 0) S.maxSlot = 20;

        // 리스트를 항상 maxSlot 길이로 유지 (빈칸은 null)
        if (S.items.Count != S.maxSlot)
        {
            if (S.items.Count < S.maxSlot)
            {
                while (S.items.Count < S.maxSlot)
                    S.items.Add(null);
            }
            else // Count > maxSlot인 경우 초과분 제거
            {
                S.items.RemoveRange(S.maxSlot, S.items.Count - S.maxSlot);
            }
        }
        return true;
    }

    private bool IsValidIndex(int index)
    {
        if (!EnsureState()) return false;
        return index >= 0 && index < S.maxSlot;
    }

    private int FindFirstEmptyIndex()
    {
        if (!EnsureState()) return -1;
        for (int i = 0; i < S.maxSlot; i++)
        {
            if (S.items[i] == null || S.items[i].oreData == null || S.items[i].quantity <= 0)
                return i;
        }
        return -1;
    }

    // ---------- 아이템 추가/제거 ----------

    // 외부에서 아이템을 추가할 때 쓰는 함수
    public bool AddItem(OreData oreData, int amount)
    {
        return TryAddItem(oreData, amount) > 0;
    }

    // 인벤토리에 아이템 추가 (고정 길이 슬롯 기반)
    private int TryAddItem(OreData oreData, int amount)
    {
        if (oreData == null || amount <= 0) return 0;
        if (!EnsureState()) return 0;

        // 무게 제한
        int canAddByWeight = GetMaxAddableWeight(oreData, amount);
        if (canAddByWeight <= 0) return 0;

        // 같은 OreData 스택 찾기
        InventoryState.Item existing = null;
        int existingIdx = -1;
        for (int i = 0; i < S.maxSlot; i++)
        {
            var it = S.items[i];
            if (it != null && it.oreData == oreData && it.quantity > 0)
            {
                existing = it;
                existingIdx = i;
                break;
            }
        }

        if (existing != null)
        {
            existing.quantity += canAddByWeight;
        }
        else
        {
            // 빈 슬롯에 새로 넣기
            int empty = FindFirstEmptyIndex();
            if (empty < 0) return 0; // 빈칸 없음
            S.items[empty] = new InventoryState.Item { oreData = oreData, quantity = canAddByWeight };
        }

        RecalculateWeight();
        InventoryChanged?.Invoke();
        return canAddByWeight;
    }

    // 특정 슬롯에서 amount 만큼 제거
    public int RemoveFromSlot(int slotIndex, int amount)
    {
        if (!IsValidIndex(slotIndex) || amount <= 0) return 0;

        var it = S.items[slotIndex];
        if (it == null || it.oreData == null || it.quantity <= 0) return 0;

        int removed = Mathf.Min(amount, it.quantity);
        it.quantity -= removed;

        if (it.quantity <= 0)
            S.items[slotIndex] = null;

        RecalculateWeight();
        InventoryChanged?.Invoke();
        Debug.Log("해당 아이템 버리기 [수량]: " + removed);
        return removed;
    }

    // 해당 슬롯 조회용 함수
    public (OreData ore, int count) PeekSlot(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return (null, 0);
        var it = S.items[slotIndex];
        if (it == null) return (null, 0);
        return (it.oreData, Mathf.Max(0, it.quantity));
    }

    // 현재 무게를 다시 계산
    public void RecalculateWeight()
    {
        if (!EnsureState()) return;

        float sum = 0f;
        for (int i = 0; i < S.maxSlot; i++)
        {
            var it = S.items[i];
            if (it?.oreData != null && it.quantity > 0)
                sum += it.oreData.Weight * it.quantity;
        }
        S.currentWeight = sum;
    }

    // 남은 무게 반환
    public float GetRemainingWeight() => Mathf.Max(0f, S.maxWeight - S.currentWeight);

    // 요청한 수량 중 무게 제한 때문에 실제로 넣을 수 있는 최대 개수 계산
    private int GetMaxAddableWeight(OreData oreData, int requested)
    {
        if (!EnsureState()) return 0;

        float remain = GetRemainingWeight();
        if (oreData.Weight <= 0f) return requested;

        int maxByW = Mathf.FloorToInt(remain / oreData.Weight);
        return Mathf.Clamp(maxByW, 0, requested);
    }

    // 현재 아이템 리스트 반환(참조)
    public List<InventoryState.Item> GetItems()
    {
        EnsureState();
        return S.items;
    }

    public float GetMaxCarryWeight()
    {
        EnsureState();
        return S.maxWeight;
    }

    // 인덱스의 아이템(없을 수 있음) 가져오기.
    public InventoryState.Item GetAt(int index)
    {
        if (!IsValidIndex(index)) return null;
        return S.items[index]; // 항상 고정 길이
    }

    // 인덱스에 아이템/수량을 설정(빈 슬롯은 null)
    public void SetAt(int index, OreData ore, int count)
    {
        if (!IsValidIndex(index)) return;

        if (ore == null || count <= 0)
        {
            S.items[index] = null;
        }
        else
        {
            if (S.items[index] == null) S.items[index] = new InventoryState.Item();
            S.items[index].oreData = ore;
            S.items[index].quantity = count;
        }

        InventoryChanged?.Invoke();
    }

    /// 슬롯 스왑
    public void SwapSlots(int a, int b)
    {
        if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return;

        var tmp = S.items[a];
        S.items[a] = S.items[b];
        S.items[b] = tmp;

        InventoryChanged?.Invoke();
    }

    // src의 스택을 dst로 이동. dst가 비면 이동, 같은 Ore면 합치기, 다르면 스왑.
    public void MoveAll(int src, int dst)
    {
        if (!IsValidIndex(src) || !IsValidIndex(dst) || src == dst) return;

        var s = S.items[src];
        var d = S.items[dst];
        if (s == null || s.oreData == null || s.quantity <= 0) return;

        if (d == null || d.oreData == null || d.quantity <= 0)
        {
            S.items[dst] = s;
            S.items[src] = null;
        }
        else if (d.oreData == s.oreData)
        {
            d.quantity += s.quantity;
            S.items[src] = null;
        }
        else
        {
            // 내용이 다르면 스왑
            var tmp = S.items[dst];
            S.items[dst] = s;
            S.items[src] = tmp;
        }

        InventoryChanged?.Invoke();
    }

    public void ClearAllSlots(bool keepSize = true, bool notify = true)
    {
        if (!EnsureState()) return;

        if (S.items != null)
        {
            if (keepSize)
            {
                for (int i = 0; i < S.items.Count; i++)
                {
                    S.items[i] = null;
                }
            }
            else
            {
                S.items.Clear();
            }
        }

        S.currentWeight = 0f;

        if (keepSize)
        {
            EnsureState();
        }

        if (notify)
        {
            InventoryChanged?.Invoke();
        }
    }

    public void ResetAtferRun()
    {
        ClearAllSlots(keepSize: true, notify: true);
    }
}
