# 한밭대학교 컴퓨터공학과 Spolart팀

**팀 구성**
- 20201758 구한빈
- 20201781 이윤재

## <u>Teamate</u> Project Background
- ### 필요성
  - 기존 로그라이트 장르는 메타프로그레션(런이 끝난 후 다음 런에도 적용되는 영구적인 업그레이드)이 필수적으로 전투를 동반함.
이는 게임에 익숙하지 않은 초보들에게 게임에 흥미를 잃게되는 진입장벽이 될 수도 있음.
  - 본 게임에서는 채광시스템을 메인으로 둬서 전투에 익숙하지 않은 플레이어도 게임을 오래 진행함으로서 수월한 게임 클리어가 가능해짐.
  - 전투를 비필수적인 요소로 만들기 위하여 몬스터들은 처치해도 보상이 없고, 채광의 방해꾼 역할을 수행함.
  

## System Design
  - ### System Requirements
    - 게임 코어 시스템: 플레이어는 키보드(WASD)로 이동하고 마우스 왼쪽/오른쪽 클릭으로 공격/채광을 수행함.

    - 던전 생성 시스템: 플레이어가 입장할 때마다 랜덤한 맵을 절차적으로 생성함.

    - 메타 시스템: 플레이어는 마을의 NPC와 상호작용하여 장비/스킬/마을시설의 영구적인 업그레이드를 진행함.

    - 데이터 관리 시스템: 게임 진행 상황(재화, 업그레이드 상태)은 저장되고 로드되어야 함.
  - ### 플레이 흐름도
    <img width="1377" height="802" alt="플레이흐름도" src="https://github.com/user-attachments/assets/af95508b-87f1-4a98-82aa-2166eccacc6f" />

    
## Case Study
  - ### Description
<img width="1903" height="1080" alt="NPCInteraction" src="https://github.com/user-attachments/assets/f2af5c43-a407-4613-bac1-e19c1977e83d" />
<img width="1727" height="1022" alt="Upgrade" src="https://github.com/user-attachments/assets/7e2d45e6-4037-4412-8396-fc17071b62ec" />
<img width="1747" height="996" alt="Mining" src="https://github.com/user-attachments/assets/181420cb-e4f0-4c90-b568-56f518a562f4" />

- ### 주요 알고리즘 및 설계 방법:
  - 절차적 던전 생성 (Procedural Generation): 방 생성 및 충돌 해소: 무작위 크기의 방을 생성한 후, 방들에 감쇠를 부여하고 충돌 해소 시뮬레이션을 반복하여 맵을 구성.
  - 플레이어 시스템 설계: 스킬 시스템은 ScriptableObject 기반 데이터와 코루틴을 활용한 전략 패턴으로 확장성과 유연성을 확보.
  - 데이터 통합 관리: 게임 내 데이터(스탯, 장비, 광물 등)는 dataManager.cs(퍼사드 패턴)를 통해 단일화된 진입점에서 관리. UI는 옵저버/이벤트 기반으로 데이터를 구독하여 일관된 갱신을 보장.
## Conclusion
  - 이번 프로젝트를 통해 유니티를 활용한 로그라이크 게임 개발의 핵심 요소인 절차적 던전 생성, FSM 기반 AI, 그리고 로그라이크 장르의 핵심 콘텐츠(채광, 전투, 지속적인 성장 루프) 구현까지 성공적으로 완수할 수 있었습니다.
  - 향후 과제로는 멀티플레이 구현을 목표로 하고있습니다.
