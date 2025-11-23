using System;
using System.Collections.Generic;
using UnityEngine;

public class NPC_DialogData : MonoBehaviour
{
    [Serializable]
    public class QuestDialogSet
    {
        public string[] offerLines;     // 퀘스트 수주 시 대사
        public string[] completeLines;  // 퀘스트 완수 시 대사
    }

    // <NPC ID, STRING>
    Dictionary<int, string[]> dialogData;

    // 퀘스트 전용 대사 데이터
    Dictionary<string, Dictionary<int, QuestDialogSet>> questDialogData;

    private void Awake()
    {

        dialogData = new Dictionary<int, string[]>();
        questDialogData = new Dictionary<string, Dictionary<int, QuestDialogSet>>();

        ReadyData();
        ReadyQuestDialogData();
    }
    
    public void ReadyData()
    {
        dialogData.Add(0, new string[] {"무슨 일 때문에 왔는가", "대장장이 2번째 대화문" , "3번째 대장장이"} );
        dialogData.Add(1, new string[] {"잘 왔네", "사제장 2번째 대화문" , "신전에서는 신을 모셔"} );
        dialogData.Add(2, new string[] {"돈 되는 얘기를 하자고", "무역길드장 2번째 대화문" , "돈 관련 건물이야"} );

        // 기사단원 스크립트
        dialogData.Add(3, new string[] { "나는 광산마을에 파견된 신성기사단의 단원이지.",
            "원래는 조용한 광산 마을에 불과했지만 이젠 아니야.",
            "광산으로 내려갈 땐 만반의 준비를 다 하도록."
        });

        dialogData.Add(200, new string[] { "크르르..", "감히 여기까지 내려오다니", "뭉게주마 !!!" });
    }

    private void ReadyQuestDialogData()
    {
        #region QT-001 "대장장이와 대화하기"
        questDialogData["QT-001"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주
            {
                3, new QuestDialogSet {
                    offerLines = new[] // 튜토리얼 시작(수주) 시 대사
                    {
                        "광산마을에 온 걸 환영한다.",
                        "나는 이곳에 파견된 신성기사단의 단원이다.",
                        "광산 깊은 곳에서 몬스터가 솟아나오기 시작한 뒤로, 이 마을은 예전과 전혀 다른 곳이 되었지.",
                        "처음 온 자네 같은 신입은, 무턱대고 광산으로 내려가기 전에 장비부터 챙겨야 한다.",
                        "우선 대장장이에게 가서 무기 상태부터 점검받도록 해. 마을 안쪽에 있는 작업장에 계시지."
                    },
                    completeLines = null
                }
            },
            // 대장장이(0) - 완수
            {
                0, new QuestDialogSet {
                    // offerLines는 없음
                    completeLines = new[]
                    {
                        "오, 신입인가? 기사단원이 보내서 왔다고?",
                        "광산에 내려갈 생각이라면, 제일 먼저 챙겨야 할 건 장비지.",
                        "자네 무기를 한 번 보여보게. 어디 보자… 상태가 영 좋지 않군.",
                        "마침 잘 왔어. 장비 관리하는 법부터 차근차근 알려주지."
                    }
                }
            }
        };
        #endregion

        #region QT-002 "장비 수리하기"
        // QT-002: 대장장이(0)
        questDialogData["QT-002"] = new Dictionary<int, QuestDialogSet>
        {
            // 대장장이(0) - 수주 & 완수
            {
                0, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "지금 자네 무기의 상태를 보게. 칼날이 다 나갔잖나.",
                        "장비는 튼튼해야 제 몫을 할 수 있지. 특히 이 마을처럼 위험한 곳에선 더더욱.",
                        "내게서 장비를 확인하고 장비를 선택해 수리 기능을 사용해 보게.",
                        "이번 수리는 연습 삼아 공짜로 해 주지. 대신 다음부터는 제대로 돈을 받을 거라구."
                    },
                    completeLines = new[]
                    {
                        "좋아, 이제야 무기답게 생겼군.",
                        "장비 내구도는 계속 소모되니, 광산에서 돌아올 때마다 꼭 점검하도록 하게.",
                        "언제든지 무기가 상태가 안 좋아 보이면 다시 찾아오게. 내 대장간은 항상 열려 있으니까."
                    }
                }
            }
        };
        #endregion

        #region QT-003 무역길드장과 대화하기
        questDialogData["QT-003"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주
            {
                3, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "무기도 수리했으니 이제 조금은 던전에 들어갈 준비가 된 셈이군.",
                        "하지만 싸움만 잘한다고 해서 이 마을에서 살아남는 건 아니야.",
                        "어떻게하면 돈을 더 벌 수 있는지 알려주는 사람이 따로 있지.",
                        "무역길드장을 찾아가 보도록 해. 돈 냄새를 기가 막히게 잘 맡는 사람이야.",
                        "내가 안내하도록 하지."
                    },
                    completeLines = null
                }
            },
            // 무역길드장(2) - 완수
            {
                2, new QuestDialogSet {
                    offerLines = null,
                    completeLines = new[]
                    {
                        "오, 기사단원이 말하던 신입이 자네인가?",
                        "나는 이 마을의 무역길드장이다. 광산에서 나오는 모든 것들은 결국 내 손을 거쳐 나가지.",
                        "광물을 잘 캐고, 잘 팔고, 그 돈으로 다시 더 깊은 곳을 노릴 수 있어야 진짜 모험가라고 할 수 있지.",
                        "자, 너무 긴장하지 말고. 앞으로 자네가 어떻게 벌어올지 기대하고 있다고."
                    }
                }
            }
        };
        #endregion

        #region QT-004 퀘스트 수주하기
        questDialogData["QT-004"] = new Dictionary<int, QuestDialogSet>
        {
            // 무역길드장(2) - 수주 & 완수
            {
                2, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "이 마을에선 ‘의뢰’라는 시스템을 통해 돈을 벌 수 있다네.",
                        "광산에서 몬스터를 처리해 달라, 특정 광물을 가져와 달라… 사람들의 요구는 끝이 없지.",
                        "하지만 아무에게나 의뢰를 맡길 순 없어서 말이야.",
                        "일단 자네가 얼마나 할 수 있는 사람인지 얘기부터 좀 나눠보도록 하지."
                    },
                    completeLines = new[]
                    {
                        "흠… 어느 정도 감은 잡았군. 하지만 아직 검증이 다 끝난 건 아니야.",
                        "진짜 의뢰를 맡기기 전에, 먼저 던전이 어떤 곳인지 몸으로 익혀 두는 게 좋겠지.",
                        "기사단원과 함께 던전에 들어가 기본적인 것부터 익히게. 그게 끝나면 의뢰를 수행할 수 있도록 해주지."
                    }
                }
            }
        };
        #endregion

        #region QT-005 던전 입장하기
        questDialogData["QT-005"] = new Dictionary<int, QuestDialogSet>
    {
        // 무역길드장(2) - 수주
        {
            2, new QuestDialogSet {
                offerLines = new[]
                {
                    "던전에 들어가려면, 입구 쪽에 있는 기사단원과 함께 움직이는 게 좋을 거야.",
                    "처음부터 깊은 곳까지 내려가려 들지 말고, 일단 구조와 분위기부터 익히도록 하게.",
                    "이번엔 튜토리얼이라고 생각하면 돼. 자네가 돌아오기만 한다면, 앞으로 의뢰를 열어 줄 수도 있겠지.",
                    "자, 준비가 됐다면 던전 입구로 가서 기사단원과 함께 내려가 보게."
                },
                completeLines = null
            }
        },
        // 기사단원(3) - 완수
        {
            3, new QuestDialogSet {
                offerLines = null,
                completeLines = new[]
                {
                    "왔군. 드디어 던전에 들어갈 준비가 된 건가?",
                    "여긴 생각보다 훨씬 위험한 곳이다. 방심하면 한순간에 끝장이니까 항상 주변을 살피도록 해.",
                    "우선은 기본적인 채광과 전투부터 익히게. 내가 옆에서 지켜보고 있으마.",
                    "겁먹을 필요는 없어. 자, 가보자고."
                }
            }
        }
    };
        #endregion

        #region QT-006 광물 채광하기
        questDialogData["QT-006"] = new Dictionary<int, QuestDialogSet>
        {
        // 기사단원(3) - 수주 & 완수
        {
            3, new QuestDialogSet {
                offerLines = new[]
                {
                    "광산에 내려왔으니, 이제 이곳의 주 수입원인 ‘광물’을 직접 캐볼 차례다.",
                    "앞에 보이는 광맥에 다가가 무기를 휘두르듯 채광을 시도해 봐.",
                    "마우스 우클릭을 누르면 곡괭이를 휘두를 수 있지.",
                    "너무 한 곳에만 서 있지 말고, 주변을 살피면서 캐는 게 좋다. 몬스터가 어디서 튀어나올지 모르거든.",
                    "철광석 한 개만이라도 캐서 가져와 보게. 그게 오늘의 첫 목표다."
                },
                completeLines = new[]
                {
                    "좋아, 제대로 캐왔군. 처음치고는 나쁘지 않은 솜씨야.",
                    "이런 광물들은 무역길드장에게 팔아서 돈으로 바꿀 수도 있고, 대장간에서 새로운 장비로 가공할 수도 있다.",
                    "이건 첫 기념이라고 생각하고 보너스다. 앞으로도 광맥을 잘 살피면서 움직이도록 해.",
                    "자네 주머니에 골드가 조금 들어갔으니, 그만큼 더 깊은 곳도 노려볼 수 있겠지."
                }
            }
        }
    };
        #endregion

        #region QT-007 박쥐 처치하기
        questDialogData["QT-007"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주 & 완수
            {
                3, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "광물만 있는 줄 알았지? 안타깝게도 이곳엔 몬스터도 함께 산다.",
                        "특히 박쥐 같은 녀석들은 소리 없이 다가와 우리의 뒤통수를 노리곤 하지.",
                        "근처에 날아다니는 박쥐를 한 마리만 처치해 보게. 공격을 피하면서 빈틈을 노리는 연습이라 생각해.",
                        "공격 타이밍과 이동을 함께 익혀두면, 나중에 더 강한 놈들을 상대할 때도 큰 도움이 될 거다."
                    },
                    completeLines = new[]
                    {
                        "좋아, 박쥐 정도는 거뜬히 상대할 수 있겠군.",
                        "몬스터와 싸울 땐 체력과 위치, 퇴로를 항상 염두에 둬야 한다.",
                        "넌 이제 최소한 ‘그냥 먹잇감’은 아니야. 그 정도 실력이면 더 깊이 들어가 볼 자격은 있다.",
                        "하지만 방심은 금물이다. 이곳은 한 번 잘못 발을 디디면 모든 걸 잃게 되는 곳이니까."
                    }
                }
            }
        };
        #endregion

        #region QT-008 광물바구니 사용하기
        questDialogData["QT-008"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주 & 완수
            {
                3, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "여기선 한 번 죽으면, 들고 있던 광물을 몽땅 잃어버릴 수도 있다.",
                        "그래서 준비된 게 바로 ‘광물바구니’다. 일정 지점마다 있는 바구니에 광물을 담아 올려보낼 수 있지.",
                        "바구니에 넣은 광물은 마을로 안전하게 보내지니, 위험해지기 전에 수시로 이용하는 습관을 들여라.",
                        "광물바구니에 광물을 넣고 마을로 돌아온 뒤, 나에게 다시 말을 걸어 보게."
                    },
                    completeLines = new[]
                    {
                        "잘 했다. 이제 광물을 잔뜩 캐고도, 죽기 전에만 바구니에 넣으면 손실을 줄일 수 있다는 걸 알았겠지.",
                        "겉으론 단순해 보이지만, 이런 작은 습관들이 결국 생사를 가른다.",
                        "앞으로는 욕심내서 버티지만 말고, 어느 정도 채웠다 싶으면 바구니를 먼저 찾도록 해.",
                        "물론 '깊은 곳'의 기운이 광물에 흡수되면 광물의 가치는 조금 더 올라가지.",
                        "이제 최소한 ‘손해만 보는 광부’는 아니게 되었군."
                    }
                }
            }
        };
        #endregion

        #region QT-009 사제장과 대화하기
        questDialogData["QT-009"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주
            {
                3, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "첫 던전은 잘 버텨냈군. 하지만 앞으로는 지금보다 훨씬 거친 녀석들을 보게 될 거야.",
                        "그 전에 준비해야 할 게 하나 더 있다. 바로 신성한 ‘축복’이지.",
                        "마을 신전의 사제장을 찾아가 보게. 일정 금액을 지불하면 각종 버프를 내려 줄 거다.",
                        "축복을 받고 나면, 몸이 훨씬 가벼워지고 힘이 넘치는 걸 느낄 수 있을 거야."
                    },
                    completeLines = null
                }
            },
            // 사제장(1) - 완수
            {
                1, new QuestDialogSet {
                    offerLines = null,
                    completeLines = new[]
                    {
                        "어서 오게, 방금 광산에서 돌아온 모험가로군.",
                        "이곳은 신전이자, 그대를 잠시나마 지켜주는 안식처와도 같은 곳이라네.",
                        "이 몸이 드리는 축복은 단순한 기도 이상이지. 광산 속에서 자네의 몸과 정신을 지탱해 줄 테니까.",
                        "어떤 축복이 필요한지 천천히 살펴보게. 대가를 치를 준비만 되어 있다면 말이야."
                    }
                }
            }
        };
        #endregion

        #region QT-010 버프 구매하기
        questDialogData["QT-010"] = new Dictionary<int, QuestDialogSet>
        {
            // 사제장(1) - 수주 & 완수
            {
                1, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "축복은 공짜가 아니네. 이 세계의 이치가 그렇듯, 신의 힘에도 나름의 ‘대가’가 필요하지.",
                        "가지고 있는 골드로 원하는 버프를 하나 선택해 보게.",
                        "공격력, 이동 속도, 채광 효율… 어떤 축복을 받느냐에 따라 자네의 플레이 방식도 달라질 거야.",
                        "한 번쯤은 직접 체험해 보는 편이 좋겠지?"
                    },
                    completeLines = new[]
                    {
                        "어떤가, 몸이 한결 가벼워진 느낌이 들지 않나?",
                        "축복은 영원하지 않지만, 그 한 번의 기회가 생사를 갈라놓을 수도 있다네.",
                        "광산으로 다시 떠나기 전에, 언제든 이곳에 들러 마음의 준비와 함께 강해질 수 있다.",
                        "자, 다음 단계로 나아갈 준비가 되었다면 더 이상 망설이지 말게."
                    }
                }
            }
        };
        #endregion

        #region QT-011 무역길드장과 대화하기
        questDialogData["QT-011"] = new Dictionary<int, QuestDialogSet>
        {
            // 기사단원(3) - 수주
            {
                3, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "이제 축복까지 받았다면, 마을 전체를 보는 눈도 좀 넓혀야겠지.",
                        "지금 마을을 둘러보면 제대로 돌아가지 않는 시설이 꽤 많을 거야.",
                        "그걸 다시 살려낼 수 있는 열쇠가 바로 무역길드장에게 있지.",
                        "주머니에 어느 정도 골드도 쌓였을 테니, 무역길드장을 찾아가 앞으로의 투자 방향을 논의해 보게."
                    },
                    completeLines = null
                }
            },
            // 무역길드장(2) - 완수
            {
                2, new QuestDialogSet {
                    offerLines = null,
                    completeLines = new[]
                    {
                        "다시 보니 아까와는 눈빛이 조금 달라졌군. 경험과 축복이 사람을 바꾸는 법이지.",
                        "이 마을의 시설들은 자네 같은 모험가들이 투자해 줄수록 더 강해진다네.",
                        "그만큼 더 깊은 층을 노릴 수 있고, 더 비싼 광물을 쉽게 캐올 수 있게 되겠지.",
                        "이제부터는 단순히 살아남는 것에서 그치지 말고, 어떻게 성장해 나갈지 고민해 보게."
                    }
                }
            }
        };
        #endregion

        #region QT-012 대장간 업그레이드하기
        questDialogData["QT-012"] = new Dictionary<int, QuestDialogSet>
        {
            // 무역길드장(2) - 수주 & 완수
            {
                2, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "장비가 좋으면 좋을수록, 광산에서 살아 돌아올 확률도 높아진다네.",
                        "우선 대장간부터 업그레이드해 보는 건 어떻겠나? 더 좋은 무기와 도구를 취급할 수 있게 될 거야.",
                        "시설 강화 메뉴에서 대장간을 선택하고, 2레벨까지 올려 보게.",
                        "투자는 언제나 부담스럽지만, 그만큼 확실한 보상도 따라오지."
                    },
                    completeLines = new[]
                    {
                        "오, 대장간이 한층 더 번듯해졌군. 장비 수준도 한 단계는 올라가겠어.",
                        "이제 광산에서 캐 온 광물들을 더 효율적으로, 더 강력한 장비로 바꿔 줄 수 있을 거다.",
                        "앞으로도 이렇게 차근차근 마을을 키워나가 봐. 그만큼 자네의 영향력도 커질 테니까.",
                        "이제 마지막 단계만 남았군. 진짜 ‘의뢰’를 맡을 준비가 되었는지 시험해 볼 시간이야."
                    }
                }
            }
        };
        #endregion

        #region QT-013 반복 퀘스트 수주하기
        questDialogData["QT-013"] = new Dictionary<int, QuestDialogSet>
        {
            // 무역길드장(2) - 수주 & 완수
            {
                2, new QuestDialogSet {
                    offerLines = new[]
                    {
                        "이제야 제대로 된 모험가를 상대하는 기분이 드는군.",
                        "앞으로는 언제든지 의뢰 게시판을 통해 반복 퀘스트를 받아 돈을 벌 수 있다네.",
                        "몬스터 처치, 광물 채집, 특정 목표 수행… 다양한 의뢰가 자네를 기다리고 있지.",
                        "한 번 게시판에서 의뢰를 골라 수주해 보게. 그게 자네의 ‘진짜 일거리’가 될 테니까."
                    },
                    completeLines = new[]
                    {
                        "좋아, 이제 자네는 이 마을의 정식 일원이라고 봐도 되겠군.",
                        "반복 퀘스트는 자네가 필요할 때마다 언제든 찾아올 수 있는 안정적인 수입원이다.",
                        "광산을 돌고, 의뢰를 완수하고, 다시 장비와 시설에 투자하면서 더 깊은 곳을 노려 보게.",
                        "그리고 잊지 말게. 이 모든 것의 시작은, 지금 이 마을과 이 광산이라는 걸 말이야."
                    }
                }
            }
        };
        #endregion
    }


    public string GetData(int npcId, int dialogIndex)
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;
        string[] questLines = null;

        if (qm == null || dm == null) { /* 퀘스트 매니저 없으면 기본 대사로 */ }
        else
        {
            // === 튜토리얼 퀘스트 우선 처리 ===
            var curTutorialDef = qm.GetTutorialByStep(dm.GetTutorialStep());
            string curQuestId = (curTutorialDef != null) ? curTutorialDef.questId : null;

            if (curTutorialDef != null)
            {
                // 1. [신규] 이 NPC가 현재 튜토리얼 퀘스트의 *완료* 대상인가?
                if (curTutorialDef.completeNpcId == npcId.ToString())
                {
                    // 퀘스트가 활성화(수락) 상태이고, 완료 준비가 되었는지 확인
                    if (qm.Active.TryGetValue(curQuestId, out var save))
                    {
                        bool ready = (curTutorialDef.goalType == QuestGoalType.None) || save.completed;
                        if (ready)
                        {
                            if (questDialogData.TryGetValue(curQuestId, out var npcMap) &&
                                npcMap.TryGetValue(npcId, out var dialogSet))
                            {
                                questLines = dialogSet.completeLines;
                            }
                        }
                    }
                }

                // 2. [기존] 이 NPC가 현재 튜토리얼 퀘스트의 *수주* 대상인가?
                // (완료 대사가 아닐 경우에만 체크)
                if (questLines == null && curTutorialDef.assignNpcId == npcId.ToString())
                {
                    if (questDialogData.TryGetValue(curQuestId, out var npcMap) &&
                        npcMap.TryGetValue(npcId, out var dialogSet))
                    {
                        questLines = dialogSet.offerLines;
                    }
                }
            }

            // === 튜토리얼이 아닐 경우, 일반/반복 퀘스트 처리 ===

            // 3. [순서 변경] 튜토리얼 외에 *완수* 가능한 퀘스트가 있는가?
            if (questLines == null && qm.HasTurnInAtNpc(npcId, out var completableQuest))
            {
                // (방금 확인한 튜토리얼 퀘스트가 아니어야 함)
                if (completableQuest.questId != curQuestId)
                {
                    if (questDialogData.TryGetValue(completableQuest.questId, out var npcMap) &&
                        npcMap.TryGetValue(npcId, out var dialogSet))
                    {
                        questLines = dialogSet.completeLines;
                    }
                }
            }

            // 4. [순서 변경] 튜토리얼 외에 *수주* 가능한 퀘스트가 있는가?
            if (questLines == null && qm.HasOfferAtNpc(npcId, out var offerableQuest))
            {
                // (방금 확인한 튜토리얼 퀘스트가 아니어야 함)
                if (offerableQuest.questId != curQuestId)
                {
                    if (questDialogData.TryGetValue(offerableQuest.questId, out var npcMap) &&
                       npcMap.TryGetValue(npcId, out var dialogSet))
                    {
                        questLines = dialogSet.offerLines;
                    }
                }
            }
        }

        // 6. 퀘스트 전용 대사가 결정된 경우 (기존과 동일)
        if (questLines != null)
        {
            if (dialogIndex >= questLines.Length)
            {
                return null; // 대화 종료
            }
            return questLines[dialogIndex];
        }

        // 7. 퀘스트 전용 대사가 없다면 기존 일반 대사 사용 (기존과 동일)
        if (!dialogData.TryGetValue(npcId, out var defaultLines) || defaultLines == null)
        {
            return null;
        }
        if (dialogIndex >= defaultLines.Length)
        {
            return null;
        }
        return defaultLines[dialogIndex];
    }
}