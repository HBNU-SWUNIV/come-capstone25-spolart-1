using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEditor;

public class InventoryUIManager : MonoBehaviour, IRebindOnSceneChange
{
    [Header("UI 오브젝트 참조")]
    [SerializeField] private GameObject inventoryPanel;   // 전체 패널 (토글용)
    [SerializeField] private Transform slotContainer;     // 슬롯 컨테이너
    [SerializeField] private GameObject slotPrefab;       // 슬롯 프리팹
    [SerializeField] private Image weightFill;            // 무게 Fill 이미지 
    [SerializeField] private TMP_Text weightText;         // "현재무게/최대무게" 표시
    [SerializeField] private Button closeButton;

    [Header("슬롯 캐시")]
    private List<InventorySlotUI> slots = new();

    private Inventory boundInventory;

    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    private void OnEnable()
    {
        // 씬이 바뀔 때마다 바인딩 재시도
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 씬 처음 들어왔을 때도 시도
        TryBindInventoryAndInitUI();

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnClickExit);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeInventory();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때 Inventory가 새로 생겼을 수 있으니 재바인딩
        TryBindInventoryAndInitUI();
    }

    public void RebindSceneRefs()
    {
        TryBindInventoryAndInitUI(); // 파일에 이미 존재하는 안전한 재바인드 함수 활용
    }

    private void TryBindInventoryAndInitUI()
    {
        // 이미 구독된 게 있다면 정리
        UnsubscribeInventory();

        // 현재 씬에서 플레이어의 Inventory 찾기
        boundInventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Exclude);

        if (boundInventory == null)
        {
            return;
        }

        // 이벤트 구독
        boundInventory.InventoryChanged += OnInventoryChanged;

        // 슬롯이 아직 없으면 생성 (SO의 슬롯 수 기준)
        if (slots.Count == 0)
        {
            EnsureSlots(InventoryStateHandle.Runtime.maxSlot);
        }

        // 슬롯에 인벤토리 인덱스 바인딩
        BindAllSlots();

        // 초기 UI 강제 갱신
        ForceRefreshAll();
    }

    private void UnsubscribeInventory()
    {
        if (boundInventory != null)
        {
            boundInventory.InventoryChanged -= OnInventoryChanged;
            boundInventory = null;
        }
    }

    // SO의 maxSlot과 현재 슬롯 개수 일치시킴
    private void EnsureSlots(int desiredCount)
    {
        // 개수 다르면 재생성
        if (slots.Count == desiredCount && slotContainer.childCount == desiredCount) return;

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        slots.Clear();

        for (int i = 0; i < desiredCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            var ui = go.GetComponent<InventorySlotUI>();
            slots.Add(ui);
        }
    }

    // 현재 생성된 슬롯에 인덱스 바인딩
    private void BindAllSlots()
    {
        if (boundInventory == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var ui = slots[i];
            if (ui == null) continue;
            ui.Bind(boundInventory, i); // 데이터 바인딩
        }
    }

    private void OnInventoryChanged()
    {
        UpdateWeightUI();
    }

    private void UpdateWeightUI()
    {
        var S = InventoryStateHandle.Runtime;

        if (weightText != null)
        {
            weightText.text = $"{S.currentWeight:0.#} / {S.maxWeight:0.#}";
        }
        else 
        {
            Debug.LogWarning("무게 표시 텍스트 연결되지 않음");
        }

        if (weightFill != null)
        {
            weightFill.fillAmount = (S.maxWeight > 0f) ? Mathf.Clamp01(S.currentWeight / S.maxWeight) : 0f;
        }
    }

    private void ForceRefreshAll()
    {
        UpdateWeightUI();

        ForceRefreshAllSlots();
    }

    // 모든 슬롯에 대해 RefreshFromData 강제 호출
    private void ForceRefreshAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var ui = slots[i];
            if (ui != null)
            {
                ui.RefreshFromData();
            }
        }
    }

    public void OnClickExit()
    {
        TogglePanel();
    }

    // 필요 시 UI 열고 닫기
    public void TogglePanel()
    {
        if (inventoryPanel == null) return;

        var town = TownUIManager.Instance;
        if (town != null && town.IsBusy)
        {
            Debug.Log("대화/상호작용 중에는 인벤토리 오픈 불가능");
            return;
        }

        bool next = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(next);
    }
}
