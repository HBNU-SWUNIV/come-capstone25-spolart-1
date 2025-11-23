using UnityEngine;

// 애니메이션 키프레임 이벤트를 위한 사운드 출력 함수

public class SFXController : MonoBehaviour
{
    [Header("SFX 설정")]
    [SerializeField] private AudioSource audioSource;

    public void PlaySound()
    {
        audioSource.Play();
    }
}
