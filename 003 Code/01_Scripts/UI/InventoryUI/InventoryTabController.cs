using UnityEngine;
using UnityEngine.UI;

public class InventoryTabController : MonoBehaviour
{
    [Header("참조 대상")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button questButton;

    private SkillUIManager _skillUI; // 최초 1회 캐시

    private void Awake()
    {
        // 누락 방지: 버튼 클릭 연결
        if (inventoryButton) inventoryButton.onClick.AddListener(OpenInventoryPanel);
        if (skillButton) skillButton.onClick.AddListener(OpenSkillPanel);

        // 최초 상태는 인벤토리 탭
        SetTab(inventoryOn: true);
    }

    public void OpenInventoryPanel() => SetTab(inventoryOn: true);
    public void OpenSkillPanel()
    {
        SetTab(inventoryOn: false);

        // 스킬 패널을 처음 여는 순간 초기화 보장
        if (_skillUI == null && skillPanel)
            _skillUI = skillPanel.GetComponentInChildren<SkillUIManager>(true);
    }

    private void SetTab(bool inventoryOn)
    {
        if (inventoryPanel) inventoryPanel.SetActive(inventoryOn);
        if (skillPanel) skillPanel.SetActive(!inventoryOn);
    }
}
