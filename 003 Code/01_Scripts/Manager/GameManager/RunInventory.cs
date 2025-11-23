using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/RunInventory")]
public class RunInventory : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public OreData ore; // 기존 OreData SO 참조
        public int count;
    }

    [SerializeField] private List<Entry> stacks = new();
    public IReadOnlyList<Entry> Stacks => stacks;

    public void Clear() => stacks.Clear();

    public void Add(OreData ore, int count)
    {
        if (ore == null || count <= 0)
        {
            return;
        }

        int idx = stacks.FindIndex(e => e.ore == ore);
        if (idx >= 0)
        {
            // 구조체 안의 값을 가져와 증가 후 다시 삽입
            var e = stacks[idx];
            e.count += count;
            stacks[idx] = e;
        }
        else
        {
            stacks.Add(new Entry { ore = ore, count = count });
        }
    }
}

