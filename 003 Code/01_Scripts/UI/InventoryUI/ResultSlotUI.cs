using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultSlotUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amount;

    [Header("하이라이트")]
    [SerializeField] private GameObject highliteFrame; // 판매 중일 때 테두리 설정할 테두리 오브젝트

    public void Set(Sprite sprite, int count)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (amount != null)
        {
            amount.text = $"x{count}";
        }
    }

    public void SetHighlight(bool on)
    {
        if (highliteFrame != null)
        {
            highliteFrame.SetActive(on);
        }
    }
}
