using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialPortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") ) return;
        // if (exitPoint == null || transition == null) return;
        
        SceneManager.LoadScene("TownScene");
    }
}
