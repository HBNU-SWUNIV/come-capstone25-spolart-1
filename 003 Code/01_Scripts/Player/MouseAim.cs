using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MouseAim : MonoBehaviour
{
    public static MouseAim Instance { get; private set; }

    [Header("레이 시작 위치")]
    [SerializeField] private Transform origin;

    [Header("플레이어 스프라이트 오브젝트")]
    [SerializeField] private SpriteRenderer[] flipTarget; // 플레이어 오브젝트
    [SerializeField] private bool autoFlip = true;
    
    private Camera _cam;

    public Transform Origin => origin != null ? origin : transform; // 없을 시 예외처리
    public Vector3 MouseWorld { get; private set; }
    public Vector2 Direction { get; private set; } = Vector2.right;
    public bool LookLeft { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 최초 1회 시도
        RebindCamera();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindCamera();
    }

    private void LateUpdate()
    {
        if (CutSceneManager.Instance?.isCutScenePlaying == true) return;

        if (_cam == null) return;

        // 마우스 월드 좌표
        Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        float z = Mathf.Abs(_cam.transform.position.z - Origin.position.z);
        MouseWorld = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, z));

        // 마우스 에임 방향
        Vector2 raw = (MouseWorld - Origin.position);
        Direction = raw.sqrMagnitude > 0.0001f ? raw.normalized : Direction;

        // 좌우 판정
        LookLeft = Direction.x < 0f;
        if (autoFlip && flipTarget != null)
        {
            for (int i = 0; i < flipTarget.Length; i++)
            {
                if (flipTarget[i] != null)
                {
                    flipTarget[i].flipX = LookLeft;
                }
            }
        }
    }

    public void RebindCamera()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            Debug.LogWarning("[MouseAim] 메인 카메라를 찾지 못했습니다. 씬에 Camera 태그가 있는 카메라가 필요합니다.");
        }
    }

    public Vector2 GetAimDirection() => Direction;
    public Transform GetRayOrigin() => Origin;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Origin == null) return;
        Gizmos.DrawLine(Origin.position, Origin.position + (Vector3)(Direction * 1.5f));
    }
#endif
}
