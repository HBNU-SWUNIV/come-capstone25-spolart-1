using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour, IRebindOnSceneChange
{
    [Header("상태바 참조")]
    [SerializeField] private StatBar hpBar;
    [SerializeField] private StatBar mpBar;

    [Header("플레이어 탐색 및 연결")]
    [SerializeField] private PlayerStatus player;
    [SerializeField] private bool findByTag = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("씬 전환 시 자동으로 재바인딩할지 여부")]
    [SerializeField] private bool autoRebindOnSceneChange = true;

    [Header("장비/아이콘")]
    [SerializeField] private PlayerEquipment equip;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image miningtoolIcon;

    [Header("스킬 슬롯 연결")]
    [SerializeField] private SkillSlotHUD skillSlot1;
    [SerializeField] private SkillSlotHUD skillSlot2;

    private bool _skillsAppliedFromSave = false;

    // 현재 바인딩된 대상 캐시(중복 구독 방지용)
    private PlayerStatus _boundPlayer;
    private PlayerEquipment _boundEquip;

    private void Awake()
    {
        // 최초 시도
        TryInitialBind();

        // 아이콘 초기 상태 정리
        SafeSetIcon(weaponIcon, null);
        SafeSetIcon(miningtoolIcon, null);

        if (autoRebindOnSceneChange)
            SceneManager.sceneLoaded += OnSceneLoadedRebind;
    }

    private void OnDestroy()
    {
        if (autoRebindOnSceneChange)
            SceneManager.sceneLoaded -= OnSceneLoadedRebind;

        // 안전 해제
        if (_boundEquip != null)
        {
            _boundEquip.OnEquipmentChanged -= Refresh;
            _boundEquip = null;
        }
        if (_boundPlayer != null)
        {
            _boundPlayer.OnHPChanged -= OnHPChanged;
            _boundPlayer.OnMPChanged -= OnMPChanged;
            _boundPlayer = null;
        }
    }

    private void OnEnable()
    {
        RebindIfNeeded();
        Refresh();
    }

    private void OnDisable()
    {
        if (_boundEquip != null)
            _boundEquip.OnEquipmentChanged -= Refresh;

        if (_boundPlayer != null)
        {
            _boundPlayer.OnHPChanged -= OnHPChanged;
            _boundPlayer.OnMPChanged -= OnMPChanged;
        }
    }

    private void Update()
    {
        // 아직 못 찾았으면 가볍게 재시도
        if ((_boundPlayer == null || _boundEquip == null) && findByTag)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) BindTo(go);
        }
    }

    public void RebindSceneRefs()
    {
        RebindIfNeeded();
        Refresh();
    }

    // --------- 씬 전환 대응 ---------
    private void OnSceneLoadedRebind(Scene s, LoadSceneMode m)
    {
        RebindIfNeeded();
        Refresh();
    }

    // --------- 초기 바인딩 ---------
    private void TryInitialBind()
    {
        if (player == null && findByTag)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) BindTo(go);
        }
        else if (player != null)
        {
            BindTo(player.gameObject);
        }
    }

    // --------- 바인딩 유틸 ---------
    private void RebindIfNeeded()
    {
        // 인스펙터 연결 우선
        if (player != null && player != _boundPlayer)
        {
            BindTo(player.gameObject);
            return;
        }

        if (equip != null && equip != _boundEquip)
        {
            BindTo(equip.gameObject);
            return;
        }

        // 없으면 태그 탐색
        if (_boundPlayer == null && findByTag)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) BindTo(go);
        }
    }

    private void BindTo(GameObject go)
    {
        if (!go) return;

        // 기존 구독 해제
        if (_boundEquip != null)
            _boundEquip.OnEquipmentChanged -= Refresh;
        if (_boundPlayer != null)
        {
            _boundPlayer.OnHPChanged -= OnHPChanged;
            _boundPlayer.OnMPChanged -= OnMPChanged;
        }

        // 신규 참조(자식까지 탐색해서 더 안정적)
        player = go.GetComponent<PlayerStatus>() ?? go.GetComponentInChildren<PlayerStatus>(true);
        equip = go.GetComponent<PlayerEquipment>() ?? go.GetComponentInChildren<PlayerEquipment>(true);

        _boundPlayer = player;
        _boundEquip = equip;

        // 초기 값 반영 + 이벤트 구독
        if (_boundPlayer != null)
        {
            hpBar?.SetStatBar(_boundPlayer.CurrentHP, _boundPlayer.MaxHP);
            mpBar?.SetStatBar(_boundPlayer.CurrentMP, _boundPlayer.MaxMP);
            _boundPlayer.OnHPChanged += OnHPChanged;
            _boundPlayer.OnMPChanged += OnMPChanged;
        }

        if (_boundEquip != null)
        {
            _boundEquip.OnEquipmentChanged += Refresh;
        }

        BindSkillSlots(go);

        Refresh();
        Debug.Log("[PlayerHUD] Player/Equipment/Skill 바인딩 완료");
    }

    public void Unbind()
    {
        if (_boundEquip != null)
        {
            _boundEquip.OnEquipmentChanged -= Refresh;
            _boundEquip = null;
        }
        if (_boundPlayer != null)
        {
            _boundPlayer.OnHPChanged -= OnHPChanged;
            _boundPlayer.OnMPChanged -= OnMPChanged;
            _boundPlayer = null;
        }
        player = null;
        equip = null;

        // UI도 비움
        SafeSetIcon(weaponIcon, null);
        SafeSetIcon(miningtoolIcon, null);
    }

    // PlayerHUD 인스펙터에 skillSlot1, skillSlot2 연결
    private void BindSkillSlots(GameObject playerGO)
    {
        var pss = playerGO.GetComponent<PlayerSkillSystem>();
        var status = playerGO.GetComponent<PlayerStatus>();

        skillSlot1?.Bind(pss, status, 1);
        skillSlot2?.Bind(pss, status, 2);
    }

    // --------- UI 갱신 ---------
    private void Refresh()
    {
        if (!_skillsAppliedFromSave)
        {
            var D = DataManager.Instance;
            var pss = _boundPlayer ? _boundPlayer.GetComponent<PlayerSkillSystem>() : null;

            if (D != null && pss != null)
            {
                // 저장된 슬롯1/슬롯2를 플레이어에 주입
                D.ApplySkillsTo(pss);

                // 슬롯 HUD가 즉시 새 아이콘을 잡도록 재바인딩(또는 초기화 트리거)
                BindSkillSlots(pss.gameObject);

                _skillsAppliedFromSave = true;
                Debug.Log("[PlayerHUD] 저장된 스킬 슬롯 복구 완료");
            }
        }

        // PlayerEquipment -> EquipmentItem -> Icon
        var weapon = _boundEquip != null ? _boundEquip.CurrentWeapon : null;
        var mining = _boundEquip != null ? _boundEquip.CurrentMiningTool : null;

        SafeSetIcon(weaponIcon, weapon?.Icon);
        SafeSetIcon(miningtoolIcon, mining?.Icon);
    }

    private static void SafeSetIcon(Image img, Sprite sp)
    {
        if (!img) return;
        img.sprite = sp;
        img.enabled = (sp != null);
    }

    private void OnHPChanged(float current, float max)
    {
        hpBar?.SetSmooth(current, max);
    }

    private void OnMPChanged(float current, float max)
    {
        mpBar?.SetSmooth(current, max);
    }
}
