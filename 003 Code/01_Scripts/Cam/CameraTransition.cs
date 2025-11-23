using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraTransition : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private CameraFollow follow;
    [SerializeField] private Transform player;

    [Header("전환 설정")]
    [SerializeField] private float descendDistance = 50f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private AnimationCurve curve = null;
    [Tooltip("음수면 줌 인(orthoSize 감소)")]
    [SerializeField] private float zoomDelta = -1.0f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("페이드(기존 Canvas 사용)")]
    [SerializeField] private Canvas fadeCanvas;     // Screen Space Overlay 권장
    [SerializeField] private Image fadeImage;       // 풀스크린 검은색 Image
    [SerializeField] private CanvasGroup fadeGroup; // Alpha 제어용
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float fadeHold = 0.1f;
    [SerializeField] private float fadeInDuration = 0.6f;

    private bool busy;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (curve == null) curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 안전장치: 초기 알파/활성
        if (fadeGroup) fadeGroup.alpha = 0f;
        if (fadeCanvas) fadeCanvas.enabled = false;
        if (fadeImage) fadeImage.raycastTarget = false;
    }

    public void Play(Vector3 nextFloorSpawnPos)
    {
        if (busy || !mainCam || !player) return;
        StartCoroutine(Co_Play(nextFloorSpawnPos));
    }

    private IEnumerator Co_Play(Vector3 nextFloorSpawnPos)
    {
        busy = true;

        // 0) 팔로우 정지 & 페이드 켜기
        if (follow) follow.IsEnabled = false;
        if (fadeCanvas) fadeCanvas.enabled = true;
        if (fadeImage) fadeImage.raycastTarget = true; // 입력 차단
        if (fadeGroup) fadeGroup.alpha = 0f;

        // 1) 시작값
        Vector3 camStart = mainCam.transform.position;
        float startOrtho = mainCam.orthographicSize;
        float targetOrtho = Mathf.Max(0.01f, startOrtho + zoomDelta);

        // 2) 하강+줌과 페이드아웃 동시 진행
        float t = 0f, fe = 0f;
        while (t < 1f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / duration;
            float k = curve.Evaluate(Mathf.Clamp01(t));

            // 카메라 하강
            Vector3 pos = camStart + new Vector3(0f, -descendDistance * k, 0f);
            mainCam.transform.position = pos;

            // 줌
            mainCam.orthographicSize = Mathf.Lerp(startOrtho, targetOrtho, k);

            // 페이드 아웃
            if (fadeGroup && fadeGroup.alpha < 1f)
            {
                fe += dt;
                fadeGroup.alpha = Mathf.Clamp01(fe / Mathf.Max(0.0001f, fadeOutDuration));
            }

            yield return null;
        }
        if (fadeGroup) fadeGroup.alpha = 1f;

        // 4) 텔레포트 & 카메라 위치 맞춤
        player.position = nextFloorSpawnPos;
        Vector3 p = mainCam.transform.position;
        mainCam.transform.position = new Vector3(nextFloorSpawnPos.x, nextFloorSpawnPos.y, p.z);

        // 5) 줌 원복 + 페이드 인
        float restoreDur = duration * 0.5f;
        float r = 0f, fie = 0f;
        float restoreStart = mainCam.orthographicSize;

        while (r < 1f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            r += dt / restoreDur;
            float kk = curve.Evaluate(Mathf.Clamp01(r));

            mainCam.orthographicSize = Mathf.Lerp(restoreStart, startOrtho, kk);

            if (fadeGroup && fadeGroup.alpha > 0f)
            {
                fie += dt;
                fadeGroup.alpha = 1f - Mathf.Clamp01(fie / Mathf.Max(0.0001f, fadeInDuration));
            }

            yield return null;
        }
        if (fadeGroup) fadeGroup.alpha = 0f;

        // 6) 팔로우 재개 & 페이드 비활성
        if (follow) follow.IsEnabled = true;
        if (fadeImage) fadeImage.raycastTarget = false;
        if (fadeCanvas) fadeCanvas.enabled = false;

        busy = false;
    }
}
