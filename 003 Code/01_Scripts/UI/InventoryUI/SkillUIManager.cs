using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/* 인벤토리 내 스킬 UI 기능을 관리하는 스크립트 */

public class SkillUIManager : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] private PlayerSkillSystem playerSkill;

    [Header("버튼")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button upgradeButton;

    [Header("선택 스킬 설명")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillDescription;
    [SerializeField] private TMP_Text skillCooltime;
    [SerializeField] private TMP_Text requiredText;
    [SerializeField] private TMP_Text upgradeCost;

    private SkillButtonUI _selectedSkill;
    private DataManager D => DataManager.Instance;

    private bool _initialized;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        playerSkill = player.GetComponent<PlayerSkillSystem>();

        unlockButton.onClick.AddListener(OnUnlock);
        equipButton.onClick.AddListener(OnEquip);
        unequipButton.onClick.AddListener(OnUnequip);
        upgradeButton.onClick.AddListener(OnUpgrade);

        D.SaveNow();

        // 저장된 상태 복원
        D?.ApplySkillsTo(playerSkill);
        playerSkill.NotifyAllSlotsChanged();

        foreach (var btn in FindObjectsOfType<SkillButtonUI>())
            btn.RefreshLevel();

        RefreshSkillUI();
    }

    #region === UI 갱신 ===

    public void OnSkillSelected(SkillButtonUI skillButton)
    {
        _selectedSkill = skillButton;

        // 안전 가드
        if (_selectedSkill == null || _selectedSkill.SkillData == null)
        {
            Debug.LogWarning("[SkillUIManager] 선택된 스킬 버튼 혹은 SkillData 가 null 입니다. 인스펙터 할당을 확인하세요.");
            _selectedSkill = null;
            RefreshSkillUI();
            return;
        }

        if (skillIcon) skillIcon.sprite = _selectedSkill.SkillData.skillIcon;
        if (skillName) skillName.text = _selectedSkill.SkillData.skillName;
        if (skillDescription) skillDescription.text = _selectedSkill.SkillData.description;

        RefreshSkillUI();
    }

    // 언락, 장착, 해제, 업그레이드 시마다 호출해 UI 버튼 갱신
    private void RefreshSkillUI()
    {
        // 선택 없음 → 깨끗하게 초기화
        if (_selectedSkill == null)
        {
            SetButtonsActive(false, false, false, false);
            if (skillIcon) skillIcon.enabled = false;
            if (skillName) skillName.text = "";
            if (skillDescription) skillDescription.text = "";
            if (skillCooltime) skillCooltime.text = "";
            if (requiredText) requiredText.text = "";
            if (upgradeCost) upgradeCost.text = "";
            return;
        }

        var data = _selectedSkill.SkillData;
        if (data == null)
        {
            Debug.LogWarning("[SkillUIManager] _selectedSkill.SkillData 가 null 입니다.");
            _selectedSkill = null;
            RefreshSkillUI();
            return;
        }

        var D = DataManager.Instance;
        var eco = EconomyService.Instance;

        // DataManager 없으면 더 진행하지 않고 경고 (나중에 다시 누르면 적용되도록)
        if (D == null)
        {
            Debug.LogWarning("[SkillUIManager] DataManager.Instance 가 null 이라 스킬 UI 갱신을 중단합니다.");
            return;
        }

        int currentLevel = D.GetSkillLevel(data);         // 0: 미해금
        bool unlocked = D.IsSkillUnlocked(data);
        bool equipped = (playerSkill != null) && (playerSkill.slot1 == data || playerSkill.slot2 == data);

        // 아이콘/텍스트
        if (skillIcon)
        {
            skillIcon.enabled = data.skillIcon != null;
            skillIcon.sprite = data.skillIcon;
        }
        if (skillName) skillName.text = data.skillName ?? "";
        if (skillDescription) skillDescription.text = data.description ?? "";

        // 쿨타임
        if (skillCooltime)
        {
            float t = GetCooldownForLevel(data, Mathf.Max(1, currentLevel));
            skillCooltime.text = $"쿨타임 {t:0.##}초";
        }

        // 해금 조건 요약
        if (requiredText) requiredText.text = BuildRequirementSummary(D, data, currentLevel > 0);

        // 비용 표기
        if (upgradeCost)
        {
            if (currentLevel >= data.maxLevel) upgradeCost.text = "최대 레벨";
            else if (currentLevel == 0) upgradeCost.text = $"해금 비용 : {GetNextCost(data, 0):N0}";
            else upgradeCost.text = $"업그레이드 비용 : {GetNextCost(data, currentLevel):N0}";
        }

        SetButtonsActive(!unlocked, unlocked && !equipped, equipped, unlocked && currentLevel < data.maxLevel);

        // 상호작용 가능 여부
        int nextCost = (currentLevel < data.maxLevel) ? GetNextCost(data, currentLevel) : 0;
        bool canAfford = (eco != null) && (eco.Money >= nextCost);

        if (!unlocked && unlockButton)
            unlockButton.interactable = D.CanUnlock(data) && canAfford;

        if (upgradeButton)
            upgradeButton.interactable = unlocked && currentLevel < data.maxLevel && canAfford;

        if (upgradeCost)
            upgradeCost.color = (eco != null && eco.Money >= nextCost) ? Color.white : Color.red;

        _selectedSkill.RefreshLevel();
    }

    #endregion

    #region ===비용/쿨타임/요약 계산 유틸 ===
    private int GetNextCost(SkillData data, int currentLevel)
    {
        // 규칙: upgradeCost[currentLevel]가 다음 단계로 가는 비용(0→1 해금 포함)
        if (data.upgradeCost == null || data.upgradeCost.Length == 0) return 0;
        if (currentLevel < 0) currentLevel = 0;
        if (currentLevel >= data.upgradeCost.Length) return data.upgradeCost[data.upgradeCost.Length - 1];
        return data.upgradeCost[currentLevel];
    }

    private float GetCooldownForLevel(SkillData data, int level)
    {
        if (data.levelCooldown != null && data.levelCooldown.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, data.levelCooldown.Length - 1);
            return data.levelCooldown[idx];
        }
        return data.cooldown;
    }

    private string BuildRequirementSummary(DataManager D, SkillData data, bool alreadyUnlocked)
    {
        if (data.unlockConditions == null || data.unlockConditions.Length == 0)
            return alreadyUnlocked ? "해금 조건: (없음) - 해금됨" : "해금 조건: (없음)";

        var sb = new StringBuilder("해금 조건:\n");
        foreach (var cond in data.unlockConditions)
        {
            var req = D.GetSkillById(cond.requiredSkillId);
            string reqName = req != null ? req.skillName : cond.requiredSkillId;
            int haveLv = req != null ? D.GetSkillLevel(req) : 0;

            sb.Append("- ").Append(reqName)
              .Append(" Lv").Append(cond.requiredLevel)
              .Append(" 이상  [현재 Lv").Append(haveLv).Append("]\n");
        }
        if (alreadyUnlocked) sb.Append("→ 현재: 해금됨");
        return sb.ToString().TrimEnd();
    }

    private void SetButtonsActive(bool showUnlock, bool showEquip, bool showUnequip, bool showUpgrade)
    {
        if (unlockButton) unlockButton.gameObject.SetActive(showUnlock);
        if (equipButton) equipButton.gameObject.SetActive(showEquip);
        if (unequipButton) unequipButton.gameObject.SetActive(showUnequip);
        if (upgradeButton) upgradeButton.gameObject.SetActive(showUpgrade);
    }
    #endregion

    #region === 버튼 콜백 ===
    private void OnUnlock()
    {
        if (_selectedSkill == null) return;

        var data = _selectedSkill.SkillData;
        var eco = EconomyService.Instance;

        int cost = GetNextCost(data, 0); // 해금 비용
        if (!D.CanUnlock(data))
        {
            Debug.Log("해금 조건을 만족하지 못했습니다.");
            return;
        }
        if (eco != null && eco.Money < cost)
        {
            Debug.Log("골드 부족");
            return;
        }

        if (eco != null) eco.AddMoney(-cost);
        if (D.UnlockSkill(data))
        {
            // 해금 시 레벨 1로 세팅(필요 시 주석 해제)
            D.SetSkillLevel(data, 1);
            Debug.Log($"{data.skillName} 해금 완료!");
        }
        RefreshSkillUI();
    }

    private void OnEquip()
    {
        if (_selectedSkill == null || !D) return;

        var data = _selectedSkill.SkillData;
        if (playerSkill.slot1 == null)
        {
            playerSkill.SetSlot(1, data);
        }
        else if (playerSkill.slot2 == null)
        {
            playerSkill.SetSlot(2, data);
        }
        else
        {
            Debug.Log("스킬 슬롯 가득 참.");
            return;
        }

        D.SaveSkillsFrom(playerSkill);
        RefreshSkillUI();
    }

    private void OnUnequip()
    {
        if (_selectedSkill == null || playerSkill == null || D == null) return;

        var data = _selectedSkill.SkillData;
        bool unequipped = false;

        if (playerSkill.slot1 == data) { playerSkill.UnequipIfMatched(1, data); unequipped = true; }
        else if (playerSkill.slot2 == data) { playerSkill.UnequipIfMatched(2, data); unequipped = true; }

        if (!unequipped)
        {
            Debug.Log($"{data.skillName}은 현재 장착 중이 아님");
            return;
        }

        D.SaveSkillsFrom(playerSkill);
        RefreshSkillUI();
    }

    private void OnUpgrade()
    {
        if (_selectedSkill == null) return;

        var data = _selectedSkill.SkillData;
        int currentLevel = D.GetSkillLevel(data);
        if (currentLevel >= data.maxLevel) return;

        int cost = GetNextCost(data, currentLevel);
        var eco = EconomyService.Instance;
        if (eco != null && eco.Money >= cost)
        {
            eco.TrySpendMoney(cost);
            D.SetSkillLevel(data, currentLevel + 1);
            Debug.Log($"{data.skillName} Lv{currentLevel + 1} 업그레이드 완료 (잔액: {eco.Money:N0})");
        }
        else
        {
            Debug.Log("골드 부족");
        }
        RefreshSkillUI();
    }
}
#endregion

