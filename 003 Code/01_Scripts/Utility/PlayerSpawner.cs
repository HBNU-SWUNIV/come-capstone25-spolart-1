using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private void Awake()
    {
        FindPlayer();
        player.transform.position = this.transform.position;
    }

    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
}
