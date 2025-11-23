using TMPro;
using UnityEngine;

public class UI_MineCart : MonoBehaviour
{
    public TMP_Text floorNumberText ;
    private int floorNumber = 1;

    public int maxFloor=5;

    [Header("던전 씬 이름")]
    [SerializeField] private string tutorial = "TutorialDungeonScene";
    [SerializeField] private string random = "DungeonGenerationScene";

    // 튜토리얼에서 사용할 QT-008 ID
    private const string TutorialBasketQuestId = "QT-008";

    #region 버튼
    public void NumberUp()
    {
        if (floorNumber == maxFloor){
            // 경고 보내고 
            return;
        }
        floorNumber++;
        floorNumberText.text = floorNumber+"";
    }
    public void NumberDown()
    {
        if (floorNumber == 1){
            // 경고 보내고 
            return;
        }
        floorNumber--;
        floorNumberText.text = floorNumber+"";
    }

    public void StartExpedition()
    {
        TownUIManager.Instance.End_ChooseFloor();
        PlayerPrefs.SetInt("StartFloor", floorNumber);

        // 튜토리얼 QT-005에서만 플래그 발동
        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-005", "ENTER_DUNGEON");
        }

        // 플레이어의 npcInRange를 초기화 해야함
        GameManager.Instance.EnterDungeon();
    }
    #endregion
}
