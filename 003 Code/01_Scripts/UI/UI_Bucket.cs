using UnityEngine;
using UnityEngine.UI;

public class UI_Bucket : MonoBehaviour
{
    // 씬의 버튼이 클릭되면 이 함수가 호출됩니다.
    public void Send()
    {
        TownUIManager.Instance.End_SendOre();
        GameManager.Instance?.ConvertOresInRunToMoney();

        // 튜토리얼 QT-005에서만 플래그 발동
        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-008", "USE_BUCKET");
        }
    }
}