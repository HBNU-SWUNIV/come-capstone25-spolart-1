using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버프 선택 시 상세설명을 보여줄 스크립트
/// </summary>

public class BuffDetailPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TMP_Text currentMoney;

    [Header("현재 구매한 버프 표시")]
    [SerializeField] private Image buffIconSlot1;
    [SerializeField] private Image buffIconSlot2;
    [SerializeField] private Button buffSlot1Button;
    [SerializeField] private Button buffSlot2Button;

    private BuffData _current;
    private DungeonRunBuffManager _runBuffs;
    private EconomyService _eco;

    // TP001 시설 ID
    private const string BuffCostDiscountId = "TP001";

    // TP001 할인율을 DataManager에서 직접 조회
    private int CalculateFinalPrice(BuffData data)
    {
        if (data == null) return 0;

        // 1. TP001 시설의 현재 레벨을 DataManager에서 조회
        int discountLevel = DataManager.Instance != null
            ? DataManager.Instance.GetFacilityLevel(BuffCostDiscountId)
            : 0;

        // 2. 할인율 계산 (Lv * 5%)
        float discountRate = Mathf.Clamp(discountLevel * 5 / 100f, 0f, 0.45f);

        // 3. 최종 가격 = 원가 * (1 - 할인율)
        float discountedPrice = data.price * (1.0f - discountRate);

        return Mathf.FloorToInt(discountedPrice);
    }

    private void Awake()
    {
        _runBuffs = DungeonRunBuffManager.Instance;
        _eco = EconomyService.Instance;

        if (buyButton) buyButton.onClick.AddListener(OnBuy);
        if (removeButton) removeButton.onClick.AddListener(OnRemoveCurrent);

        if (buffSlot1Button) buffSlot1Button.onClick.AddListener(() => OnClickCurrentBuffSlot(0));
        if (buffSlot2Button) buffSlot2Button.onClick.AddListener(() => OnClickCurrentBuffSlot(1));

        if (_eco != null)
        {
            HandleMoneyChanged(_eco.Money);
            _eco.OnMoneyChanged += HandleMoneyChanged;
        }
        if (_runBuffs != null) _runBuffs.OnBasketChanged += RefreshCurrentBasket;

        // DataManager 이벤트 구독 (TP001 레벨 변경 시 가격 갱신)
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnFacilityLevelChanged += RefreshCurrentPrice;
        }

        if (buffIconSlot1) buffIconSlot1.gameObject.SetActive(false);
        if (buffIconSlot2) buffIconSlot2.gameObject.SetActive(false);

        RefreshCurrentBasket();
        RefreshBuyRemoveButtonsForCurrent();
    }

    private void OnDestroy()
    {
        if (_eco != null) _eco.OnMoneyChanged -= HandleMoneyChanged;
        if (_runBuffs != null) _runBuffs.OnBasketChanged -= RefreshCurrentBasket;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnFacilityLevelChanged -= RefreshCurrentPrice;
        }
    }

    private void OnEnable()
    {
        // 패널이 활성화될 때 (창을 열 때) 가격을 갱신
        RefreshCurrentPrice();
    }

    /// <summary>
    /// TP001 레벨 변경 시 현재 표시 중인 버프의 가격을 재계산합니다.
    /// </summary>
    private void RefreshCurrentPrice()
    {
        if (_current != null)
        {
            Show(_current);
            RefreshBuyButtonInteractable(); // 버튼 활성화 상태도 갱신
        }
    }

    private void HandleMoneyChanged(long money)
    {
        if (currentMoney) currentMoney.text = money.ToString("N0");
        RefreshBuyButtonInteractable(); // 돈 변동 시 버튼 상태도 재평가
    }

    public void Show(BuffData data)
    {
        _current = data;
        if (!_current)
        {
            Clear();
            return;
        }

        if (iconImage) iconImage.sprite = _current.buffIcon;
        if (titleText) titleText.text = _current.buffName;
        // 할인된 가격으로 표시
        if (costText) costText.text = CalculateFinalPrice(_current) > 0 ? $"구매 금액 : {CalculateFinalPrice(_current):N0}" : "무료";
        if (descriptionText) descriptionText.text = _current.desription;
        if (effectText) effectText.text = _current.effectDesc;

        RefreshBuyRemoveButtonsForCurrent(); // Show할 때 버튼 상태 갱신
    }

    public void Clear()
    {
        _current = null;
        if (iconImage) iconImage.sprite = null;
        if (titleText) titleText.text = "";
        if (costText) costText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (effectText) effectText.text = "";
        if (buyButton) buyButton.interactable = false;
    }

    private void OnBuy()
    {
        if (_current == null || _runBuffs == null || _eco == null) return;

        // 1) 슬롯/중복 사전검증
        if (!_runBuffs.CanBuy(_current.buffId, out var reason))
        {
            Debug.LogWarning($"구매 불가: {reason}");
            RefreshBuyButtonInteractable();
            return;
        }

        // 할인된 가격으로 결제
        int finalPrice = CalculateFinalPrice(_current);
        if (!_eco.TrySpendMoney(finalPrice))
        {
            Debug.LogWarning("소지 금액이 부족합니다.");
            RefreshBuyButtonInteractable();
            return;
        }

        // 3) 장바구니 반영 (이상 시 롤백)
        if (!_runBuffs.Buy(_current.buffId))
        {
            // 방어적 롤백: 할인된 금액 환불
            _eco.AddMoney(finalPrice);
            Debug.LogWarning("구매 실패(중복 또는 제한). 금액은 환불되었습니다.");
            RefreshBuyButtonInteractable();
            return;
        }

        // 튜토리얼 QT-005에서만 플래그 발동
        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-010", "BUY_BUFF");
        }

        // 성공
        RefreshBuyRemoveButtonsForCurrent();
        buyButton.interactable = false;
        Debug.Log($"구매 완료: {_current.buffName}");
    }

    private void OnRemoveCurrent()
    {
        if (_current == null || _runBuffs == null) return;

        // 할인된 가격 환불
        int finalPrice = CalculateFinalPrice(_current);

        if (_runBuffs.Remove(_current.buffId))
        {
            // 환불 로직 (DungeonRunBuffManager에서 환불 로직을 제거했으므로 여기서 담당)
            _eco?.AddMoney(finalPrice);
            Debug.Log($"[BuffDetailPanel] 버프 '{_current.buffName}' 해제됨 → {finalPrice:N0} Gold 환불");

            RefreshCurrentBasket();
            RefreshBuyRemoveButtonsForCurrent();
        }
    }

    private void OnClickCurrentBuffSlot(int index)
    {
        if (_runBuffs == null) return;
        var ids = _runBuffs.BasketIds;
        if (ids == null || index < 0 || index >= ids.Count) return;

        string id = ids[index];
        var data = FindInCatalog(id);
        if (data == null) return;

        Show(data);
        RefreshBuyRemoveButtonsForCurrent(); // 해제 버튼/구매 버튼 상태 갱신
    }

    // 카탈로그에서 ID로 BuffData 찾기(헬퍼)
    private BuffData FindInCatalog(string id)
    {
        if (string.IsNullOrEmpty(id) || _runBuffs == null) return null;
        foreach (var b in _runBuffs.Catalog)
            if (b != null && b.buffId == id) return b;
        return null;
    }

    private void RefreshBuyButtonInteractable()
    {
        if (!buyButton) return;

        bool can = false;
        if (_current != null && _runBuffs != null && _eco != null)
        {
            if (_runBuffs.CanBuy(_current.buffId, out _))
            {
                // 슬롯/중복 조건을 통과해야만 돈 조건 체크
                can = (_eco.Money >= CalculateFinalPrice(_current));
            }
        }
        buyButton.interactable = can;
    }

    private void RefreshBuyRemoveButtonsForCurrent()
    {
        RefreshBuyButtonInteractable(); // 기존 구매 조건 평가

        if (removeButton != null)
        {
            bool canRemove = (_current != null && _runBuffs != null && _runBuffs.IsInBasket(_current.buffId));
            removeButton.gameObject.SetActive(canRemove);
            removeButton.interactable = canRemove;
        }
    }

    // 현재 적용 중인 버프를 표시
    private void RefreshCurrentBasket()
    {
        if (_runBuffs == null || buffIconSlot1 == null || buffIconSlot2 == null) return;

        var ids = _runBuffs.BasketIds;

        // 헬퍼: 카탈로그에서 ID로 BuffData 찾기
        BuffData FindInCatalog(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var b in _runBuffs.Catalog)
                if (b != null && b.buffId == id) return b;
            return null;
        }

        // 슬롯1
        if (ids != null && ids.Count >= 1)
        {
            var b1 = FindInCatalog(ids[0]);
            if (b1 != null && b1.buffIcon != null)
            {
                buffIconSlot1.sprite = b1.buffIcon;
                buffIconSlot1.gameObject.SetActive(true);
            }
            else
            {
                buffIconSlot1.sprite = null;
                buffIconSlot1.gameObject.SetActive(false);
            }
        }
        else
        {
            buffIconSlot1.sprite = null;
            buffIconSlot1.gameObject.SetActive(false);
        }

        // 슬롯2
        if (ids != null && ids.Count >= 2)
        {
            var b2 = FindInCatalog(ids[1]);
            if (b2 != null && b2.buffIcon != null)
            {
                buffIconSlot2.sprite = b2.buffIcon;
                buffIconSlot2.gameObject.SetActive(true);
            }
            else
            {
                buffIconSlot2.sprite = null;
                buffIconSlot2.gameObject.SetActive(false);
            }
        }
        else
        {
            buffIconSlot2.sprite = null;
            buffIconSlot2.gameObject.SetActive(false);
        }
    }
}