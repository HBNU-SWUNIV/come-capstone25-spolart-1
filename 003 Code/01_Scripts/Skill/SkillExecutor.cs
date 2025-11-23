using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/* 
 * 스킬별 실제 동작 구현 
 */

public class SkillExecutor : MonoBehaviour
{
    // 시전 이펙트 재생
    public static void PlayCastFX(PlayerSkillSystem caster, SkillData data)
    {
        if (data == null || data.castVFX == null) return;

        var t = caster.transform;
        var aim = caster.GetComponent<MouseAim>();
        Vector2 dir = aim ? aim.Direction : Vector2.right;
        Vector3 spawnPos = t.position;
        Quaternion rotation = SkillVFXUtil.DirectionToZRot(dir);

        // 스킬 발동 기준에 따른 VFX 프리팹 생성 위치 설정
        switch (data.castAnchor)
        {
            case SkillEffectAnchor.Caster:
                spawnPos = t.position + (Vector3)data.castOffset;
                break;
            case SkillEffectAnchor.Mouse:
                // 마우스 방향으로 range나 radius만큼 앞으로 시전
                float d = Mathf.Max(data.range, data.radius, 0.5f);
                spawnPos = t.position + (Vector3)(dir.normalized * d) + (Vector3)data.castOffset;
                break;
            case SkillEffectAnchor.InFront:
                spawnPos = t.position + (Vector3)(dir.normalized * Mathf.Max(0.5f, data.range)) + (Vector3)data.castOffset;
                break;
            case SkillEffectAnchor.CustomPoint:
                spawnPos = t.position + (Vector3)data.castOffset;
                break;
        }

        // VFX 및 SFX 생성, 재생
        var go = SkillVFXUtil.SpawnVFX(data.castVFX, spawnPos, rotation, data.vfxLifetime, data.castFollowCaster ? t : null);
        SkillVFXUtil.PlaySFX(data.castSFX, spawnPos, data.sfxVolume);
    }

    // 스킬 히트 이펙트 재생
    private static void PlayHitFX(Vector3 at, SkillData data, Vector2 ? faceDir = null)
    {
        if (data == null) return;

        if (data.hitVFX != null)
        {
            Quaternion rotation = faceDir.HasValue ? SkillVFXUtil.DirectionToZRot(faceDir.Value) : Quaternion.identity;
            SkillVFXUtil.SpawnVFX(data.hitVFX, at, rotation, data.vfxLifetime);
        }
        if (data.hitSFX != null)
        {
            SkillVFXUtil.PlaySFX(data.hitSFX, at, data.sfxVolume);
        }
    } 

    public static IEnumerator Executor(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        PlayCastFX(caster, data);

        switch (data.skillId)
        {
            case "SK001": yield return Haste(caster, data, R); break;
            case "SK002": yield return HealAOE(caster, data, R); break;
            case "SK003": yield return PowerSlash(caster, data, R); break;
            case "SK004": yield return BurstMining(caster, data, R); break;
            case "SK005": yield return DelicateTouch(caster, data, R); break;
            case "SK006": yield return EscapeRope(caster, data, R); break;
            case "SK007": yield return HolyBind(caster, data, R); break;
            case "SK008": yield return Decoy(caster, data, R); break;
            case "SK009": yield return Flash(caster, data, R); break;
            default:Debug.LogWarning($"알 수 없는 스킬 ID: {data.skillId}"); break;
        }
    }

    // 스킬1: 헤이스트
    private static IEnumerator Haste(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} Lv{R.level} 시전");

        // 레벨별 수치 로드
        float moveMul = R.value;
        float atkSpeedMul = R.value;

        var players = Physics2D.OverlapCircleAll(caster.transform.position, data.radius, LayerMask.GetMask("Player"));
        foreach (var p in players)
        {
            var buff = p.GetComponent<BuffSystem>();
            if (buff != null)
            {
                buff.ApplyTimedMultiplierEx(
                   key: string.IsNullOrEmpty(data.skillId) ? "SK001" : data.skillId,
                   duration: Mathf.Max(0.1f, R.duration),
                   moveMul: moveMul,
                   atkSpeedMul: atkSpeedMul,
                   miningSpeedMul: 1f,
                   addDoubleChance: 0f,
                   slowMul: 1f,
                   setStun: false,
                   loopVFXPrefab: data.buffVFX,        // SO에 지정한 지속 오라/루프 이펙트
                   vfxOffset: data.buffOffset,
                   vfxFollow: data.buffFollow,
                   vfxFallbackLifetime: data.vfxLifetime
               );
            }
        }
        yield break;
    }

    // 스킬2: 힐
    private static IEnumerator HealAOE(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} Lv{R.level} 시전");
        float totalHeal = R.value;
        float dur = Mathf.Max(1f, R.duration);

        var players = Physics2D.OverlapCircleAll(caster.transform.position, R.radius, LayerMask.GetMask("Player"));
        foreach (var p in players) 
        {
            var ps = p.GetComponent<PlayerSkillSystem>();
            if (ps != null)
            {
                ps.HealOverTime(totalHeal, dur); // 힐 적용

                ps.ApplyTimedBuff(
                key: string.IsNullOrEmpty(data.skillId) ? "SK002" : data.skillId,
                duration: dur,
                moveMul: 1f,
                atkSpeedMul: 1f,
                miningSpeedMul: 1f,
                addDoubleChance: 0f,
                slowMul: 1f,
                setStun: false,
                loopVFXPrefab: data.buffVFX,
                vfxOffset: data.buffOffset,
                vfxFollow: data.buffFollow,
                vfxFallbackLifetime: data.vfxLifetime
            );
            }
        }
        yield break;
    }

    // 스킬3: 파워 슬래시
    private static IEnumerator PowerSlash(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        // 전방 박스 판정
        Vector2 dir = caster.GetComponent<MouseAim>()?.Direction ?? Vector2.right;
        Vector2 center = (Vector2) caster.transform.position + dir.normalized * Mathf.Max(0.5f, R.range);
        Vector2 size = new Vector2(Mathf.Max(1.6f, R.range * 1.2f), 1.2f);
        float angle = Vector2.SignedAngle(Vector2.right, dir);

        var hits = Physics2D.OverlapBoxAll(center, size, angle, LayerMask.GetMask("Enemy"));
        float atk = caster.AttackPower * R.attackMultiplier + R.damageFlat;

        foreach (var hit in hits)
        {
            hit.GetComponent<Monster>()?.GotHit(atk);
            hit.GetComponent<Boss>()?.GotHit(atk);

            PlayHitFX(hit.transform.position, data, dir);
        }
        yield break;
    }

    // 스킬4: 버스트 마이닝
    private static IEnumerator BurstMining(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        float miningMul = (1f + R.value); // 50% -> 1.5배

        caster.ApplyTimedBuff(
           key: string.IsNullOrEmpty(data.skillId) ? "SK004" : data.skillId,
           duration: Mathf.Max(0.1f, R.duration),
           moveMul: 1f,
           atkSpeedMul: 1f,
           miningSpeedMul: miningMul,
           addDoubleChance: 0f,
           slowMul: 1f,
           setStun: false,
           loopVFXPrefab: data.buffVFX,   // 원하면 사용(없으면 null이어도 OK)
           vfxOffset: data.buffOffset,
           vfxFollow: data.buffFollow,
           vfxFallbackLifetime: data.vfxLifetime
        );
        yield break;
    }

    // 스킬5: 섬세한 손길
    private static IEnumerator DelicateTouch(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        float addChance = R.value;

        caster.ApplyTimedBuff(
            key: string.IsNullOrEmpty(data.skillId) ? "SK005" : data.skillId,
            duration: Mathf.Max(0.1f, R.duration),
            moveMul: 1f,
            atkSpeedMul: 1f,
            miningSpeedMul: 1f,
            addDoubleChance: addChance,
            slowMul: 1f,
            setStun: false,
            loopVFXPrefab: data.buffVFX,   // 원하면 사용(없으면 null)
            vfxOffset: data.buffOffset,
            vfxFollow: data.buffFollow,
            vfxFallbackLifetime: data.vfxLifetime
        );
        yield break;
    }

    // 스킬6: 탈출용 로프
    private static IEnumerator EscapeRope(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        Transform t = caster.transform;
        // 마우스 방향 혹은 오른쪽
        Vector2 dir = caster.GetComponent<MouseAim>()?.Direction ?? Vector2.right;
        float distance = Mathf.Max(0f, R.range);
        Vector2 target = (Vector2)t.position + dir.normalized * distance;

        // ---------------------------------------------------------
        // [시각화 1] 로프(LineRenderer) 생성 및 설정
        // ---------------------------------------------------------
        GameObject ropeObj = new GameObject("RopeVisual");
        LineRenderer line = ropeObj.AddComponent<LineRenderer>();

        // 로프 그래픽 설정 (취향에 맞게 조절 가능)
        line.startWidth = 0.1f; // 시작 두께
        line.endWidth = 0.1f;   // 끝 두께
        line.positionCount = 2; // 점은 2개 (시작점, 끝점)
        line.material = new Material(Shader.Find("Sprites/Default")); // 기본 스프라이트 재질 (분홍색 방지)
        line.startColor = new Color(0.6f, 0.3f, 0f, 1f); // 갈색 (로프 색상)
        line.endColor = new Color(0.6f, 0.3f, 0f, 1f);
        line.sortingOrder = 10; // 플레이어보다 앞에 보이게 설정
                                // ---------------------------------------------------------

        // 이동 중에는 충돌 무시
        int originLayer = t.gameObject.layer;
        t.gameObject.layer = LayerMask.NameToLayer("GhostPlayer");

        float time = distance / 20f;
        float el = 0f;
        Vector3 start = t.position;

        while (el < time)
        {
            el += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, el / time);

            // -----------------------------------------------------
            // [시각화 2] 로프 위치 업데이트
            // -----------------------------------------------------
            if (line != null)
            {
                // 로프의 한쪽 끝은 목표 지점에 고정
                line.SetPosition(0, target);
                // 다른 한쪽 끝은 플레이어를 따라옴
                line.SetPosition(1, t.position);
            }
            // -----------------------------------------------------

            yield return null;
        }

        t.position = target;
        t.gameObject.layer = originLayer;

        // ---------------------------------------------------------
        // [시각화 3] 로프 삭제
        // ---------------------------------------------------------
        if (ropeObj != null)
        {
            GameObject.Destroy(ropeObj);
        }
    }

    // 스킬7: 신성 구속
    private static IEnumerator HolyBind(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        Vector2 dir = caster.GetComponent<MouseAim>()?.Direction ?? Vector2.right;
        Vector2 center = (Vector2)caster.transform.position + dir.normalized * Mathf.Max(1.5f, R.range * 0.6f);
        Vector2 size = new Vector2(Mathf.Max(2.0f, R.range * 1.6f), 1.6f);
        float angle = Vector2.SignedAngle(Vector2.right, dir);

        var hits = Physics2D.OverlapBoxAll(center, size, angle, LayerMask.GetMask("Enemy"));
        float dmg = caster.AttackPower * R.attackMultiplier + data.damageFlat; // 0.2배

        foreach (var hit in hits)
        {
            hit.GetComponent<Monster>()?.GotHit(dmg);
            hit.GetComponent<Boss>()?.GotHit(dmg);

            // 몬스터에 붙어있는 버프 시스템 컴포넌트
            var mobBuff = hit.GetComponent<BuffSystem>();
            if (mobBuff != null)
            {
                float slowMul = Mathf.Clamp01(1f - data.moveSpeedPercent); // 0.25 = -75%
                mobBuff.ApplyTimedMultiplierEx(
                key: string.IsNullOrEmpty(data.skillId) ? "SK007" : data.skillId,
                duration: Mathf.Max(0.1f, R.duration),
                moveMul: 1f,
                atkSpeedMul: 1f,
                miningSpeedMul: 1f,
                addDoubleChance: 0f,
                slowMul: slowMul,
                setStun: false,
                loopVFXPrefab: data.buffVFX,     // 디버프 루프 아이콘/오라 프리팹
                vfxOffset: data.buffOffset,      // 필요시 머리 위로 (예: 0,0.5f) 세팅
                vfxFollow: data.buffFollow,
                vfxFallbackLifetime: data.vfxLifetime
            );
            }

            PlayHitFX(hit.transform.position, data, dir);
        }
        yield break;
    }

    // 스킬8: 도발 인형
    private static IEnumerator Decoy(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        if (!data.installPrefab)
        {
            Debug.LogWarning("디코이 프리팹 미지정");
            yield break;
        }

        Vector2 dir = caster.GetComponent<MouseAim>()?.Direction ?? Vector2.right;
        Vector2 pos = (Vector2)caster.transform.position + dir.normalized * Mathf.Max(0.5f, R.range);
        var obj = Object.Instantiate(data.installPrefab, pos, Quaternion.identity);
        var decoy = obj.GetComponent<Decoy>();
        if (decoy != null)
        {
            float playerHP = caster.GetComponent<PlayerStatus>()?.MaxHP ?? 100f;
            decoy.Init(maxHits: 3, hp: playerHP * 0.7f, lifeTime: data.duration);
        }
        yield break;
    }

    // 스킬9: 금빛섬광
    private static IEnumerator Flash(PlayerSkillSystem caster, SkillData data, SkillResolved R)
    {
        Debug.Log($"{data.skillName} lv{R.level} 시전");

        var hits = Physics2D.OverlapCircleAll(caster.transform.position, data.radius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var mobBuff = hit.GetComponent<BuffSystem>();
            if (mobBuff)
            {
                mobBuff.ApplyTimedMultiplierEx(
                key: string.IsNullOrEmpty(data.skillId) ? "SK009" : data.skillId,
                duration: Mathf.Max(0.1f, R.duration),
                moveMul: 1f,
                atkSpeedMul: 1f,
                miningSpeedMul: 1f,
                addDoubleChance: 0f,
                slowMul: 1f,
                setStun: true,                        // ★ 스턴
                loopVFXPrefab: data.buffVFX,      // 스턴 지속 동안 표시할 루프 VFX(선택)
                vfxOffset: data.buffOffset,       // 필요하면 적 머리 위로 조정
                vfxFollow: data.buffFollow,
                vfxFallbackLifetime: data.vfxLifetime
            );
            }
        }
        yield break;
    }
}
