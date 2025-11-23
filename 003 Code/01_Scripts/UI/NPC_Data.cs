using UnityEngine;

public class NPC_Data : MonoBehaviour
{
    // NPC     : 1~100 -- 대장장이 = 0, 사제장 = 1, 무역길드장 = 2, 기사단원 = 3,
    // 오브젝트 : 100~ -- 광산카트 = 100 , 우물 = 101 ,
    public int npcId;
    public string npcName = "대장장이";
    public Sprite npcSprite ;
    public string defaultScript; // 상호작용 시 기본적으로 보여줄 대사 스크립트
}
