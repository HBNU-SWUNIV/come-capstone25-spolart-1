using UnityEngine;

public enum QuestKind { Tutorial, Repeatable }
public enum QuestGoalType { Kill, Collect, None }

[CreateAssetMenu(fileName = "QuestData", menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("ID/분류")]
    public string questId;
    public QuestKind kind = QuestKind.Repeatable;

    [Header("표시")]
    public string title;
    [TextArea] public string description;

    [Header("NPC")]
    public string assignNpcId;      // 퀘스트를 수주할 NPC ID
    public string completeNpcId;    // 퀘스트를 완수할 NPC ID

    [Header("목표")]
    public QuestGoalType goalType;
    public string targetId;
    public int targetCount = 1;

    [Header("보상")]
    // 추후 퀘스트로 강화 재료 등 보상
    public int rewardMoney = 0;

    [Header("튜토리얼 퀘스트 순서")]
    public int tutorialStepOrder = 0; // QuestKind Tutorial일때만 유효
}
