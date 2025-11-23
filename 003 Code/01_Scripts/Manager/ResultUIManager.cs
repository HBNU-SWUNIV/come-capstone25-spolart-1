using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUIManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Transform resultPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private ResultSlotUI slotPrefab;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text clearTime;
    [SerializeField] private TMP_Text clearFloor;
    [SerializeField] private TMP_Text endReasonText;
    [SerializeField] private GameObject gameoverUI;
    [SerializeField] private GameObject statusUI;
    [SerializeField] private Button confirmButton;

    [Header("게임오버 연출")]
    [SerializeField] private CanvasGroup gameoverCG;
    [SerializeField] private Animator animator; // 플레이어 사망시 스프라이트 애니메이션 
    [SerializeField] private float fadeDuration = 1.5f; // 페이드 아웃 연출 시간

    [Header("판매 애니메이션")]
    [SerializeField] private float sellPerSlotDuration = 0.5f;
    [SerializeField] private AnimationCurve sellCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool autoStartOnEnable = true;
    [SerializeField] private bool clearContainerOnShow = true;

    private readonly List<ResultSlotUI> _spawned = new();
    private Coroutine _sellingRoutine;
    private int _currentMoney;

    private void Awake()
    {
        // GameManager에 자기 등록
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterResultUI(this);

        // resultPanel이 지정 안 되었으면 자기 자신 사용
        if (resultPanel == null) resultPanel = transform;

        // 매니저 오브젝트는 켜두고, 패널만 숨겨두기
        resultPanel.gameObject.SetActive(false);

        confirmButton.onClick.AddListener(() => GameManager.Instance.OnResultConfirmed());

        // gameoverUI의 CanvasGroup 초기화
        if (gameoverCG == null && gameoverUI != null)
        {
            gameoverCG = gameoverUI.GetComponent<CanvasGroup>();
        }
        if (gameoverCG != null)
        {
            gameoverCG.alpha = 0;
            gameoverCG.interactable = false; // 상호작용도 막음
        }
    }

    private void OnEnable()
    {
        // 이 스크립트 오브젝트가 꺼졌다 켜질 수도 있으니 옵션 유지
        if (autoStartOnEnable && resultPanel.gameObject.activeSelf)
            ShowAndPlay();
    }

    // 외부에서 호출할 공개 API: 패널 켜기 + 실행
    public void Show()
    {
        if (resultPanel == null) resultPanel = transform;
        if (!gameObject.activeSelf) gameObject.SetActive(true); // 스크립트 오브젝트 켜기
        if (resultPanel != null) resultPanel.gameObject.SetActive(true); // 실제 패널 켜기
        if (statusUI != null) statusUI.SetActive(false);

        float t = GameManager.Instance.GetDungeonPlayTime();
        clearTime.text = FormatHMS(t); // 시간 표시
        RefreshEndReason();

        ShowAndPlay();
    }

    // 외부에서 호출할 공개 API: 패널 끄기
    public void Hide()
    {
        if (_sellingRoutine != null)
        {
            StopCoroutine(_sellingRoutine);
            _sellingRoutine = null;
        }
        resultPanel.gameObject.SetActive(false);
    }

    // 내부 진입점
    private void ShowAndPlay()
    {
        BuildSlots();           // RunInventory에서 읽어 슬롯 채우기
        StartSellingAnimation();// 돈 증가 애니메이션
    }

    private string FormatHMS(float sec)
    {
        var ts = TimeSpan.FromSeconds(sec);
        return ts.ToString(@"hh\:mm\:ss");
    }

    private void BuildSlots()
    {
        if (clearContainerOnShow)
        {
            foreach (var s in _spawned) if (s) Destroy(s.gameObject);
            _spawned.Clear();
        }

        _currentMoney = 0;
        if (moneyText != null) moneyText.text = "0";

        // GameManager에서 라인 생성
        int grandTotal;
        var lines = GameManager.Instance.BuildRunResultLines(out grandTotal);

        foreach (var line in lines)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            var icon = line.ore ? line.ore.OreIcon : null;
            slot.Set(icon, line.count);     // 슬롯 UI가 (아이콘, 수량) 받는 버전이라 가정
            slot.SetHighlight(false);
            _spawned.Add(slot);
        }
    }

    private void StartSellingAnimation()
    {
        if (_sellingRoutine != null) StopCoroutine(_sellingRoutine);
        _sellingRoutine = StartCoroutine(SellingRoutine());
    }

    // (원하면 버튼에 연결) 전체 스킵
    public void SkipAnimationToEnd()
    {
        if (_sellingRoutine != null)
        {
            StopCoroutine(_sellingRoutine);
            _sellingRoutine = null;
        }

        int total;
        GameManager.Instance.BuildRunResultLines(out total);
        _currentMoney = total;
        if (moneyText != null) moneyText.text = _currentMoney.ToString("N0");

        foreach (var s in _spawned) s.SetHighlight(false);
    }

    private IEnumerator SellingRoutine()
    {
        int grandTotal;
        var lines = GameManager.Instance.BuildRunResultLines(out grandTotal);

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var slotUI = _spawned[i];

            slotUI.SetHighlight(true);

            int from = _currentMoney;
            int to = _currentMoney + line.total;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, sellPerSlotDuration);
                float k = sellCurve.Evaluate(Mathf.Clamp01(t));
                int val = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
                if (val != _currentMoney)
                {
                    _currentMoney = val;
                    if (moneyText != null) moneyText.text = _currentMoney.ToString("N0");
                }
                yield return null;
            }

            _currentMoney = to;
            if (moneyText != null) moneyText.text = _currentMoney.ToString("N0");
            slotUI.SetHighlight(false);

            var cg = slotUI.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0.8f;
        }

        _sellingRoutine = null;
    }

    public void ShowGameoverUI()
    {
        // 페이드 연출 코루틴 시작
        StartCoroutine(GameoverFadeRoutine());
    }

    private IEnumerator GameoverFadeRoutine()
    {
        gameoverUI.SetActive(true);
        if (gameoverCG == null)
        {
            gameoverCG = gameoverUI.GetComponent<CanvasGroup>();
        }

        gameoverCG.alpha = 0f; // 초기값 0
        gameoverCG.interactable = false; // 클릭 방지

        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }

        float timer = 0f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // 게임 시간이 멈춰도 UI는 움직이도록 unscaledDeltaTime 사용
            float progress = Mathf.Clamp01(timer / fadeDuration);

            if (gameoverCG != null)
            {
                gameoverCG.alpha = progress;
            }

            yield return null;
        }

        if (gameoverCG != null)
        {
            gameoverCG.alpha = 1;
            gameoverCG.interactable = true;
        }
    }

    private void RefreshEndReason()
    {
        if (endReasonText == null || GameManager.Instance == null) return;

        var reason = GameManager.Instance.GetDungeonEndReason();
        switch (reason)
        {
            case GameManager.RunEndReason.Success:
                endReasonText.text = "보스 처치!";
                endReasonText.color = new Color(0.25f, 0.85f, 0.4f); // 초록
                break;

            case GameManager.RunEndReason.Giveup:
                endReasonText.text = "포기";
                endReasonText.color = new Color(1.0f, 0.8f, 0.2f);   // 노랑
                break;

            case GameManager.RunEndReason.Death:
                endReasonText.text = "사망";
                endReasonText.color = new Color(0.95f, 0.3f, 0.3f);  // 빨강
                break;
        }
    }

    // 에디터에서 바로 테스트하고 싶으면 우클릭 메뉴용
    [ContextMenu("Show (Test)")]
    private void __Context_Show() => Show();

    [ContextMenu("Hide (Test)")]
    private void __Context_Hide() => Hide();
}
