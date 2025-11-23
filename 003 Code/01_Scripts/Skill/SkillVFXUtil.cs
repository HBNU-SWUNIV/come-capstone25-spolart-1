using UnityEngine;

/// 스킬 VFX 스폰/파괴 유틸
/// - Animator: RuntimeAnimatorController의 애니메이션 길이를 사용(루프면 fallback)
/// - 아무것도 없으면 fallbackLifetime로 파괴
/// 
public class SkillVFXUtil : MonoBehaviour
{
    /// <param name="prefab">VFX 프리팹(ParticleSystem 또는 Animator 포함 가능)</param>
    /// <param name="pos">스폰 위치</param>
    /// <param name="rotation">Z-회전 포함 회전값</param>
    /// <param name="fallbackLifetime">루프이거나 길이를 알 수 없을 때 사용</param>
    /// <param name="parent">따라붙게 하고 싶으면 지정</param>
    /// 
    public static GameObject SpawnVFX(GameObject prefab, Vector3 pos, Quaternion rotation, float fallbackLifetime, Transform parent = null)
    {
        if (!prefab) return null;

        var go = Object.Instantiate(prefab, pos, rotation, parent);

        // 1) ParticleSystem 우선
        var ps = go.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            var main = ps.main;
            // 루프가 아니면 시스템 재생 시간 + 파티클 최대 수명까지
            if (!main.loop)
            {
                float total = main.duration + main.startLifetime.constantMax;
                Object.Destroy(go, Mathf.Max(0.05f, total));
            }
            else
            {
                Object.Destroy(go, Mathf.Max(0.05f, fallbackLifetime));
            }
            return go;
        }

        // 2) Animator 지원
        var anim = go.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            float clipLen = 0f;
            bool allLoop = true;

            var ctrl = anim.runtimeAnimatorController;
            if (ctrl != null && ctrl.animationClips != null && ctrl.animationClips.Length > 0)
            {
                foreach (var clip in ctrl.animationClips)
                {
                    if (clip == null) continue;
#if UNITY_2021_3_OR_NEWER
                    bool isLoop = clip.isLooping; // Loop Time 체크
#else
                    bool isLoop = clip.wrapMode == WrapMode.Loop;
#endif
                    if (!isLoop) allLoop = false;
                    clipLen = Mathf.Max(clipLen, clip.length);
                }

                if (!allLoop && clipLen > 0f)
                {
                    Object.Destroy(go, Mathf.Max(0.05f, clipLen));
                }
                else
                {
                    Object.Destroy(go, Mathf.Max(0.05f, fallbackLifetime));
                }
            }
            else
            {
                // 컨트롤러/클립이 없으면 fallback
                Object.Destroy(go, Mathf.Max(0.05f, fallbackLifetime));
            }
            return go;
        }

        // 3) 그 외: fallback
        Object.Destroy(go, Mathf.Max(0.05f, fallbackLifetime));
        return go;
    }

    /// <summary>간편 SFX 재생</summary>
    public static void PlaySFX(AudioClip clip, Vector3 at, float volume = 1f)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, at, Mathf.Clamp01(volume));
    }

    /// <summary>2D에서 방향 벡터를 Z-회전으로 변환</summary>
    public static Quaternion DirectionToZRot(Vector2 dir)
    {
        // (y, x)가 아닌 (x, y) 순서가 맞습니다.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
