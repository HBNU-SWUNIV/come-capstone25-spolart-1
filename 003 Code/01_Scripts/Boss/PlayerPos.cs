using UnityEngine;

public class PlayerPos : MonoBehaviour
{
    void Awake(){

        
        GameObject findPlayer = GameObject.FindGameObjectWithTag("Player");
        findPlayer.transform.position = this.transform.position;
    }
}
