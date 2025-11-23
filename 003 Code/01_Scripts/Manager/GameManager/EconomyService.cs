using System;
using UnityEngine;

public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    public event Action<long> OnMoneyChanged;

    [SerializeField] private int money;
    public int Money => money;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void AddMoney(int amount)
    {
        money += Mathf.Max(0, amount);
        DataManager.Instance?.SaveNow();
        OnMoneyChanged?.Invoke(money);
    }

    // 돈 소비 시 호출해 현재 런타임 정보 갱신
    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0 || money < amount)
        {
            return false;
        }

        money -= amount;
        DataManager.Instance?.SaveNow();
        OnMoneyChanged?.Invoke(money);
        Debug.Log($"골드 소모!: {amount}");
        return true;
    }

    public void SetMoney(int value)
    {
        money = Mathf.Max(0, value);
        OnMoneyChanged?.Invoke(money);
    }
}
