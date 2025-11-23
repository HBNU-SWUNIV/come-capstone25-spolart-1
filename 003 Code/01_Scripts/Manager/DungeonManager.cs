using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 던전 내 튜토리얼, 몬스터, 광물 스폰 관리 등 기능을 수행할 스크립트

public class DungeonManager : MonoBehaviour
{
    [Header("튜토리얼 UI")]
    [SerializeField] private GameObject tutorialUI;
    [Tooltip("튜토리얼 NPC 이미지")]
    [SerializeField] private Image tutorialNPC;
    [Tooltip("튜토리얼 내용 텍스트")]
    [SerializeField] private TMP_Text tutorialText;

    [Header("튜토리얼 단계 설정")]
    [SerializeField] private List<TutorialStep> tutorialSteps;

    private HashSet<TutorialType> triggeredTutorials = new HashSet<TutorialType>();

    private int currentTutorialIndex = -1;
    private bool isTutorialActive = false;

    // 처음 몬스터 조우 이후 공격받는 시점에 게임 멈추고 튜토리얼 UI 활성화
    // 게임 정지는 게임 매니저에서 처리

    private void Update()
    {
        if (!isTutorialActive) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CloseTutorial();
        }
    }

    public void TriggerTutorialStep(TutorialType type)
    {
        // 이미 한 번 실행된 튜토리얼이라면 스킵
        if (isTutorialActive || triggeredTutorials.Contains(type)) return;

        // 중복 방지
        if (isTutorialActive) return;

        int index = tutorialSteps.FindIndex(t => t.type == type);
        if (index != -1)
        {
            currentTutorialIndex = index;
            triggeredTutorials.Add(type);
            ShowTutorial(tutorialSteps[index]);
        }
    }

    private void ShowTutorial(TutorialStep step)
    {
        isTutorialActive = true;

        tutorialNPC.sprite = step.npcSprite;
        tutorialText.text = step.tutorialText;

        tutorialUI.SetActive(true);
        GameManager.Instance.PauseGame();
    }

    private void CloseTutorial()
    {
        isTutorialActive = false;
        tutorialUI.SetActive(false);
        GameManager.Instance.ResumeGame();
    }
}
