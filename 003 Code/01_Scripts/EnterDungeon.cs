using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterDungeon : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OnTriggerEnter µø¿€: " + collision.name);
        GameManager.Instance.EnterDungeon();
    }
}
