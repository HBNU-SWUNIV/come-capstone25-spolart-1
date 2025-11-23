using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEffectPlayer : MonoBehaviour
{
    [Header("대상 UI")]
    [Tooltip("흔들/펄스/플래시 기준이 되는 아이콘. null이면 호출 시 인자로 받은 아이콘 사용")]
    [SerializeField] private Image defaultIcon;
    [Tooltip("패널/카드 전체 흔들림 대상")]
    [SerializeField] private RectTransform shakeTarget;

    [Header("파티클 (선택)")]
    [SerializeField] private ParticleSystem successParticlesPrefab;
    [SerializeField] private ParticleSystem failParticlesPrefab;

    [Header("사운드 (선택)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxSuccess;
    [SerializeField] private AudioClip sfxFail;

    [Header("텍스트 플로팅")]
    [SerializeField] private TMP_Text floatingTextPrefab;
    [SerializeField] private Transform floatingTextParent;

    [Header("텍스트 플로팅 설정")]
    [SerializeField] private float floatingRise = 30f;      // 얼마나 위로 올라갈지
    [SerializeField] private float floatingDuration = 0.6f; // 실제 이동+페이드 시간
    [SerializeField] private float floatingDelay = 0.2f;    // 생성 후 대기 시간

    // ---- 외부에서 호출하는 진입점 ----
    public void PlaySuccess(Image iconOverride = null, string text = "강화 성공!")
    {
        var icon = iconOverride ? iconOverride : defaultIcon;
        StartCoroutine(PlaySuccessRoutine(icon, text));
    }

    public void PlayFail(Image iconOverride = null, string text = "강화 실패")
    {
        var icon = iconOverride ? iconOverride : defaultIcon;
        StartCoroutine(PlayFailRoutine(icon, text));
    }

    // ---- 루틴 구현 ----
    private IEnumerator PlaySuccessRoutine(Image icon, string text)
    {
        if (audioSource && sfxSuccess) audioSource.PlayOneShot(sfxSuccess);

        if (successParticlesPrefab)
            SpawnAndPlayParticles(successParticlesPrefab, icon);

        if (floatingTextPrefab)
            SpawnFloatingText(text, icon, new Color(0.2f, 1f, 0.4f));

        // 성공: 초록/노란 톤 플래시 + 펀치 스케일
        if (icon) yield return StartCoroutine(Flash(icon, new Color(1f, 0.95f, 0.4f), 0.15f));
        if (icon) yield return StartCoroutine(PunchScale(icon.rectTransform, 1.15f, 0.18f));
    }

    private IEnumerator PlayFailRoutine(Image icon, string text)
    {
        if (audioSource && sfxFail) audioSource.PlayOneShot(sfxFail);

        if (failParticlesPrefab)
            SpawnAndPlayParticles(failParticlesPrefab, icon);

        if (floatingTextPrefab)
            SpawnFloatingText(text, icon, new Color(1f, 0.35f, 0.35f));

        // 실패: 빨간 플래시 + 패널 흔들림
        if (icon) yield return StartCoroutine(Flash(icon, new Color(1f, 0.3f, 0.3f), 0.12f));
        if (shakeTarget) yield return StartCoroutine(Shake(shakeTarget, 15f, 0.2f));
    }

    // ---- 유틸 ----
    private void SpawnAndPlayParticles(ParticleSystem prefab, Image nearIcon)
    {
        var parent = (nearIcon != null) ? nearIcon.transform : transform;
        var ps = Instantiate(prefab, parent.position, Quaternion.identity, parent);
        var main = ps.main;
        ps.Play();
        Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
    }

    private void SpawnFloatingText(string text, Image nearIcon, Color color)
    {
        var parent = floatingTextParent ? floatingTextParent : (nearIcon ? nearIcon.transform : transform);
        var label = Instantiate(floatingTextPrefab, parent);
        label.text = text;
        label.color = color;

        var rt = label.rectTransform;

        // 생성 후 잠깐 멈춰 있다가, 천천히 올라가면서 페이드아웃
        StartCoroutine(FloatAndFade(rt, label, floatingRise, floatingDuration, floatingDelay));
    }

    private IEnumerator FloatAndFade(RectTransform rt, TMP_Text label, float rise, float duration, float delay)
    {
        var start = rt.anchoredPosition;
        var end = start + Vector2.up * rise;
        var baseColor = label.color;
        // 처음에는 완전 불투명
        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        // --- 대기 시간 ---
        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        // --- 천천히 위로 & 페이드아웃 ---
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            rt.anchoredPosition = Vector2.Lerp(start, end, k);
            float alpha = Mathf.Lerp(1f, 0f, k);
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        Destroy(label.gameObject);
    }

    private IEnumerator PunchScale(RectTransform target, float peakScale, float duration)
    {
        if (!target) yield break;
        Vector3 start = Vector3.one;
        Vector3 peak = Vector3.one * peakScale;

        float half = duration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = t / half;
            target.localScale = Vector3.Lerp(start, peak, k);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = t / half;
            target.localScale = Vector3.Lerp(peak, start, k);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private IEnumerator Flash(Image img, Color flashColor, float duration)
    {
        if (!img) yield break;
        var original = img.color;
        img.color = flashColor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            img.color = Color.Lerp(flashColor, original, k);
            yield return null;
        }
        img.color = original;
    }

    private IEnumerator Shake(RectTransform target, float strength, float duration)
    {
        if (!target) yield break;
        var origin = target.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - (t / duration);
            var offset = Random.insideUnitCircle * strength * damper;
            target.anchoredPosition = origin + offset;
            yield return null;
        }
        target.anchoredPosition = origin;
    }
}
