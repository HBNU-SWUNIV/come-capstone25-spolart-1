using System.Collections;
using System.Diagnostics.Eventing.Reader;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// 크리티컬이 떴을 때 코루틴 따로 작성

public class HitText : MonoBehaviour
{
    [Header("타격 텍스트 설정")]
    [SerializeField] private float floatSpeed; // 텍스트가 올라가는 속도
    [SerializeField] private float riseDuration; // 텍스트가 올라가는데 걸리는 속도
    [SerializeField] private float fadeDuration; // 텍스트가 투명해지는 데 걸리는 시간
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector3 riseOffset = new Vector3(0, 0.2f, 0); // 텍스트가 올라가는 거리

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color critColor = Color.red;
    [SerializeField] private Color playerColor = Color.aquamarine;

    [Header("크리티컬 연출")]
    [SerializeField] private float critScale = 1.2f; // 크리티컬 시 확대 스케일
    [SerializeField] private string critPrefix = "CRITICAL!";

    public TMP_Text damageText;
    
    private Color textColor;
    private Vector3 baseScale;

    public void Initialize(float dmg, bool isCritical, bool isPlayer)
    {
        baseScale = transform.localScale;
        textColor = isCritical ? critColor : normalColor;

        if (isPlayer)
        {
            textColor = playerColor;
        }

        if (isCritical)
        {
            damageText.text = $"<b>{critPrefix}<b>\n<b>{dmg}<b>";
            transform.localScale = baseScale * critScale;
        }
        else
        {
            damageText.text = dmg.ToString();
        }

        damageText.color = textColor;

        StartCoroutine(MoveAndFade());
    }

    private IEnumerator MoveAndFade()
    {
        Vector3 startPosition = transform.position + offset;
        Vector3 endPosition = startPosition + riseOffset;

        float elapsedTime = 0;

        while (elapsedTime < riseDuration)
        {
            // Vector3.Lerp(시작 위치, 종료 위치, 걸리는 시간) : 위치 선형 보간
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / riseDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0;

        while (elapsedTime < fadeDuration)
        {
            // Mathf.Lerp() : 실수 값 선형 보간
            textColor.a = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            damageText.color = textColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(this.gameObject);
    }
}
