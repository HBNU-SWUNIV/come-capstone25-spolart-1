using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InventoryEquippedView : MonoBehaviour
{
    [Header("장비 아이콘")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image miningToolIcon;

    [Header("아이콘 없을 때 색상/투명도")]
    [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.2f);

    private PlayerEquipment _playerEquipment;

    public EquipmentItem CurrentWeapon => _playerEquipment?.CurrentWeapon;
    public EquipmentItem CurrentMiningtool => _playerEquipment?.CurrentMiningTool;

    private void Awake()
    {
        TryBindPlayerEquipment();
    }

    private void OnEnable()
    {
        TryBindPlayerEquipment();
        if (_playerEquipment != null)
            HandleEquipmentChanged();
    }

    private void Update()
    {
        // 아직 못 찾았으면 매 프레임 한 번씩 다시 시도
        if (_playerEquipment == null)
        {
            TryBindPlayerEquipment();
        }
    }

    private void OnDestroy()
    {
        if (_playerEquipment != null)
        {
            _playerEquipment.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    private void TryBindPlayerEquipment()
    {
        if (_playerEquipment != null) return;

#if UNITY_6000_OR_NEWER
        var pe = FindAnyObjectByType<PlayerEquipment>();
#else
        var pe = FindObjectOfType<PlayerEquipment>();
#endif

        if (pe == null) return;

        _playerEquipment = pe;

        // 중복 구독 방지
        _playerEquipment.OnEquipmentChanged -= HandleEquipmentChanged;
        _playerEquipment.OnEquipmentChanged += HandleEquipmentChanged;

        // 바로 한 번 갱신
        HandleEquipmentChanged();
    }

    private void HandleEquipmentChanged()
    {
        if (_playerEquipment == null) return;

        var weapon = _playerEquipment.CurrentWeapon;
        var mining = _playerEquipment.CurrentMiningTool;

        // 무기 아이콘
        if (weaponIcon != null)
        {
            if (weapon != null && weapon.Icon != null)
            {
                weaponIcon.sprite = weapon.Icon;
                weaponIcon.color = Color.white;
            }
            else
            {
                weaponIcon = null;
                weaponIcon.color = emptyColor;
            }
        }

        // 채광도구 아이콘
        if (miningToolIcon != null)
        {
            if (mining != null && mining.Icon != null)
            {
                miningToolIcon.sprite = mining.Icon;
                miningToolIcon.color = Color.white;
            }
            else
            {
                miningToolIcon = null;
                miningToolIcon.color = emptyColor;
            }
        }
    }
}
