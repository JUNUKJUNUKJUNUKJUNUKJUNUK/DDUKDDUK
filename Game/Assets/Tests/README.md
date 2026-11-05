# 테스트 안내

공통 시스템과 시작 화면 테스트의 범위, 구성 및 실행 방법을 설명한다.
공통 시스템의 구조와 API 사용법은 [공통 시스템 설명서](../Scripts/README.md)를 참고한다.

## 1. 테스트 범위

### EditMode

Manager의 데이터 처리와 규칙, 저장 파일의 읽기·쓰기를 검증한다.

| 테스트 파일 | 주요 검증 내용 |
|---|---|
| [EconomyManagerTests.cs](EditMode/EconomyManagerTests.cs) | 초기 자금, 자금 추가·지출, 부족한 자금과 잘못된 금액 처리, 정수 오버플로 방지, 데이터 교체 |
| [ManagementManagerTests.cs](EditMode/ManagementManagerTests.cs) | 재고 준비·판매, 잘못된 수량과 떡 종류 처리, 평판 추가와 잘못된 값·정수 오버플로 처리, 데이터 교체 |
| [PhaseManagerTests.cs](EditMode/PhaseManagerTests.cs) | 시간대 순환과 날짜 증가, 이벤트 발생 순서, 불러오기 시 이벤트 미발생, 데이터 교체 |
| [MarketingManagerTests.cs](EditMode/MarketingManagerTests.cs) | 캐릭터별 마케팅 사용 조건, 적용일 계산, 주간 사용 기록 초기화, 계산 실패 시 상태 유지, 데이터 교체 |
| [ResearchManagerTests.cs](EditMode/ResearchManagerTests.cs) | 연구 완료 기록·조회, 중복 완료 방지, 잘못된 연구 ID 처리, 누락 목록 초기화, 데이터 교체 |
| [AffectionManagerTests.cs](EditMode/AffectionManagerTests.cs) | 캐릭터별 호감도 조회·증감, 0~100 범위 유지, 큰 증감 값 처리, 데이터 교체 |
| [StoryManagerTests.cs](EditMode/StoryManagerTests.cs) | 스토리 데이터 보관·교체, 기존 진행 값 유지, null 거부 |
| [SaveFileServiceTests.cs](EditMode/SaveFileServiceTests.cs) | 저장 시 폴더·슬롯 생성, 문자열 보존, 덮어쓰기와 슬롯 독립성, 잘못된 입력·없는 파일·쓰기 실패 처리, 임시 파일 정리 |

### PlayMode

GameObject와 씬, 프레임 진행이 필요한 동작 및 시스템 간 연결을 검증한다.

| 테스트 파일 | 주요 검증 내용 |
|---|---|
| [GameManagerTests.cs](PlayMode/GameManagerTests.cs) | 게임 상태의 JSON 변환·복원, 누락 데이터 보완, 비정상 저장 값 거부, 불러오기 실패 시 기존 상태 유지, Manager와 이벤트 구독 유지 |
| [GameManagerLifecycleTests.cs](PlayMode/GameManagerLifecycleTests.cs) | 싱글턴 초기화와 중복 제거, 씬 전환 시 유지, 플레이 시간 누적, 비활성화·시간 정지 처리, 파괴 후 재생성 |
| [SaveFileServiceIntegrationTests.cs](PlayMode/SaveFileServiceIntegrationTests.cs) | 실제 파일을 통한 게임 상태 저장·복원, 파일 부재·잘못된 JSON·비정상 저장 값으로 불러오기 실패 시 상태 유지 |
| [MainMenuTests.cs](PlayMode/MainMenuTests.cs) | 시작 화면 버튼 연결, 새로 하기 피드백, 임시 저장 데이터에 따른 이어 하기 활성화와 슬롯 표시·선택, 환경 설정 패널 |

## 2. 폴더 구성

Unity 프로젝트 루트는 저장소 아래의 `Game` 폴더다. 테스트는 해당 프로젝트의 `Assets/Tests`에 있다.

```text
Assets/Tests/
├─ README.md
├─ EditMode/
│  ├─ DDUKDDUK.EditMode.Tests.asmdef
│  ├─ EconomyManagerTests.cs
│  ├─ ManagementManagerTests.cs
│  ├─ PhaseManagerTests.cs
│  ├─ MarketingManagerTests.cs
│  ├─ ResearchManagerTests.cs
│  ├─ AffectionManagerTests.cs
│  ├─ StoryManagerTests.cs
│  └─ SaveFileServiceTests.cs
└─ PlayMode/
   ├─ DDUKDDUK.PlayMode.Tests.asmdef
   ├─ GameManagerTests.cs
   ├─ GameManagerLifecycleTests.cs
   ├─ SaveFileServiceIntegrationTests.cs
   └─ MainMenuTests.cs
```

EditMode는 에디터의 편집 상태에서 실행한다. PlayMode는 테스트 실행 중 플레이 모드로 진입하며, GameObject 생성과 씬 로드, 프레임 경과를 사용한다. 각 폴더의 `.asmdef`는 해당 테스트 코드의 어셈블리와 참조를 정의한다.

## 3. 실행 방법

### Unity Test Runner

1. Unity Hub에서 `Game` 폴더를 프로젝트로 연다. 현재 프로젝트의 Editor 버전은 `6000.3.25f1`이다.
2. 스크립트 컴파일과 에셋 임포트가 완료될 때까지 기다린다.
3. `Window > General > Test Runner`를 연다.
4. `EditMode` 탭에서 `Run All`을 실행한다.
5. 완료 후 `PlayMode` 탭에서 `Run All`을 실행한다.

특정 테스트만 실행하려면 목록에서 해당 클래스나 테스트를 선택하고 `Run Selected`를 사용한다. 실패한 항목을 선택하면 실패 메시지와 호출 위치를 확인할 수 있다.

### Unity CLI

Unity CLI와 프로젝트 버전에 맞는 Editor가 설치되어 있어야 한다. 아래는 현재 프로젝트 경로를 기준으로 한 **PowerShell** 예시다. `unity`가 인식되지 않으면 CLI 설치 후 터미널을 다시 연다.

먼저 해당 프로젝트의 Unity Editor를 닫고, 결과를 보관할 폴더를 만든다.

```powershell
New-Item -ItemType Directory -Force -Path "D:\Unity\DDUKDDUK-TestResults"
```

EditMode와 PlayMode는 아래 순서대로, 앞의 실행이 끝난 후 각각 실행한다.

```powershell
unity test "D:\Unity\DDUKDDUK\Game" --mode EditMode --editor-path "D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe" --output "D:\Unity\DDUKDDUK-TestResults\editmode-results.xml" --timeout 600 --format json

unity test "D:\Unity\DDUKDDUK\Game" --mode PlayMode --editor-path "D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe" --output "D:\Unity\DDUKDDUK-TestResults\playmode-results.xml" --timeout 600 --format json
```

특정 테스트 클래스만 실행하려면 `--filter`를 추가한다.

```powershell
unity test "D:\Unity\DDUKDDUK\Game" --mode EditMode --filter "EconomyManagerTests" --editor-path "D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe" --output "D:\Unity\DDUKDDUK-TestResults\economy-results.xml" --timeout 600 --format json
```

`--output`의 파일은 NUnit XML 결과다. 전체 결과와 실패 항목은 이 파일에서 확인한다. `--format json`은 CLI 응답 형식이며, 결과 파일의 형식을 바꾸지 않는다. 다른 PC에서는 프로젝트·Editor·결과 경로를 설치 환경에 맞게 변경한다.

## 4. 실행 시 주의사항

- **프로젝트 경로:** 저장소 루트가 아닌 `Game` 폴더를 열거나 CLI에 전달한다.
- **에디터와 실행 순서:** 위 CLI 명령은 별도 Editor 프로세스로 테스트를 실행하므로 같은 프로젝트의 Editor를 먼저 닫는다. EditMode와 PlayMode를 동시에 실행하지 않는다. Test Runner로 실행할 때는 Editor를 열어 둔다.
- **씬 변경 사항:** PlayMode 테스트는 씬을 열거나 전환하므로, 실행 전에 편집 중인 씬을 저장한다.
- **저장 파일:** 파일 테스트는 운영체제 임시 폴더 아래에 실행마다 고유한 경로를 만들고, 종료 시 정리한다. 기본 저장 경로를 확인하는 테스트는 경로만 확인하며 실제 플레이어 저장 파일을 생성·덮어쓰지 않는다.
- **Windows 전용 항목:** 읽기 전용 파일과 배타적으로 잠긴 파일 관련 테스트는 Windows용으로 지정되어 있다. 다른 운영체제에서는 해당 항목이 건너뛰어질 수 있다.
- **시작 화면:** `MainMenuTests`는 실제 `Assets/Scenes/MainMenu.unity`를 추가로 연다. 이어 하기의 저장 여부는 임시 슬롯 데이터를 사용하며, 실제 저장 파일과 연결하지 않는다.
