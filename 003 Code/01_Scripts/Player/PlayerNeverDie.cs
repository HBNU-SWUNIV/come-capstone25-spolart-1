using UnityEngine;

public class PlayerNeverDie : MonoBehaviour
{
    [SerializeField] private bool isInvincible = true;

    private bool _destroyed;

    private void Awake()
    {
        // DDOL 유지
        if (isInvincible) DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // GameManager가 존재하고, 상태가 Dead가 되는 순간 1회만 파괴
        var gm = GameManager.Instance;
        if (!_destroyed && gm != null && gm.State == GameManager.GameState.Dead)
        {
            _destroyed = true;
            Destroy(gameObject);
        }
    }
}