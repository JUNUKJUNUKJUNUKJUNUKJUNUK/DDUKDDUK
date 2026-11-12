# 환경 설정

메인 메뉴의 환경 설정과 방앗간의 가게 설정은 같은 패널을 사용한다.

- Master, BGM, SFX: 0~100%, 기본값 100%. 조절 즉시 AudioMixer에 적용한다.
- 화면 모드: 테두리 없는 전체 화면 또는 창 모드. 기본값은 창 모드다.
- 창 모드 크기: 1280×720을 기준으로 모니터보다 크면 축소한다.
- 설정 완료 또는 패널 비활성화 시 저장한다. 기본값 복원도 즉시 적용하고 닫을 때 저장한다.
- 게임 시작 시 설정을 복원하며 새로하기와 슬롯 불러오기는 환경 설정을 초기화하지 않는다.

## 파일

| 파일 | 역할 |
| --- | --- |
| `Game/Assets/Scripts/Data/SettingsData.cs` | 기본값과 음량 범위 검증 |
| `Game/Assets/Scripts/Core/SettingsService.cs` | 설정 조회·변경·적용·저장 |
| `Game/Assets/Scripts/Core/SettingsRuntime.cs` | 시작 시 믹서 적용과 앱 일시 정지 시 저장 |
| `Game/Assets/UI/MainMenu/SettingsPanelController.cs` | 공통 UI 연결 |
| `Game/Assets/Resources/UI/SettingsPanel.prefab` | 방앗간에서도 사용하는 설정 패널 |
| `Game/Assets/Audio/Resources/GameAudioMixer.mixer` | Master 아래 BGM·SFX 그룹 |
| `Game/Assets/Editor/SettingsSceneBuilder.cs` | 시작 화면 생성 시 설정 항목 구성 |

설정은 `PlayerPrefs`의 `DDUKDDUK.Settings.v1` 키에 JSON으로 저장한다. 게임 진행 데이터인 `SaveData`나 `SaveFileService`의 슬롯 파일과 독립적이다. 저장값이 잘못되면 기본값을 사용하고 음량은 0~1로 제한한다.

## 오디오 연결

AudioSource의 Output을 용도에 맞는 `GameAudioMixer`의 BGM 또는 SFX 그룹에 지정한다. UI 클릭음도 SFX에 연결한다. 마스터와 각 그룹의 음량은 함께 적용된다.

믹서 창에서 노출한 볼륨 이름은 `SettingsService.MasterParameter`, `BgmParameter`, `SfxParameter`에서 관리한다. 해당 상수와 믹서의 노출 파라미터 이름을 함께 변경해야 한다.

`AudioListener.volume`은 1로 유지한다. 방앗간 소리 버튼은 마스터를 0으로 바꾸고 다시 누르면 직전 음량으로 복구한다. 별도로 `AudioListener.pause`를 변경하지 않는다.

```csharp
SettingsService.SetMasterVolume(0.8f);
SettingsService.SetBgmVolume(0.5f);
SettingsService.SetSfxVolume(1f);
SettingsService.SetFullscreen(true);
SettingsService.Save();
```

## 검증

Unity EditMode에서는 설정 기본값, 손상 값, 범위 보정, 저장·복원을 확인한다. PlayMode에서는 슬라이더·토글·기본값 복원·닫기 저장, 실제 믹서 값과 UI 레이캐스트를 확인한다. 테스트 키는 고유하게 만들고 UI 통합 테스트가 변경한 사용자 설정은 종료 시 원상 복구한다.

운영체제의 전체 화면·창 모드 전환은 Editor Game 뷰에 적용하지 않는다. Windows Player에서 전환, 재실행 복원, 기본값 복원을 검증한다.
