// 역할: 게임 진행 상태와 독립적으로 환경 설정을 관리한다.
// 구성: 시작 시 복원, 음량·화면 모드 적용, 변경 알림, 저장과 기본값 복원.

using System;
using UnityEngine;
using UnityEngine.Audio;

public static class SettingsService
{
    // AudioMixer 창의 노출 파라미터 이름과 정확히 일치해야 한다.
    public const string MasterParameter = "MyExposedParam 2";
    public const string BgmParameter = "MyExposedParam 1";
    public const string SfxParameter = "MyExposedParam";
    public const string PreferenceKey = "DDUKDDUK.Settings.v1";

    private static SettingsData data;
    private static AudioMixer mixer;
    private static bool dirty;
    private static float lastAudibleMaster = 1f;

    public static event Action Changed;

    public static float MasterVolume
    {
        get
        {
            EnsureLoaded();

            return data.masterVolume;
        }
    }

    public static float BgmVolume
    {
        get
        {
            EnsureLoaded();

            return data.bgmVolume;
        }
    }

    public static float SfxVolume
    {
        get
        {
            EnsureLoaded();

            return data.sfxVolume;
        }
    }

    public static bool Fullscreen
    {
        get
        {
            EnsureLoaded();

            return data.fullscreen;
        }
    }

    public static AudioMixer Mixer
    {
        get
        {
            EnsureLoaded();

            return mixer;
        }
    }

    // Domain Reload를 끈 Editor에서도 이전 실행의 상태와 이벤트 구독이 남지 않게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Application.quitting -= Save;
        data = null;
        mixer = null;
        dirty = false;
        lastAudibleMaster = 1f;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureLoaded();
        ApplyScreen();

        Application.quitting += Save;

        var owner = new GameObject("SettingsRuntime");
        UnityEngine.Object.DontDestroyOnLoad(owner);
        owner.AddComponent<SettingsRuntime>();
    }

    private static void EnsureLoaded()
    {
        if (data != null)
            return;

        data = Read(PreferenceKey);
        if (data.masterVolume > 0)
            lastAudibleMaster = data.masterVolume;

        mixer = Resources.Load<AudioMixer>("GameAudioMixer");
    }

    public static SettingsData Read(string key)
    {
        SettingsData result = null;

        try
        {
            if (PlayerPrefs.HasKey(key))
                result = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(key));
        }
        catch (ArgumentException)
        {
            // 손상된 설정 때문에 시작이 막히지 않도록 아래에서 기본값을 사용한다.
        }

        result = result ?? new SettingsData();
        result.Validate();
        return result;
    }

    public static void Write(string key, SettingsData value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        value.Validate();

        PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        PlayerPrefs.Save();
    }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();

        data.masterVolume = value;
        CommitAudioChange();

        if (data.masterVolume > 0)
            lastAudibleMaster = data.masterVolume;
    }

    public static void SetBgmVolume(float value)
    {
        EnsureLoaded();

        data.bgmVolume = value;
        CommitAudioChange();
    }

    public static void SetSfxVolume(float value)
    {
        EnsureLoaded();

        data.sfxVolume = value;
        CommitAudioChange();
    }

    public static void ToggleMasterMute()
    {
        // 음소거를 해제할 때는 마지막으로 사용한 0보다 큰 음량으로 돌아간다.
        SetMasterVolume(MasterVolume > 0 ? 0 : lastAudibleMaster);
        Save();
    }

    private static void CommitAudioChange()
    {
        data.Validate();
        dirty = true;

        ApplyAudio();
        Changed?.Invoke();
    }

    public static void SetFullscreen(bool enabled)
    {
        EnsureLoaded();

        data.fullscreen = enabled;
        dirty = true;

        ApplyScreen();
        Changed?.Invoke();
    }

    public static float VolumeToDecibels(float volume)
    {
        // log(0)을 피하고, 믹서의 최소 감쇠값인 -80 dB를 음소거 값으로 사용한다.
        return volume <= 0 ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(Mathf.Clamp01(volume)));
    }

    public static void ApplyAudio()
    {
        EnsureLoaded();

        // 전역 음량과 믹서 음량이 중복 적용되지 않게 조절 창구를 믹서로 통일한다.
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        if (mixer == null)
            return;

        SetMixerVolume(MasterParameter, data.masterVolume);
        SetMixerVolume(BgmParameter, data.bgmVolume);
        SetMixerVolume(SfxParameter, data.sfxVolume);
    }

    private static void SetMixerVolume(string parameter, float value)
    {
        if (!mixer.SetFloat(parameter, VolumeToDecibels(value)))
            Debug.LogError("AudioMixer의 노출 파라미터를 확인해주세요: " + parameter);
    }

    private static void ApplyScreen()
    {
        // Editor의 Game 뷰에는 운영체제 창 모드를 적용하지 않는다.
        if (Application.isEditor)
            return;

        int monitorWidth = Display.main.systemWidth;
        int monitorHeight = Display.main.systemHeight;

        if (data.fullscreen)
            Screen.SetResolution(monitorWidth, monitorHeight, FullScreenMode.FullScreenWindow);
        else
        {
            // 창 크기는 1280×720을 기준으로 하되 모니터 바깥으로 넘치지 않게 줄인다.
            float scale = Mathf.Min(1f, monitorWidth * 0.9f / 1280f, monitorHeight * 0.9f / 720f);
            Screen.SetResolution(
                Mathf.RoundToInt(1280 * scale),
                Mathf.RoundToInt(720 * scale),
                FullScreenMode.Windowed);
        }
    }

    public static void RestoreDefaults()
    {
        data = new SettingsData();
        lastAudibleMaster = 1f;
        dirty = true;

        ApplyAudio();
        ApplyScreen();
        Changed?.Invoke();
    }

    public static void Save()
    {
        // 슬라이더를 움직일 때마다 디스크에 쓰지 않고 변경된 설정만 저장한다.
        if (!dirty || data == null)
            return;

        Write(PreferenceKey, data);
        dirty = false;
    }
}
