using UnityEngine;
using UnityEngine.EventSystems;

public class EquippedSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private InventoryEquippedView equippedView;
    [SerializeField] private EquipmentType slotType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EquipmentTooltip.Instance == null || equippedView == null)
            return;

        EquipmentItem item = null;
        if (slotType == EquipmentType.Weapon)
            item = equippedView.CurrentWeapon;
        else
            item = equippedView.CurrentMiningtool;

        if (item != null)
        {
            EquipmentTooltip.Instance.Show(item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EquipmentTooltip.Instance != null)
        {
            EquipmentTooltip.Instance.Hide();
        }
    }
}
