using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeFacilitiesController : MonoBehaviour
{
    [Header("무역길드")]
    [SerializeField] Transform panel_GuildUpgrades;
    
    [Header("대장간")]
    [SerializeField] Transform panel_BlackSmithUpgrades;

    [Header("신전")]
    [SerializeField] Transform panel_TempleUpgrades;

    [Header("상단 보유 금액")]
    [SerializeField] private TMP_Text moneyTextDisplay;

    private Button[] _allUpgradeButtons;

    private const string FacilityCostDiscountId = "GD001"; // 시설 강화 비용 감소 ID
    private const string FacilityMaxLvIncreaseId = "GD002"; // GD002 시설 ID

    private void Awake()
    {
        Button[] buttons_GD = panel_GuildUpgrades.GetComponentsInChildren<Button>(true);
        Button[] buttons_BS = panel_BlackSmithUpgrades.GetComponentsInChildren<Button>(true);
        Button[] buttons_TP = panel_TempleUpgrades.GetComponentsInChildren<Button>(true);
        
        // 모든 버튼을 하나의 배열로 합칩니다.
        _allUpgradeButtons = buttons_GD.Concat(buttons_BS).Concat(buttons_TP).ToArray();

        // 1. 버튼 이벤트 리스너 등록 및 활성화 상태 평가
        foreach (var btn in _allUpgradeButtons)
        {
            btn.onClick.RemoveAllListeners(); // 혹시 모를 중복 방지
            btn.onClick.AddListener(() => TryUpgrade(btn));

            // 초기 상태 평가
            CheckAndDeactivateBtn(btn);
        }
        
        // 2. 돈 변경 이벤트 구독 (골드 변동 시 버튼 활성화 상태 재평가)
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged += HandleMoneyChanged;

            // 초기 금액을 한 번 표시합니다.
            HandleMoneyChanged(EconomyService.Instance.Money);
        }
    }

    private void Start()
    {
        // DataManager 로드가 완료된 후 레벨 초기화 및 버튼 상태 평가를 시작합니다.
        InitializeLevelsAndUI();

        // 버튼 이벤트 리스너 등록 및 활성화 상태 평가
        foreach (var btn in _allUpgradeButtons)
        {
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(() => TryUpgrade(btn));

            // 초기 상태 평가
            CheckAndDeactivateBtn(btn);
        }
    }

    private void OnDestroy()
    {
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnFacilityLevelChanged -= RefreshAllButtonsState;
        }
    }

    private void HandleMoneyChanged(long newMoney)
    {
        // 1. 금액 표시 UI 갱신
        if (moneyTextDisplay != null)
        {
            // "N0" 포맷을 사용하여 콤마가 포함된 금액을 표시합니다.
            moneyTextDisplay.text = newMoney.ToString("N0"); 
        }
        
        // 2. 모든 버튼의 활성화/비활성화 상태를 재평가
        RefreshAllButtonsState();
    }
    
    // 저장된 레벨을 불러와 FacilityData에 적용하고 UI를 초기화합니다.
    private void InitializeLevelsAndUI()
    {
        DataManager dm = DataManager.Instance;
        if (dm == null) return;

        // dm.SetFacilityLevel("GD002",Mathf.Max(1, dm.GetFacilityLevel("GD002")));
        // dm.SetFacilityLevel("BS002",Mathf.Max(1, dm.GetFacilityLevel("BS002")));
        // dm.SetFacilityLevel("BS003",Mathf.Max(1, dm.GetFacilityLevel("BS003")));
        // dm.SetFacilityLevel("TP002",Mathf.Max(1, dm.GetFacilityLevel("TP002")));

        foreach (var btn in _allUpgradeButtons)
        {
            FacilityData facilityData = btn.GetComponentInParent<FacilityData>();
            if (facilityData == null) continue;
            
            // 1. 저장된 레벨 로드
            int savedLevel = dm.GetFacilityLevel(facilityData.FacilityId);
            facilityData.SetLevel(savedLevel); // FacilityData에 레벨 적용
            
            // 2. UI 갱신
            UpdateUpgradeInfo(btn);
        }
    }

    private void RefreshAllButtonsState()
    {
        foreach (var btn in _allUpgradeButtons)
        {
            // 1. 버튼 상태 재평가 (골드 부족 여부, 최대 레벨 여부)
            CheckAndDeactivateBtn(btn); 
            
            // 2. 비용 정보 UI 갱신 (할인된 가격 표시)
            UpdateUpgradeInfo(btn);
        }
    }

    private void TryUpgrade(Button btn)
    {
        FacilityData facilityData = btn.GetComponentInParent<FacilityData>();
        if (facilityData == null) return;
        
        // 1. 업그레이드 가능 여부 및 비용 계산
        if (facilityData.Lv >= facilityData.MaxLv)
        {
            Debug.LogWarning($"[시설 강화] {facilityData.FacilityId}는 이미 최대 레벨입니다.");
            return;
        }

        int cost = CalculateCost(btn);
        EconomyService eco = EconomyService.Instance;

        // 2. 코스트 결제
        if (eco == null || !eco.TrySpendMoney(cost))
        {
            Debug.LogWarning($"[시설 강화] 골드 부족. 필요 비용: {cost}");
            // 골드 부족 시 즉시 버튼 비활성화 (CheckAndDeactivateBtn이 처리)
            CheckAndDeactivateBtn(btn);
            return;
        }

        // 3. 레벨 증가 및 데이터 저장
        facilityData.IncrementLevel();
        DataManager.Instance?.SetFacilityLevel(facilityData.FacilityId, facilityData.Lv); // DataManager에 변경된 레벨 저장

        Debug.Log($"[시설 강화] {facilityData.FacilityId} 강화 성공. 레벨: {facilityData.Lv}");

        // 4. UI 갱신 및 모든 버튼 상태 재평가 (최대 레벨 도달 시 비활성화 등)
        UpdateUpgradeInfo(btn);
        RefreshAllButtonsState(); // 모든 버튼 상태를 갱신

        // 튜토리얼 QT-005에서만 플래그 발동
        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-012", "UPGRADE_FORGE");
        }
    }

    private void CheckAndDeactivateBtn(Button btn)
    {
        FacilityData facilityData = btn.GetComponentInParent<FacilityData>();
        if (facilityData == null) return;
        
        // 1. 최대 레벨 도달 시
        int gd002Limit = (facilityData.FacilityId == FacilityMaxLvIncreaseId) ? facilityData.MaxLv : GetFacilityMaxLevelLimit(facilityData.FacilityId);

        int currentMaxLv = Mathf.Min(facilityData.MaxLv, gd002Limit);


        if (facilityData.Lv >= currentMaxLv)
        {
        btn.interactable = false;
        
        FacilitiesPanel panel = btn.GetComponentInParent<FacilitiesPanel>();
        if (panel != null) 
        {
            // 최종 한계치 도달 시 UI 표시 (GD002 제한인지, 원래 MAX인지)
            panel.cost.text = (facilityData.Lv >= facilityData.MaxLv) ? "MAX" : "GD002 제한"; 
        }
        return;
        }

        int cost = CalculateCost(btn);
        long money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

        // 2. 골드 부족 시
        if (money < cost)
        {
            btn.interactable = false;
            DeactivateBtn(btn); // 시각적 피드백
        }
        else
        {
            btn.interactable = true;
            ActivateBtn(btn); // 시각적 피드백
        }
    }

    private void DeactivateBtn(Button btn)
    {
        // TODO: 버튼 시각적 비활성화 (빨간색 등으로) 구현
        // 현재는 interactable = false 만으로 충분합니다.
        // 예: btn.image.color = Color.red; 
    }
    
    private void ActivateBtn(Button btn)
    {
        // TODO: 버튼 시각적 활성화 (원래 색으로) 구현
        // 예: btn.image.color = Color.white;
    }

    private int CalculateCost(Button btn)
    {
        // 1. 기본 비용 계산
        int baseCost = btn.GetComponentInParent<FacilityData>().Lv * 10 + 10;
        
        // 2. GD001 시설의 현재 레벨을 DataManager에서 조회
        int discountLevel = DataManager.Instance != null 
            ? DataManager.Instance.GetFacilityLevel(FacilityCostDiscountId) 
            : 0;

        // 3. 할인율 계산 (Lv * 5%)
        float discountRate = Mathf.Clamp(discountLevel * 5 / 100f, 0f, 0.45f);
        
        // 4. 할인율 적용
        float finalCostFloat = baseCost * (1.0f - discountRate);
        
        // 5. 정수로 내림 처리
        return Mathf.FloorToInt(finalCostFloat);
    }
    
    // (기존 코드 유지)
    private void UpdateUpgradeInfo(Button btn)
    {
        FacilityData facilityData = btn.GetComponentInParent<FacilityData>();
        FacilitiesPanel facilitiesPanel = btn.GetComponentInParent<FacilitiesPanel>();

        if (facilityData == null || facilitiesPanel == null) return;

        // 1. 최종 한계치 결정
        int gd002Limit = (facilityData.FacilityId == FacilityMaxLvIncreaseId) 
            ? facilityData.MaxLv 
            : GetFacilityMaxLevelLimit(facilityData.FacilityId);
            
        int currentMaxLv = Mathf.Min(facilityData.MaxLv, gd002Limit);

        // 2. 다음 레벨과 비용 텍스트 결정
        string nextCostText = facilityData.Lv >= currentMaxLv ? 
            (facilityData.Lv >= facilityData.MaxLv ? "MAX" : "GD002 제한") : 
            CalculateCost(btn).ToString("N0");
        
        // 패널의 데이터를 업데이트
        switch (facilityData.FacilityId){
            case "GD001": // 시설 강화 비용 감소
            case "TP001": // 버프 구매 비용 감소
            case "BS001": // 장비 강화 비용 감소
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null )
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}

            facilitiesPanel.before.text = ((facilityData.Lv) *5).ToString() + "%" ;
            facilitiesPanel.after.text = ((facilityData.Lv + 1) *5).ToString() + "%" ;
            facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
            facilitiesPanel.cost.text = nextCostText;
            break;

            case "BS002": // 장비 강화 한계치 증가
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null)
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}

            // 현재 BS002 레벨이 제공하는 강화 한계치
            facilitiesPanel.before.text = (facilityData.Lv).ToString(); 
            facilitiesPanel.after.text = ((facilityData.Lv + 1)).ToString() ;
            facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
            facilitiesPanel.cost.text = nextCostText;

            
            break;

            // 한계치 증가, 광산카트
            case "GD002": // 시설 강화 한계치 증가
            case "TP003": // 버프 장착 슬롯 (레벨과 슬롯 개수가 일치)
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null)
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}

            facilitiesPanel.before.text = facilityData.Lv.ToString();
            facilitiesPanel.after.text = (facilityData.Lv + 1).ToString();
            facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
            facilitiesPanel.cost.text = nextCostText;
            break;

            case "GD003": // 광산 카트 층 한계치
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null)
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}

            // 현재 레벨이 제공하는 층 한계치 (0->0, 1->1, 2->3, 3->5)
            int currentFloorLimit = facilityData.Lv switch { 1 => 1, 2 => 3, 3 => 5, _ => 0 };
            facilitiesPanel.before.text = currentFloorLimit.ToString();
            
            // 다음 레벨이 제공할 층 한계치
            int nextFloorLimit = (facilityData.Lv + 1) switch { 1 => 1, 2 => 3, 3 => 5, _ => 5 };
            facilitiesPanel.after.text = nextFloorLimit.ToString();

            facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
            facilitiesPanel.cost.text = nextCostText;
            break;

            // 해금 시설
            case "BS003": // 장비 희귀도 해금
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null)
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}

            // 희귀도 등급에 대한 문자열 매핑 (1=Common, 7=Epic)
            string GetRarityName(int level) => level switch
            {
                1 => "Common", 2 => "Uncommon", 3 => "Rare", 4 => "Rare",
                5 => "Unique", 6 => "Epic", 7 => "Mystic",
                _ => "Max"
            };

            facilitiesPanel.after.text = GetRarityName(facilityData.Lv + 1);
            facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
            facilitiesPanel.cost.text = nextCostText;

            break;

            case "TP002": // 버프 구매 해금 (0~9레벨)
            if (facilityData.Lv == facilityData.MaxLv && facilitiesPanel.MAX !=null && facilitiesPanel.changes != null)
            {facilitiesPanel.MAX.gameObject.SetActive(true); 
            facilitiesPanel.changes.gameObject.SetActive(false);}
            else{
                var bm = DungeonRunBuffManager.Instance ;

                facilitiesPanel.after.text = bm.Catalog[facilityData.Lv].buffName;
                facilitiesPanel.unlock.sprite = bm.Catalog[facilityData.Lv].buffIcon;
                facilitiesPanel.LV.text = "Lv." + facilityData.Lv.ToString() ;
                facilitiesPanel.cost.text = nextCostText;
            }
            break;

            default:
            facilitiesPanel.before.text = "Error";
            facilitiesPanel.after.text = "Error";
            Debug.LogError($"Unknown FacilityId encountered: {facilityData.FacilityId}");
            break;
        }
    }

    private int GetFacilityMaxLevelLimit(string targetFacilityId)
{
    // GD002는 레벨 1부터 시작
    int gd002Level = DataManager.Instance != null 
        ? DataManager.Instance.GetFacilityLevel(FacilityMaxLvIncreaseId) 
        : 0; 
    
    // GD002가 0레벨이면, 모든 강화는 막혀야 합니다.
    if (gd002Level < 1) return 0;

    // GD002 레벨에 따른 시설별 최대 레벨 제한 (표 기준)
    return targetFacilityId switch
    {
        // GD001, TP001, TP002, BS001 (0~9레벨 시설)
        "GD001" or "TP001" or "BS001" => gd002Level switch
        {
            1 => 3,
            2 => 6,
            3 => 9,
            _ => 9
        },

        // GD001, TP001, TP002, BS001 (0~9레벨 시설)
        "TP002" => gd002Level switch
        {
            1 => 3,
            2 => 6,
            3 => 10,
            _ => 10
        },
        
        // BS002 (0~12레벨 시설)
        "BS002" => gd002Level switch
        {
            1 => 4,
            2 => 8,
            3 => 12,
            _ => 12
        },
        
        // BS003 (Common(1) ~ Epic(7) 해금, 레벨 제한으로 변환 필요)
        "BS003" => gd002Level switch
        {
            1 => 4, // 4 레벨 (Common(1) ~ Rare(4) 해금 가정)
            2 => 6, // 6 레벨 (Rare(4) ~ Unique(6) 해금 가정)
            3 => 7, // 7 레벨 (Unique(6) ~ Epic(7) 해금 가정)
            _ => 7
        },
        
        // TP003 (1~3 슬롯 해금, 1레벨부터 시작)
        "TP003" => gd002Level switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 3
        },
        
        // GD003 (광산 카트, 0~3레벨 시설)
        "GD003" => gd002Level switch
        {
            1 => 1,
            2 => 3,
            3 => 5, // 표에 1, 3, 5 레벨이라고 되어 있으나, GD002가 3레벨까지이므로 MaxLv 3을 기준으로 5레벨 이상을 지원하지 않음
            _ => 3
        },
        
        // GD002 (자기 자신은 항상 MaxLv까지 가능하도록 허용)
        _ => int.MaxValue
    };
}

    public void RESET_LEVEL(){
        DataManager.Instance.ResetAllFacilityLevels();
        if (EconomyService.Instance != null)
        {
            // Money 필드는 int 타입이므로 10000을 전달합니다.
            EconomyService.Instance.AddMoney(10000);

        }
        else
        {
            // EconomyService가 아직 초기화되지 않았거나 Scene에 없는 경우
            Debug.LogError("EconomyService 인스턴스가 존재하지 않습니다.");
        }
    }
}