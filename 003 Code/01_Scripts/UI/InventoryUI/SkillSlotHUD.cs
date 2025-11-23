using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotHUD : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconTarget;
    [SerializeField] private Image cooldownBox;
    [SerializeField] private TMP_Text costText;

    [Header("설정")]
    [SerializeField] private int slotIndex = 1; // 1 or 2

    private PlayerSkillSystem _pss;
    private PlayerStatus _status;
    private SkillData _cachedSkill;

    public void Bind(PlayerSkillSystem pss, PlayerStatus status, int index)
    {
        _pss = pss;
        _status = status;
        slotIndex = index;

        if (_pss != null)
        {
            _pss.OnSlotChanged += HandleSlotChanged;
        }

        HandleSlotChanged(slotIndex, GetCurrentSkill());
    }

    private void OnDisable()
    {
        if (_pss != null)
        {
            _pss.OnSlotChanged -= HandleSlotChanged;
        }
    }

    private void HandleSlotChanged(int changedSlot, SkillData data)
    {
        if (changedSlot != slotIndex) return;

        _cachedSkill = data;

        // 아이콘
        if (iconTarget)
        {
            iconTarget.sprite = (data != null) ? data.skillIcon : null;
            iconTarget.enabled = (iconTarget.sprite != null);
        }

        // 쿨타임 박스 초기화
        if (cooldownBox != null)
        {
            cooldownBox.type = Image.Type.Filled;
            cooldownBox.fillMethod = Image.FillMethod.Radial360;
            cooldownBox.fillClockwise = true;
            cooldownBox.fillOrigin = (int)Image.Origin360.Top;
            cooldownBox.fillAmount = 0f;
            cooldownBox.enabled = false;
        }

        // 코스트 텍스트
        if (costText != null)
        {
            if (data == null)
            {
                costText.gameObject.SetActive(false);   // 슬롯 비었으면 숨김
            }
            else
            {
                costText.gameObject.SetActive(true);
                costText.text = data.cost.ToString();
                costText.color = Color.white;
            }
        }
    }

    private void Update()
    {
        // 쿨타임 진행만 매 프레임 반영
        var s = _cachedSkill;
        if (_pss == null || s == null || cooldownBox == null) return;

        float remain = _pss.GetRemainCooldown(s.skillId);
        float fill = (s.cooldown <= 0f) ? 0f : Mathf.Clamp01(remain / s.cooldown);
        cooldownBox.fillAmount = fill;
        cooldownBox.enabled = fill > 0f;

        if (costText && _status != null && s.cost > 0)
        {
            bool lack = _status.CurrentMP < s.cost;
           
            // 마나가 부족하다면 마나 소모량 빨간색 처리
            if (lack) costText.color = Color.red;
        }
    }

    private SkillData GetCurrentSkill()
    {
        if (_pss == null) return null;
        return (slotIndex == 1) ? _pss.slot1 : _pss.slot2;
    }
}
