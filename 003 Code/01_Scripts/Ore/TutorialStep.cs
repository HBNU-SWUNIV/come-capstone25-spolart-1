using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    public TutorialType type;
    [TextArea(2, 5)] public string tutorialText;
    public Sprite npcSprite;
}

public enum TutorialType
{
    Melee,
    Ranged,
    Dash,
    AOE
}