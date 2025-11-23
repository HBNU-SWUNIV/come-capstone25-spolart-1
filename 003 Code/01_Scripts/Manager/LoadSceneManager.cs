using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance { get; private set; }

    [HideInInspector] public string nextScene;

    [SerializeField] private bool waitForGeneration;

    [Tooltip("씬 로드/생성 진행률(0~1). LoadingScene UI에서 읽어 표시)")]
    [Range(0f, 1f)] public float progress01;

    private bool _generationCompleted;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
        }
        else 
        { 
            Destroy(gameObject); 
            return; 
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 일반 로딩
    public void LoadScene(string sceneName)
    {
        Debug.Log("일반 로딩 씬");
        nextScene = sceneName;
        waitForGeneration = false;
        _generationCompleted = false;
        progress01 = 0f;

        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
    }

    // 던전 생성 대기 로딩
    public void LoadSceneWaitGenerator(string sceneName)
    {
        Debug.Log("던전 로딩 씬");
        nextScene = sceneName;
        waitForGeneration = true;
        _generationCompleted = false;
        progress01 = 0f;

        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
    }

    public void MarkGenerationComplete()
    {
        _generationCompleted = true;
        Debug.Log("던전 배치 완료!");
    }


    // LoadingScene이 로드되면 자동으로 다음 씬 비동기 로딩 시작
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 로딩 씬에서만 다음 단계 실행
        if (scene.name != "LoadingScene") return;

        Debug.Log("로딩 씬 로드 완료. 다음 씬 로드 시작");
        StopAllCoroutines();
        StartCoroutine(waitForGeneration ? LoadTargetAdditiveAndAwait(nextScene) : LoadTargetSingle(nextScene));
    }

    private IEnumerator LoadTargetSingle(string target)
    {
        Debug.Log("싱글 씬 로드 코루틴 시작");

        progress01 = 0f;

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[LoadSceneManager] nextScene이 비어있습니다.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"[LoadSceneManager] LoadSceneAsync('{target}')가 null입니다. " +
                           "Build Settings에 등록됐는지 / 씬 이름 오타가 없는지 확인하세요.");
            yield break;
        }

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            progress01 = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        progress01 = 1f;
        op.allowSceneActivation = true;
    }

    private IEnumerator LoadTargetAdditiveAndAwait(string target)
    {
        Debug.Log("애디티브 씬 로드 코루틴 시작");

        progress01 = 0f;

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[LoadSceneManager] nextScene이 비어있습니다.");
            yield break;
        }

        var load = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);

        if (load == null)
        {
            Debug.LogError($"[LoadSceneManager] LoadSceneAsync('{target}')가 null입니다. " +
                           "Build Settings에 등록됐는지 / 씬 이름 오타가 없는지 확인하세요.");
            yield break;
        }

        load.allowSceneActivation = true;

        while (!load.isDone)
        {
            progress01 = Mathf.MoveTowards(progress01, Mathf.Clamp01(load.progress), Time.unscaledDeltaTime * 2f);
            yield return null;
        }

        // 활성 씬 전환
        var targetScene = SceneManager.GetSceneByName(target);
        if (targetScene.IsValid())
        {
            SceneManager.SetActiveScene(targetScene);
        }

        const float safetyTimeout = 180f;
        float t = 0f;
        while (!_generationCompleted && t < safetyTimeout)
        {
            progress01 = Mathf.MoveTowards(progress01, 0.98f, Time.unscaledDeltaTime * 0.5f);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        progress01 = 1f;
        yield return new WaitForSecondsRealtime(0.05f);

        // 로딩 씬 언로드
        var loading = SceneManager.GetSceneByName("LoadingScene");
        if (loading.IsValid())
        {
            yield return SceneManager.UnloadSceneAsync("LoadingScene");
        }

        Debug.Log("로딩씬 언로드");
    }

    /// <summary>
    /// 던전 생성기(RoomFirstDungeonGenerator)가 호출하는 진행 보고.
    /// 로딩씬에서는 60%~98% 구간을 '생성 중'으로 사용.
    /// </summary>
    public void ReportGeneratorProgress(float normalized01)
    {
        normalized01 = Mathf.Clamp01(normalized01);
        // 0.60(씬 로드 완료 이후) ~ 0.98(생성 완료 직전) 구간에 매핑
        progress01 = Mathf.Lerp(0, 0.98f, normalized01);
    }
}
