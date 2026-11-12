// 역할: 공통 환경 설정 패널의 UI와 SettingsService를 연결한다.
// 구성: 입력 이벤트 등록, 현재 설정 표시, 기본값 복원과 패널 닫기.

using TMPro;
using UnityEngine;

namespace DdukDduk.MainMenu
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.UI.Slider masterSlider;
        [SerializeField]
        private UnityEngine.UI.Slider bgmSlider;
        [SerializeField]
        private UnityEngine.UI.Slider sfxSlider;

        [SerializeField]
        private TMP_Text masterValue;
        [SerializeField]
        private TMP_Text bgmValue;
        [SerializeField]
        private TMP_Text sfxValue;

        [SerializeField]
        private UnityEngine.UI.Toggle fullscreenToggle;
        [SerializeField]
        private TMP_Text modeValue;

        private bool bound;

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            // 같은 패널을 다시 열어도 UI 입력 리스너는 한 번만 등록한다.
            if (!bound)
            {
                masterSlider.onValueChanged.AddListener(SettingsService.SetMasterVolume);
                bgmSlider.onValueChanged.AddListener(SettingsService.SetBgmVolume);
                sfxSlider.onValueChanged.AddListener(SettingsService.SetSfxVolume);
                fullscreenToggle.onValueChanged.AddListener(SettingsService.SetFullscreen);

                bound = true;
            }

            SettingsService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            SettingsService.Changed -= Refresh;
            SettingsService.Save();
        }

        public void Refresh()
        {
            // 표시를 갱신할 때 입력 이벤트가 다시 발생해 설정을 덮어쓰지 않게 한다.
            masterSlider.SetValueWithoutNotify(SettingsService.MasterVolume);
            bgmSlider.SetValueWithoutNotify(SettingsService.BgmVolume);
            sfxSlider.SetValueWithoutNotify(SettingsService.SfxVolume);

            masterValue.text = Mathf.RoundToInt(SettingsService.MasterVolume * 100) + "%";
            bgmValue.text = Mathf.RoundToInt(SettingsService.BgmVolume * 100) + "%";
            sfxValue.text = Mathf.RoundToInt(SettingsService.SfxVolume * 100) + "%";

            fullscreenToggle.SetIsOnWithoutNotify(SettingsService.Fullscreen);
            modeValue.text = SettingsService.Fullscreen ? "전체 화면" : "창 모드";
        }

        public void RestoreDefaults()
        {
            SettingsService.RestoreDefaults();
        }

        public void Close()
        {
            SettingsService.Save();
            gameObject.SetActive(false);
        }
    }
}
