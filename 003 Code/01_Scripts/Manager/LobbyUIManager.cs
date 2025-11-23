using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("버튼 부모 오브젝트")]
    [SerializeField] private GameObject main;
    [SerializeField] private GameObject selectPlay;
    [SerializeField] private GameObject selectOption;

    [Header("버튼 - 메인")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("버튼 - 플레이")]
    [SerializeField] private Button playSingleButton;
    [SerializeField] private Button playToMainButton;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(OnClickMainToPlay);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnClickMainToOptions);
        if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);

        if (playSingleButton != null) playSingleButton.onClick.AddListener(OnClickSinglePlay);
        if (playToMainButton != null) playToMainButton.onClick.AddListener(OnClickPlayToMain);
    }

    // 버튼 전환
    private void OnClickMainToPlay()
    {
        main.SetActive(false);
        selectPlay.SetActive(true);
    }

    private void OnClickPlayToMain()
    {
        main.SetActive(true);
        selectPlay.SetActive(false);
    }

    private void OnClickMainToOptions()
    {
        // 설정 버튼들 활성화
    }

    public void OnClickSinglePlay()
    {
        Debug.Log("싱글플레이 시작");
        GameManager.Instance.LobbyToTown();
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}
