using UnityEngine;

// 게임이 시작될 때 SO 원본 에셋을 복사해서 런타임 전용 인스턴스를 만드는 클래스
public class InventoryStateHandle : MonoBehaviour
{
    // 인벤토리 상태 SO 할당
    [SerializeField] private InventoryState stateAsset;

    public static InventoryState Runtime { get; private set; }

    private void Awake()
    {
        if (Runtime == null)
        {
            // 원본 에셋 복제
            Runtime = Instantiate(stateAsset);
            Runtime.name = stateAsset.name + "_Runtime";

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
