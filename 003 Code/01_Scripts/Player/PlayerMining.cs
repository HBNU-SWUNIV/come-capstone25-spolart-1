using UnityEngine;

public class PlayerMining : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator animator;      // 공격 애니메이션 트리거
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private PlayerStatus status;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private BuffSystem buff;
    [SerializeField] private ActionLock actionLock;

    private InventoryUIManager inventoryUI;

    [Header("애니메이션 (채광속도 증가 시 애니메이션 속도 증가를 위함)")]
    [SerializeField] private float baseMiningAnimLength = 0.5f;
    private float _animBaseSpeed = 1f;

    [Header("SFX")]
    [SerializeField] private AudioClip swingClip;        // 휘두르는 소리
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private Vector2 swingPitch = new Vector2(0.95f, 1.05f);

    [Header("레이캐스트")]
    [SerializeField] private float miningRange;
    [SerializeField] private LayerMask miningMask;

    [SerializeField] private float miningTimer;

    private void Awake()
    {
        if (!actionLock) actionLock = GetComponent<ActionLock>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        if (!status) status = GetComponent<PlayerStatus>();
        if (animator != null) _animBaseSpeed = animator.speed;
        if (!equipment) equipment = GetComponent<PlayerEquipment>();

        inventoryUI = FindFirstObjectByType<InventoryUIManager>();
    }

    private void Update()
    {
        if (miningTimer > 0f)
        { 
            miningTimer -= Time.deltaTime; 
        }
    }

    private bool IsUIBlocking()
    {
        if (TownUIManager.Instance != null && TownUIManager.Instance.IsBusy)
            return true;

        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUIManager>();

        if (inventoryUI != null && inventoryUI.IsOpen)
            return true;

        return false;
    }

    public void OnMining()
    {
        if (IsUIBlocking()) return;

        if (actionLock != null && actionLock.IsLocked) return;

        if (miningTimer > 0f) return;

        // 장비 참조 없이 PlayerStatus의 최종 쿨다운만 사용
        float cd = status ? status.FinalMiningCooldown : 0.5f;
        miningTimer = cd;

        // 애니 속도는 버프의 채광 속도 배수만 반영
        if (animator)
        {
            float speedMul = buff ? buff.MiningSpeedMul : 1f;
            animator.speed = _animBaseSpeed * speedMul;

            // reason = "mining"으로 잠금
            actionLock?.Lock("mining");
            animator.SetTrigger("IsMining");

            // 애니 속도에 맞춰 원래 속도로 복귀
            float effectiveAnim = Mathf.Max(0.01f, baseMiningAnimLength / Mathf.Max(0.01f, speedMul));
            StartCoroutine(ResetAnimatorSpeedAfter(effectiveAnim));
        }
    }

    public void MiningCheck()
    {
        Transform origin = MouseAim.Instance.GetRayOrigin();
        Vector2 dir = MouseAim.Instance.GetAimDirection();

        int dmg = status ? status.FinalMiningPower : 1;
        float critChance = status ? status.FinalMiningCritChance : 0f;

        bool isCrit = Random.value < critChance;
        if (isCrit) dmg = Mathf.RoundToInt(dmg * 1.5f);

        RaycastHit2D hit = Physics2D.Raycast(origin.position, dir, miningRange, miningMask);
        if (hit.collider == null)
        {
            // 빗나감: 애니메이션만 재생되고 종료
            return;
        }

        var mineable = hit.collider.GetComponent<Mineable>();
        if (mineable == null)
        {
            // 광물 아님: 애니메이션만 재생되고 종료
            return;
        }

        // 4) 광물일 경우 채광 시도
        if (mineable.TryMine(dmg, playerInventory, out var ore, out var count))
        {
            

            if (count > 0 && ore != null)
            {
                GameManager.Instance?.AddOreToRun(ore, count);
            }
        }

        // 채광 성공 시 내구도 감소
        equipment.UseMiningTool();
    }

    public void PlaySwingSfx()
    {
        if (!sfxSource || !swingClip)
            return;

        float p = Random.Range(swingPitch.x, swingPitch.y);
        float originalPitch = sfxSource.pitch;

        sfxSource.pitch = p;
        sfxSource.PlayOneShot(swingClip, sfxVolume);
        sfxSource.pitch = originalPitch;
    }

    // 애니메이션의 마지막 키프레임에 이벤트 등록
    public void OnMiningEnd()
    {
        actionLock?.Unlock("mining");
    }

    private System.Collections.IEnumerator ResetAnimatorSpeedAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (animator != null) animator.speed = _animBaseSpeed;
    }
}
