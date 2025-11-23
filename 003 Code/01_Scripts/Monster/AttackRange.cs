using System.Collections.Generic;
using UnityEngine;

public class AttackRange : MonoBehaviour
{
    // �����Ŵ��� ���� ����
    [SerializeField] private DungeonManager dungeonManager;
    HashSet<Collider2D> InRange_players = new HashSet<Collider2D>();
    

    private void Awake()
    {
        dungeonManager = FindAnyObjectByType<DungeonManager>();
    }

    // �÷��̾ ���ݹ��� �ȿ� ������ �� ����
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            InRange_players.Add(collision);
            collision.GetComponent<PlayerParry>()?.SetParryAvailable(true);
            dungeonManager?.TriggerTutorialStep(TutorialType.Melee);
        }
    }


    // �÷��̾ ���ݹ��� ������ ����� �� ����
    private void OnTriggerExit2D(Collider2D collision)
    {
        
        if (collision.CompareTag("Player"))
        {
            InRange_players.Remove(collision);
            collision.GetComponent<PlayerParry>()?.SetParryAvailable(false);
        }
    }

    public void DoDamage(float damage)
    {
        foreach (var item in InRange_players)
        {
            if (item.isTrigger) item.GetComponent<PlayerStatus>().TakeDamage(damage);
        }
    }
}
