using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadSceneController : MonoBehaviour
{
    [Header("참조 UI")]
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text progressText;

    private void Start()
    {
       if (!tipText)
        {
            tipText.text = "던전으로 들어가는 중...";
        }
    }

    private void Update()
    {
        if (LoadSceneManager.Instance == null) return;

        float p = Mathf.Clamp01(LoadSceneManager.Instance.progress01);

        if (progressBar != null) progressBar.fillAmount = p;
        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(p * 100f)}%";
    }
}
