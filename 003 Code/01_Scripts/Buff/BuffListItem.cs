using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제사장 버프 구매 및 업그레이드 리스트 출력 스크립트
/// </summary>
/// 

public class BuffListItem : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text buffName;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedHighlight; // 선택시 리스트 하이라이트

    public BuffData Data { get; private set; }
    private System.Action<BuffListItem> _onClick;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    public void Setup(BuffData data, System.Action<BuffListItem> onclick)
    {
        Data = data;
        _onClick = onclick;

        if (icon) icon.sprite = data != null ? data.buffIcon : null;
        if (buffName) buffName.text = data != null ? data.buffName : "-";

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(this));
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight) selectedHighlight.SetActive(selected);
    }
}
