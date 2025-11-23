using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 1회성 버프 구매/적용/정리를 담당.
/// - 마을: Buy(buffId)
/// - 던전 입장: ApplyAllTo(player)
/// - 던전 퇴장: ClearAfterRun()
/// - 중복 구매 방지
/// - 최대 활성 버프 개수 제한 : 2개
/// </summary>
/// 
public class DungeonRunBuffManager : MonoBehaviour
{
    public static DungeonRunBuffManager Instance { get; private set; }

    [Header("버프 카탈로그(Shop에 노출할 목록)")]
    [SerializeField] private List<BuffData> catalog = new();

    [Header("제한 설정")]
    [SerializeField] private int maxActiveSlots = 2; // 한 번에 가질 수 있는 버프 수

    // 다음 입장 한 번에 적용될 장바구니
    private readonly List<string> _basket = new();

    // 외부 바스켓 조회용    
    public IReadOnlyList<string> BasketIds => _basket;

    private EconomyService _eco;

    public IReadOnlyList<BuffData> Catalog => catalog;
    public int BasketCount => _basket.Count;
    public int MaxActiveSlots => maxActiveSlots;
    public bool IsInBasket(string buffId) => _basket.Contains(buffId);

    // 장바구니 갱신
    public event Action OnBasketChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _eco = EconomyService.Instance;
    }

    // DataManager에서 바스켓 로드 (Start에서 수행해 로드 완료 이후 동기화)
    private void Start()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            var loaded = dm.LoadRunBuffBasket();
            if (loaded != null)
            {
                _basket.Clear();

                // 유효한 버프만, 최대 슬롯만큼, 중복 없이 복구
                foreach (var id in loaded)
                {
                    if (_basket.Count >= maxActiveSlots) break;
                    if (string.IsNullOrEmpty(id)) continue;
                    if (_basket.Contains(id)) continue;
                    if (catalog.Exists(b => b != null && b.buffId == id))
                        _basket.Add(id);
                }

                OnBasketChanged?.Invoke();
            }
        }
    }

    private BuffData FindBuff(string buffId)
    {
        if (string.IsNullOrEmpty(buffId)) return null;
        return catalog.Find(b => b != null && b.buffId == buffId);
    }

    // 구매 가능 여부 사전 검증
    public bool CanBuy(string buffId, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(buffId)) { reason = "유효하지 않은 버프입니다."; return false; }
        if (_basket.Contains(buffId)) { reason = "이미 선택한 버프입니다."; return false; }
        if (_basket.Count >= maxActiveSlots) { reason = $"버프는 최대 {maxActiveSlots}개까지 활성화할 수 있습니다."; return false; }
        return true;
    }

    // 실제 바스켓에 추가
    public bool Buy(string buffId)
    {
        if (!CanBuy(buffId, out _)) return false;

        _basket.Add(buffId);
        OnBasketChanged?.Invoke();
        PersistBasket();
        return true;
    }

    public bool Remove(string buffId)
    {
        if (string.IsNullOrEmpty(buffId)) return false;
        if (!_basket.Contains(buffId)) return false;

        _basket.Remove(buffId);

        // 환불 처리
        var data = FindBuff(buffId);
        if (data != null && data.price > 0 && _eco != null)
        {
            _eco.AddMoney(data.price);
            Debug.Log($"[DungeonRunBuffManager] 버프 '{data.buffName}' 해제됨 → {data.price:N0} Gold 환불");
        }

        OnBasketChanged?.Invoke();
        PersistBasket();
        return true;
    }

    // 던전 입장 시 플레이어에게 전부 적용
    public void ApplyAllTo(GameObject player)
    {
        if (player == null || _basket.Count == 0) return;

        var buff = player.GetComponent<BuffSystem>();
        if (buff == null) return;

        foreach (var id in _basket)
        {
            var data = catalog.Find(b => b.buffId == id);
            if (data != null && data.runScoped)
            {
                buff.ApplyRunBuff(data);
            }
        }
    }

    public void ClearBasket()
    {
        if (_basket.Count == 0) return;
        _basket.Clear();
        OnBasketChanged?.Invoke();
        PersistBasket();
    }

    // 던전에서 나오면 장바구니/적용 버프를 모두 폐기
    public void ClearAfterRun(GameObject player = null)
    {
        if (player != null)
        {
            var buff = player.GetComponent<BuffSystem>();
            buff?.ClearRunBuffs();
        }
        _basket.Clear();
        OnBasketChanged?.Invoke();
        PersistBasket();
    }

    // DataManager에 현재 바스켓 반영
    private void PersistBasket()
    {
        DataManager.Instance?.SaveRunBuffBasket(_basket);
    }
}
