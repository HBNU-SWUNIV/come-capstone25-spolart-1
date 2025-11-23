using UnityEngine;

public class CutSceneTrigger : MonoBehaviour
{
    private void Awake(){
        if(!gameObject.activeSelf)gameObject.SetActive(true);
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player") ) return;

        if (other.isTrigger) {
            CutSceneManager.Instance.StartCutScene(other);
            gameObject.SetActive(false);
        }
    }
}
