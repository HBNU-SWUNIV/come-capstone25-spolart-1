using UnityEngine;

public class RunBuffShop : MonoBehaviour
{
    [SerializeField] private DungeonRunBuffManager runBuffs;

    private void Awake()
    {
        if (!runBuffs) runBuffs = DungeonRunBuffManager.Instance;
    }

    // UI Button에서 이 메서드에 buffId만 넘겨주면 구매됨
    public void BuyBuffById(string buffId)
    {
        if (runBuffs == null) return;
        bool ok = runBuffs.Buy(buffId);
        if (ok) Debug.Log($"구매 완료: {buffId}");
        else Debug.LogWarning($"구매 실패(중복 또는 잘못된 ID): {buffId}");
    }
}
