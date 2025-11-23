using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuantityPromptPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject RemovePrompt;

    private int _max = 0;
    private Action<int> _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        if (okButton != null) okButton.onClick.AddListener(OnClickOK);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnClickCancel);
        RemovePrompt.SetActive(false);

        Debug.Log("QuantityPromptPanel 설정 완료");
    }

    /// <summary>
    /// 수량 입력 패널 표시
    /// </summary>
    /// <param name="defaultValue">기본 입력값(예: 1 또는 최대치)</param>
    /// <param name="max">최대 입력 가능 수량</param>
    /// <param name="onConfirm">확정 시 콜백(입력 수량)</param>
    /// <param name="onCancel">취소 시 콜백(옵션)</param>
    public void Show(int defaultValue, int max, Action<int> onConfirm, Action onCancel = null)
    {
        _max = Mathf.Max(0, max);
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        RemovePrompt.SetActive(true);

        if (inputField != null)
        {
            int clamped = Mathf.Clamp(defaultValue, 1, Mathf.Max(1, _max));
            inputField.text = clamped.ToString();
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    public void Hide()
    {
        RemovePrompt.SetActive(false);
        _onConfirm = null;
        _onCancel = null;
        _max = 0;
    }

    private void OnClickOK()
    {
        int val = ParseInput();
        if (val <= 0)
        {
            // 0 또는 잘못 입력이면 최소 1로 보정
            val = 1;
        }
        val = Mathf.Clamp(val, 1, Mathf.Max(1, _max));
        Debug.Log(val);

        _onConfirm?.Invoke(val);
        Hide();
    }

    private void OnClickCancel()
    {
        _onCancel?.Invoke();
        Hide();
    }

    private int ParseInput()
    {
        if (inputField == null) return 1;
        if (int.TryParse(inputField.text, out int v)) return v;
        return 1;
    }
}