using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("참조")]
    [SerializeField] private InventorySlotUI slotUI;          // 같은 오브젝트에 붙어있는 슬롯 UI
    [SerializeField] private RectTransform inventoryPanelRect; // 인벤토리 패널 Rect (바깥 드롭 판정용)
    [SerializeField] private Canvas rootCanvas;               // 최상위 캔버스 (드래그 아이콘 붙일 곳)
    [SerializeField] private Camera uiCamera;                 // UI 카메라 (없으면 null 허용)
    [SerializeField] private QuantityPromptPanel quantityPrompt; // 수량 입력 패널

    [Header("자동 찾기 설정(이름/경로)")]
    [SerializeField] private string canvasName = "Inventory_Canvas";
    [SerializeField] private string panelPathUnderCanvas = "Inventory_Panel/Inventory_BG/Backpack_BG"; // 캔버스 하위 경로

    // 드래그 고스트 아이콘
    private GameObject dragginIconGO;
    private RectTransform draggingIconRT;

    // 드래그 시작 시 슬롯 인덱싱 및 인벤토리 캐시
    private int dragSourceIndex = -1;
    private Inventory dragInventory;

    private void Awake()
    {
        EnsureRefs(); // 시작 시 한 번 참조 보정
    }

    private void EnsureRefs()
    {
        // inventoryPanelRect 자동 탐색
        TryFindBackpackBG();

        // rootCanvas 자동 할당
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        // uiCamera 자동 할당 (ScreenSpaceCamera/WorldSpace일 때)
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            uiCamera = rootCanvas.worldCamera;

        if (quantityPrompt == null)
        {
            quantityPrompt = GetComponentInParent<QuantityPromptPanel>();
            Debug.Log("QuantityPromptPanel 연결 성공");
        }
    }

    private RectTransform TryFindBackpackBG()
    {
        if (inventoryPanelRect != null) return inventoryPanelRect;

        // 1) 부모 트리에서 이름이 "Inventory_BG"인 RectTransform 찾기
        var parents = GetComponentsInParent<RectTransform>(true);
        foreach (var rt in parents)
        {
            if (rt != null && rt.name == "Backpack_BG")
            {
                inventoryPanelRect = rt;
                return inventoryPanelRect;
            }
        }

        // 2) 캔버스 이름 + 하위 경로로 찾기
        var canvasGO = GameObject.Find(canvasName);
        if (canvasGO != null)
        {
            var t = canvasGO.transform.Find(panelPathUnderCanvas);
            if (t != null)
            {
                inventoryPanelRect = t.GetComponent<RectTransform>();
                if (inventoryPanelRect != null) return inventoryPanelRect;
            }
        }

        // 3) 씬 전역에서 이름으로 직접 찾기(최후의 수단)
        var bg = GameObject.Find("Backpack_BG");
        if (bg != null)
        {
            inventoryPanelRect = bg.GetComponent<RectTransform>();
            if (inventoryPanelRect != null) return inventoryPanelRect;
        }

        // 못 찾았으면 경고
        Debug.LogWarning("[InventorySlotDrag] Backpack_BG RectTransform을 찾지 못했습니다. 인스펙터에 직접 할당하세요.");
        return null;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {    
        // 문제 추적용 로그
        if (slotUI == null) { Debug.LogWarning("[ISD] slotUI null"); return; }
        if (slotUI.IsEmpty) { Debug.Log("[ISD] slot empty"); return; }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;
        }

        dragSourceIndex = slotUI.SlotIndex;
        dragInventory = slotUI.Inventory;

        dragginIconGO = new GameObject("DragginIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragginIconGO.transform.SetParent(rootCanvas.transform, false);

        draggingIconRT = dragginIconGO.GetComponent<RectTransform>();
        var img = dragginIconGO.GetComponent<Image>();
        var cg = dragginIconGO.GetComponent<CanvasGroup>();

        // 아이템 스프라이트 이미지 설정
        img.sprite = slotUI.Ore != null ? slotUI.Ore.OreIcon : null;

        // 고스트가 기존 UI를 가리지 않도록 레이캐스트 차단
        img.raycastTarget = false;
        cg.blocksRaycasts = false;

        var srcIcon = GetComponentInChildren<Image>();
        if (srcIcon != null)
        {
            draggingIconRT.sizeDelta = (srcIcon.rectTransform != null) ? srcIcon.rectTransform.rect.size : new Vector2(64, 64);
        }
        else
        {
            draggingIconRT.sizeDelta = new Vector2(64, 64);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(draggingIconRT == null) return;
        SetGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 고스트 제거
        if (dragginIconGO != null)
        {
            Destroy(dragginIconGO);
            dragginIconGO = null;
            draggingIconRT = null;
        }

        Debug.Log("OnEndDrag 동작");

        // 버리기 기능
        if (dragInventory == null || dragSourceIndex < 0) { ResetDragCache(); return; }
        if (slotUI == null) 
        { 
            ResetDragCache(); return; 
        }

        bool pointerInsideInventory =
        inventoryPanelRect != null && RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRect, eventData.position, uiCamera);

        if (!pointerInsideInventory)
        {
            var (ore, count) = dragInventory.PeekSlot(dragSourceIndex);
            if (ore != null && count > 0 && quantityPrompt != null)
            {
                // 로컬 변수로 
                var inv = dragInventory;
                var srcIndex = dragSourceIndex;

                quantityPrompt.Show(
                    defaultValue: count,
                    max: count,
                    onConfirm: amt =>
                    {
                        if (inv != null)  // 방어 코드
                        {
                            inv.RemoveFromSlot(srcIndex, amt);
                        }
                    },
                    onCancel: () => { /* 취소 시 아무 것도 안 함 */ }
                );
            }
        }

        ResetDragCache();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var sourceDrag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<InventorySlotDrag>() : null;
        if (sourceDrag == null || sourceDrag == this) return;
        if (slotUI == null || sourceDrag.slotUI == null) return;

        var inv = slotUI.Inventory;
        if (inv == null) return;

        int dst = slotUI.SlotIndex;
        int src = sourceDrag.slotUI.SlotIndex;

        // 소스가 빈 슬롯이면 무시
        if (sourceDrag.slotUI.IsEmpty) return;

        // 타겟이 비었으면 이동, 아니면 MoveAll(내부에서 동종이면 합치고 다르면 스왑)
        if (slotUI.IsEmpty)
            inv.MoveAll(src, dst);
        else
            inv.MoveAll(src, dst);
        // InventoryChanged 이벤트를 통해 두 슬롯 UI가 자동 새로고침됩니다.
    }

    private void SetGhostPosition(PointerEventData eventData)
    {
        if (draggingIconRT == null) return;

        // Canvas Render Mode에 따른 마우스 좌표 변환
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            draggingIconRT.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                uiCamera,
                out var localPoint);
            draggingIconRT.localPosition = localPoint;
        }
    }

    private void ResetDragCache()
    {
        dragSourceIndex = -1;
        dragInventory = null;
    }
}
