using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestListItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] GameObject selectedHighlight;
    [SerializeField] private Button rowButton;

    public QuestData Source { get; private set; }

    private System.Action<QuestData> _onSelected;

    public void Bind(QuestData def, int cur, int max, System.Action<QuestData> onSelected)
    {
        Source = def;
        _onSelected = onSelected;

        if (titleText) titleText.text = def.title;
        SetProgress(cur, max);

        if (rowButton)
        {
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => _onSelected?.Invoke(def));
        }
        SetSelected(false);
    }

    // 리롤 시 내용 교체
    public void Rebind(QuestData def, int cur, int max)
    {
        Source = def;
        if (titleText) titleText.text = def.title;
        SetProgress(cur, max);
    }


    public void SetProgress(int cur, int max)
    {
        if (!progressFill) return;
        if (!progressText) return;

        if (max <= 0)
        {
            progressFill.fillAmount = 0f;
            return;
        }

        // 정수형을 실수형으로 변환하고, Clamp01 함수를 이용해 0~1로 수치 수정 후 fillAmount 적용
        float progressRatio = (float)cur / max;
        progressRatio = Mathf.Clamp01(progressRatio);
            
        progressFill.fillAmount = progressRatio;
        progressText.text = $"{cur} / {max}";
    }

    public void SetSelected(bool on)
    {
        if (selectedHighlight) selectedHighlight.SetActive(on);
    }
}
