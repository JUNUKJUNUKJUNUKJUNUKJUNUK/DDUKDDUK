// 역할: 환경 설정을 Unity 실행 시점과 연결한다.
// 구성: Start에서 믹서 적용, 앱 일시 정지 시 변경된 설정 저장.

using UnityEngine;

public sealed class SettingsRuntime : MonoBehaviour
{
    // AudioMixer 파라미터는 Awake 이전 초기화보다 Start 이후 적용해야 안정적이다.
    private void Start()
    {
        SettingsService.ApplyAudio();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SettingsService.Save();
    }
}
