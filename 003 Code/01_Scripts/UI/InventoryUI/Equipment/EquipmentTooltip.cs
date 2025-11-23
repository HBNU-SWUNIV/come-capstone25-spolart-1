using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentTooltip : MonoBehaviour
{
    public static EquipmentTooltip Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private CanvasGroup cg;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text rarityText;
    [SerializeField] TMP_Text damageText;
    [SerializeField] TMP_Text attackSpeedText;
    [SerializeField] TMP_Text critChanceText;
    [SerializeField] TMP_Text durabilityText;

    [Header("마우스 오프셋")]
    [SerializeField] private Vector2 offset = new Vector2(16f, -16f);

    private bool _visible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideImmediate();
    }

    private void Update()
    {
        if (!_visible) return;

        Vector2 mousePos;

        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else
        {
            mousePos = rectTransform.position;
        }

        rectTransform.position = mousePos + offset;
    }

    public void Show(EquipmentItem item)
    {
        if (item == null || item.data == null)
        {
            Hide();
            return;
        }

        if (titleText != null) titleText.text = item.Name;

        if (rarityText != null) rarityText.text = $"{item.Type} / {item.Rarity}";
        if (damageText != null) damageText.text = $"공격력: {item.AttackDamage}";
        if (attackSpeedText != null) attackSpeedText.text = $"공격속도: {item.Speed:0.##}";
        if (critChanceText != null) critChanceText.text = $"크리티컬 확률: {item.CriticalChance * 100f:0.#}%";
        if (durabilityText != null) durabilityText.text = $"내구도: {item.currentDurability} / {item.MaxDurability}";

        _visible = true;
        cg.alpha = 1.0f;
        cg.blocksRaycasts = false;
    }

    public void Hide()
    {
        _visible = false;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
    }

    private void HideImmediate()
    {
        _visible = false;
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
        }
    }
}
