using Unity.VisualScripting;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance { get; private set; }
    public bool isCutScenePlaying = false;
    private GameObject player;
    void Start() {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }    
    public void StartCutScene(Collider2D playerCollider){
        isCutScenePlaying = true;
        player = playerCollider.gameObject;
        player.GetComponent<ActionLock>().Lock("cutscene");

        TownUIManager.Instance.StartConversation(GetComponent<NPC_Data>());
    }

    public void EndCutScene(){
        isCutScenePlaying = false;
        player.GetComponent<ActionLock>().Unlock("cutscene");
    }
}
