using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairUI : MonoBehaviour
{
    [Header("UI ÂüÁ¶")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        // ½ÃÀÛ ½Ã ÆË¾÷ ¼û±è
        if (popupContainer)
        {
            popupContainer.SetActive(false);
        }
    }

    public void Show(int cost, Action onConfirm)
    {
        if (costText)
        {
            costText.text = cost.ToString("N0");
        }

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        confirmButton.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            Close();
        });

        cancelButton.onClick.AddListener(Close);

        if (popupContainer)
        {
            popupContainer.SetActive(true);
        }
    }

    private void Close()
    {
        if (popupContainer)
        {
            popupContainer.SetActive(false);
        }
    }
}
