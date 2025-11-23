using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class BuffShopUIController : MonoBehaviour
{
    [Header("목록/프리팹")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private BuffListItem listItemPrefab;

    [Header("상세정보")]
    [SerializeField] private BuffDetailPanel detailPanel;


    private readonly List<BuffListItem> _items = new();
    private BuffListItem _selected;

    // TP002 시설 ID
    private const string BuffUnlockFacilityId = "TP002";

    private void Start()
    {
        BuildList();
        if (detailPanel) detailPanel.Clear();
    }

    private void OnEnable()
    {
        // 패널이 활성화될 때 (창을 열 때) 가격을 갱신
        BuildList();
    }

    private void BuildList()
    {
        // 기존 아이템 클리어
        for (int i = listContainer.childCount - 1; i >= 0; i--)
            Destroy(listContainer.GetChild(i).gameObject);
        _items.Clear();

        // 런버프 매니저에서 데이터 로드
        var mgr = DungeonRunBuffManager.Instance;
        List<BuffData> source = null;
        if (mgr != null && mgr.Catalog != null && mgr.Catalog.Count > 0)
        {
            source = new List<BuffData>(mgr.Catalog);
        }

        // 현재 TP002 시설 레벨을 조회합니다.
        int currentTP002Level = DataManager.Instance != null
            ? DataManager.Instance.GetFacilityLevel(BuffUnlockFacilityId)
            : 0;

        foreach (var data in source)
        {
            if (data == null) continue;

            // ★ 버프 잠금 로직: data.requiredTP002Level 필드가 BuffData에 있다고 가정
            if (data.requiredTP002Level > currentTP002Level) continue;

            var item = Instantiate(listItemPrefab, listContainer);
            item.Setup(data, OnClickItem);
            _items.Add(item);
        }

        if (_items.Count > 0)
        {
            OnClickItem(_items[0]);
        }
    }

    private void OnClickItem(BuffListItem item)
    {
        if (item == null) return;

        if (_selected != null) _selected.SetSelected(false);
        _selected = item;
        _selected.SetSelected(true);

        // 상세 패널 표시
        if (detailPanel) detailPanel.Show(item.Data);
    }
}