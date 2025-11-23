using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPopupUI : MonoBehaviour
{
    private enum PopupMode
    {
        None,
        Offer,  // 수주
        TurnIn  // 완료 보고
    }

    [Header("루트 패널")]
    [SerializeField] private GameObject rootPanel;

    [Header("텍스트 UI")]
    [SerializeField] private TMP_Text stateText; // 퀘스트 수락 / 퀘스트 완료
    [SerializeField] private TMP_Text titleText; // 퀘스트 제목
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rewardText;

    [Header("버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    private PopupMode _mode = PopupMode.None;
    private QuestData _currentDef;
    private Action _onConfirmed;

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void ShowOffer(QuestData def, Action onAccepted)
    {
        Debug.Log($"퀘스트 수락 창 오픈. 퀘스트 정보: {def.questId}");

        if (def == null)
        {
            Debug.LogWarning("[QuestPopupUI] ShowOffer: def is null");
            return;
        }

        _mode = PopupMode.Offer;
        _currentDef = def; 
        _onConfirmed = onAccepted;
        
        if (stateText) stateText.text = "[퀘스트 수락]";
        if (titleText) titleText.text = def.title;
        if (descriptionText) descriptionText.text = def.description;
        if (rewardText) rewardText.text = def.rewardMoney > 0 ? $"{def.rewardMoney:N0}" : "없음";

        if (confirmButton != null)
        {
            var label = confirmButton.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "수락";
        }

        Show();
    }

    public void ShowTurnIn(QuestData def, Action onCompleted)
    {
        Debug.Log($"퀘스트 완료 창 오픈. 퀘스트 정보: {def.questId}");

        if (def == null)
        {
            Debug.LogWarning("[QuestPopupUI] ShowTurnIn: def is null");
            return;
        }

        _mode = PopupMode.TurnIn;
        _currentDef = def;
        _onConfirmed = onCompleted;

        if (stateText) stateText.text = "[퀘스트 완료]";
        if (titleText) titleText.text = def.title;

        if (descriptionText)
        {
            descriptionText.text =
                string.IsNullOrEmpty(def.description)
                    ? "의뢰 목표를 모두 달성했습니다.\n보상을 수령하시겠습니까?"
                    : def.description + "\n\n의뢰 목표를 모두 달성했습니다.\n보상을 수령하시겠습니까?";
        }

        if (rewardText) rewardText.text = def.rewardMoney > 0
            ? $"{def.rewardMoney:N0} G"
            : "없음";

        if (confirmButton != null)
        {
            var label = confirmButton.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "완료";
        }

        Show();
    }

    private void Show()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Hide()
    {
        _mode = PopupMode.None;
        _currentDef = null;
        _onConfirmed = null;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnClickConfirm()
    {
        _onConfirmed?.Invoke();
        Hide();
    }
}
