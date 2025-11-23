using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeListItem : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameAndGradeText;  // 예: "녹슨 검 +0"
    [SerializeField] private TMP_Text statAtkText;
    [SerializeField] private TMP_Text statAtkSpeedText;
    [SerializeField] private TMP_Text durabilityText;
    [SerializeField] private Button selectButton;

    private EquipmentData _data;
    private BlacksmithUpgradeController _controller;

    public void Init(EquipmentData data, BlacksmithUpgradeController controller)
    {
        _data = data;
        _controller = controller;

        if (selectButton) selectButton.onClick.AddListener(OnClickSelect);

        if (EnhancementService.Instance != null)
            EnhancementService.Instance.OnEnhanced += HandleEnhanced;

        Refresh();
    }

    private void OnDestroy()
    {
        if (EnhancementService.Instance != null)
            EnhancementService.Instance.OnEnhanced -= HandleEnhanced;
    }

    private void HandleEnhanced(EquipmentData eq, int lv)
    {
        if (eq == _data) Refresh();
    }

    public void Refresh()
    {
        if (_data == null) return;

        var svc = EnhancementService.Instance;
        int lv = svc != null ? svc.GetLevel(_data) : _data.EquipmentUpgrade;
        _data.ApplyLoadedUpgrade(lv);

        if (iconImage) iconImage.sprite = _data.Icon;
        if (nameAndGradeText) nameAndGradeText.text = $"{_data.EquipmentName} +{lv}";

        // 간단 표기(원하면 더 꾸며도 됨)
        int atk = _data.CalculatedAttackDamage;
        if (statAtkText) statAtkText.text = $"기본 공격력: {atk}";
        if (statAtkSpeedText) statAtkSpeedText.text = $"공격 속도: {_data.Speed:0.##}";

        // DataManager에서 내구도 조회
        int currentDurability = _data.MaxDurability;
        int maxDurability = _data.MaxDurability;

        var dm = DataManager.Instance;
        if (dm != null)
        {
            currentDurability = dm.GetEquipmentDurability(_data);
        }

        if (durabilityText) durabilityText.text = $"내구도: {currentDurability} / {maxDurability}";
    }

    private void OnClickSelect()
    {
        _controller?.Select(_data);
    }
}