using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private SkillData skillData;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text level;
    [SerializeField] Image equippedMark;
    [SerializeField] Button button;

    public SkillData SkillData => skillData;
    public System.Action<SkillButtonUI> OnSelected;

    private PlayerSkillSystem _player;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);

        TryBindPlayer();
        RefreshAll();
    }

    private void OnEnable()
    {
        TryBindPlayer();
        SubscribePlayerEvents(true);
        RefreshAll();
    }

    private void OnDisable()
    {
        SubscribePlayerEvents(false);
    }

    private void TryBindPlayer()
    {
        // 우선 Local 참조 사용(플레이 시점에 세팅됨)
        if (_player == null) _player = PlayerSkillSystem.Local;

        // Local이 아직 없을 수도 있으니, 필요 시 태그로 보강 검색
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.GetComponent<PlayerSkillSystem>();
        }
    }

    private void SubscribePlayerEvents(bool subscribe)
    {
        if (_player == null) return;

        if (subscribe)
            _player.OnSlotChanged += HandleSlotChanged;
        else
            _player.OnSlotChanged -= HandleSlotChanged;
    }

    private void HandleSlotChanged(int slotIndex, SkillData data)
    {
        RefreshEquippedMark();
    }

    private void HandleClick()
    {
        OnSelected?.Invoke(this);
    }

    /// <summary>
    /// 아이콘/레벨/착용 마크까지 한 번에 갱신
    /// </summary>
    public void RefreshAll()
    {
        RefreshIcon();
        RefreshLevel();
        RefreshEquippedMark();
    }

    private void RefreshIcon()
    {
        if (icon == null) return;

        if (skillData != null && skillData.skillIcon != null)
        {
            icon.enabled = true;
            icon.sprite = skillData.skillIcon;
        }
        else
        {
            icon.enabled = false;
            icon.sprite = null;
        }
    }

    public void RefreshLevel()
    {
        if (level == null || skillData == null)
        {
            if (level != null) level.text = "";
            return;
        }

        var D = DataManager.Instance;
        if (D == null)
        {
            level.text = "";
            return;
        }

        int lv = D.GetSkillLevel(SkillData);
        level.text = lv > 0 ? $"+{lv}" : "";
    }

    public void RefreshEquippedMark()
    {
        if (equippedMark == null || skillData == null)
        {
            if (equippedMark != null) equippedMark.gameObject.SetActive(false);
            return;
        }

        bool equipped = IsEquippedByPlayer(skillData);
        equippedMark.gameObject.SetActive(equipped);
    }

    private bool IsEquippedByPlayer(SkillData data)
    {
        if (_player == null || data == null) return false;
        return (_player.slot1 == data) || (_player.slot2 == data);
    }
}
