using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TownUIManager : MonoBehaviour, IRebindOnSceneChange
{
    public static TownUIManager Instance { get; private set; }

    [Header("UI 참조")]
    public GameObject canvas;
    public TMP_Text dialogText;
    public TMP_Text npcNameText;
    public Image npcImage;

    private NPC_DialogData dialogData ;
    private NPC_Data npc_Data;
    private int talk_idx = 0; // NPC 대사 인덱스

    public Transform conversation;
    public Transform window_Options;
    public Transform[] options;
    public Transform[] special_Interacts;
    public Transform[] object_Interacts;

    [HideInInspector] public bool intalk = false;

    public System.Action<int> OnNpcTalkEnded;
    public System.Action<NPC_Data> OnNpcTalkStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start(){
        dialogData = GetComponent<NPC_DialogData>();
    }

    public void RebindSceneRefs()
    {
        // 필요 시, 새 씬의 패널/버튼/텍스트 레퍼런스를 동적 찾기
        // (지금 구조에선 대부분 자체 Canvas 세트이므로 비워도 OK)
    }

    public void Interact_InTalk()
    {
        if (dialogData.GetData(npc_Data.npcId, talk_idx) == null) 
        {
            if (npc_Data.npcId >= 200 && npc_Data.npcId < 300)
            {
                CutSceneManager.Instance?.EndCutScene();
                CloseAllTownUI();
            }
            Endtalk();
        }
        else 
        {
            dialogText.text = dialogData.GetData(npc_Data.npcId, talk_idx);
            talk_idx++;
        }
    }

    public void Endtalk()
    {
        // 대화가 끝나면 알림
        if (npc_Data != null)
        {
            OnNpcTalkEnded?.Invoke(npc_Data.npcId);
        }

        window_Options.gameObject.SetActive(true);

        // 대화 종료 시 NPC 기본 대사로 수정
        if (npc_Data != null)
        {
            dialogText.gameObject.SetActive(true);
            dialogText.text = npc_Data.defaultScript;
        }
        else
        {
            dialogText.gameObject.SetActive(false);
        }

        // canvas.SetActive(false); 
        intalk = false; 
        talk_idx = 0 ;
    }

    public void StartConversation(NPC_Data npcData)
    {
        canvas.SetActive(true);
        npc_Data = npcData;
        npcNameText.text = npcData.npcName;
        npcImage.sprite = npcData.npcSprite;

        var inv = FindObjectOfType<InventoryUIManager>();
        if (inv != null && inv.IsOpen)
        {
            Debug.Log("인벤토리가 열린 상태에서는 대화 시작 불가능");
            return;
        }

        // NPC별 기본 대사 출력
        dialogText.gameObject.SetActive(true);
        dialogText.text = npc_Data.defaultScript;

        window_Options.gameObject.SetActive(true);

        // NPC에 따라 다른 상호작용 버튼
        switch (npcData.npcId)
        {
            case 0 :
                // 대장장이 : 장비를 강화한다
                options[0].gameObject.SetActive(true);
                break;
            case 1 :
                // 사제장 : 스킬을 강화한다
                options[1].gameObject.SetActive(true);
                break;
            case 2 :
                // 무역 길드장 : 의뢰를 받는다, 마을 시설을 강화한다
                options[2].gameObject.SetActive(true);
                options[3].gameObject.SetActive(true);
                break;
            case 3 :
                // 기사단원
                //options[4].gameObject.SetActive(true);
                break;

            /// 오브젝트 상호작용
            case 100 :
                // 대화창 끄기
                conversation.gameObject.SetActive(false);
                // 장비 강화 창 켜기
                object_Interacts[0].gameObject.SetActive(true);  
                break;
            case 101 :
                // 대화창 끄기
                conversation.gameObject.SetActive(false);
                // 장비 강화 창 켜기
                object_Interacts[1].gameObject.SetActive(true); 
                break;
            case 200:
                Option_Talk();
                break;
        }

        OnNpcTalkStarted?.Invoke(npcData);
    }

    public void EndConversation()
    {
        if (npc_Data == null)
        {
            return;
        }

        switch (npc_Data.npcId)
        {
            case 0 :
            // 대장장이 : 장비를 강화한다
                options[0].gameObject.SetActive(false);
                break;
            case 1 :
            // 사제장 : 스킬을 강화한다
                options[1].gameObject.SetActive(false);
                break;
            case 2 :
            // 무역 길드장 : 의뢰를 받는다, 마을 시설을 강화한다
                options[2].gameObject.SetActive(false);
                options[3].gameObject.SetActive(false);
                break;
            case 3:
                // 소모품 상인 : 거래한다
                options[4].gameObject.SetActive(false);
                break;
        }
        npc_Data = null;
        canvas.SetActive(false);
    }

    public bool IsBusy
    {
        get
        {
            // 1. 메인 대화 캔버스 자체가 활성화되어 있다면 (대화 창, 옵션 창 포함)
            if (canvas != null && canvas.activeSelf) return true;

            // 2. 특수 상호작용 창(강화, 스킬, 퀘스트 등) 중 활성화된 것이 있는지 확인
            if (special_Interacts != null)
            {
                for (int i = 0; i < special_Interacts.Length; i++)
                {
                    if (special_Interacts[i] != null && special_Interacts[i].gameObject.activeSelf)
                    {
                        return true;
                    }
                }
            }

            // 3. 오브젝트 상호작용 창(층 선택, 광물 전송 등) 중 활성화된 것이 있는지 확인
            if (object_Interacts != null)
            {
                for (int i = 0; i < object_Interacts.Length; i++)
                {
                    if (object_Interacts[i] != null && object_Interacts[i].gameObject.activeSelf)
                    {
                        return true;
                    }
                }
            }

            // UI가 아무것도 활성화되지 않았다면 false 반환
            return false;
        }
    }

    public void Option_Talk()
    {
        // 선택지 끄기
        window_Options.gameObject.SetActive(false);
        // 대화문 켜기
        dialogText.gameObject.SetActive(true);

        intalk = true;
        dialogText.text = dialogData.GetData(npc_Data.npcId, talk_idx);
        talk_idx ++;
    }
    public void Option_Upgrade()
    {
        // 대화창 끄기
        conversation.gameObject.SetActive(false);
        // 장비 강화 창 켜기
        special_Interacts[0].gameObject.SetActive(true);  
    }

    public void End_Upgrade()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 장비 강화 창 끄기
        special_Interacts[0].gameObject.SetActive(false);  
        EndConversation();
    }

    public void Option_LevelUPSkill()
    {
        // 대화창 끄기
        conversation.gameObject.SetActive(false);
        // 스킬 레벨업 창 켜기
        special_Interacts[1].gameObject.SetActive(true);  
    }

    public void End_LevelUPSkill()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 스킬 레벨업 창 끄기
        special_Interacts[1].gameObject.SetActive(false);  
        EndConversation();
    }

    public void Option_Quest()
    {
        // 대화창 끄기
        conversation.gameObject.SetActive(false);
        // 의뢰 받기 창 켜기
        special_Interacts[2].gameObject.SetActive(true);  
    }

    public void End_Quest()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 의뢰 받기 창 끄기
        special_Interacts[2].gameObject.SetActive(false);
        EndConversation();
    }

    public void Option_UpgradeFacilities()
    {
        // 대화창 끄기
        conversation.gameObject.SetActive(false);
        // 시설 강화 창 켜기
        special_Interacts[3].gameObject.SetActive(true);  
    }
    public void End_UpgradeFacilities()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 시설 강화 창 끄기
        special_Interacts[3].gameObject.SetActive(false);  
        EndConversation();
    }

    public void End_ChooseFloor()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 시설 강화 창 끄기
        object_Interacts[0].gameObject.SetActive(false);  
        EndConversation();
    }

    public void End_SendOre()
    {
        // 대화창 켜기
        conversation.gameObject.SetActive(true);
        // 시설 강화 창 끄기
        object_Interacts[1].gameObject.SetActive(false);  
        EndConversation();
    }

    public void Option_Leave()
    {
        Debug.Log("떠난다 선택지 클릭");
        // 대화창 닫기
        EndConversation();
    }

    /// <summary>
    /// 플레이어가 범위를 벗어나는 등, 모든 상호작용 UI를 강제로 닫아야 할 때 호출됩니다.
    /// </summary>
    public void CloseAllTownUI()
    {
        // 1. 모든 특수 상호작용 창 닫기
        if (special_Interacts != null)
        {
            foreach (var interact in special_Interacts)
            {
                if (interact != null) interact.gameObject.SetActive(false);
            }
        }

        // 2. 모든 오브젝트 상호작용 창 닫기
        if (object_Interacts != null)
        {
            foreach (var interact in object_Interacts)
            {
                if (interact != null) interact.gameObject.SetActive(false);
            }
        }

        // 3. 메인 대화창의 선택지 버튼들 끄기 (선택 사항이지만, 초기화를 위해 유지)
        if (options != null)
        {
            foreach (var opt in options)
            {
                if (opt != null) opt.gameObject.SetActive(false);
            }
        }

        // 4. UI_Conversation (대화창 본체)를 다시 활성화합니다.
        if (conversation != null)
        {
            conversation.gameObject.SetActive(true);
        }

        // 5. 대화 상태 초기화 (intalk 플래그 등)
        intalk = false;
        talk_idx = 0;

        // 6. [수정됨] 모든 자식 오브젝트의 상태를 리셋한 후, 마지막에 루트 캔버스를 끕니다.
        if (canvas != null)
        {
            canvas.SetActive(false);
        }

        // 7. NPC 참조 초기화
        npc_Data = null;
    }
}
