// 역할: 실제 환경 설정 프리팹의 동작을 PlayMode에서 검증한다.
// 구성: 믹서 적용, 닫기 저장, 기본값 복원, 슬라이더 표시와 클릭 영역.

using System.Collections;
using System.Linq;
using DdukDduk.MainMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SettingsPanelTests
{
    private GameObject popup;
    private GameObject canvasOwner;
    private GameObject eventOwner;

    private SettingsData previous;
    private bool hadPreference;
    private string preference;
    [SetUp]
    public void SetUp()
    {
        // 통합 테스트가 실제 설정 API를 호출하므로 저장 원문과 실행 중 값을 함께 보관한다.
        hadPreference = PlayerPrefs.HasKey(SettingsService.PreferenceKey);
        preference = PlayerPrefs.GetString(SettingsService.PreferenceKey);

        previous = new SettingsData
        {
            masterVolume = SettingsService.MasterVolume,
            bgmVolume = SettingsService.BgmVolume,
            sfxVolume = SettingsService.SfxVolume,
            fullscreen = SettingsService.Fullscreen
        };

        popup = Object.Instantiate(Resources.Load<GameObject>("UI/SettingsPanel"));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (popup != null)
            Object.Destroy(popup);
        if (canvasOwner != null)
            Object.Destroy(canvasOwner);
        if (eventOwner != null)
            Object.Destroy(eventOwner);

        yield return null;

        SettingsService.SetMasterVolume(previous.masterVolume);
        SettingsService.SetBgmVolume(previous.bgmVolume);
        SettingsService.SetSfxVolume(previous.sfxVolume);
        SettingsService.SetFullscreen(previous.fullscreen);
        SettingsService.Save();

        // 저장 유무와 JSON 원문까지 복구해 테스트가 사용자 설정을 바꾸지 않게 한다.
        if (hadPreference)
            PlayerPrefs.SetString(SettingsService.PreferenceKey, preference);
        else
            PlayerPrefs.DeleteKey(SettingsService.PreferenceKey);

        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator Controls_ApplyIndependentMixerVolumesAndSaveOnClose()
    {
        yield return null;

        Slider("MasterSlider").value = .5f;
        Slider("BgmSlider").value = .25f;
        Slider("SfxSlider").value = 0;

        Assert.That(SettingsService.Mixer.GetFloat(SettingsService.MasterParameter, out float master), Is.True);
        Assert.That(SettingsService.Mixer.GetFloat(SettingsService.BgmParameter, out float bgm), Is.True);
        Assert.That(SettingsService.Mixer.GetFloat(SettingsService.SfxParameter, out float sfx), Is.True);
        Assert.That(master, Is.EqualTo(-6.0206f).Within(.001));
        Assert.That(bgm, Is.EqualTo(-12.0412f).Within(.001));
        Assert.That(sfx, Is.EqualTo(-80));

        popup.GetComponent<SettingsPanelController>().Close();

        var saved = SettingsService.Read(SettingsService.PreferenceKey);
        Assert.That(saved.masterVolume, Is.EqualTo(.5f));
        Assert.That(saved.bgmVolume, Is.EqualTo(.25f));
        Assert.That(saved.sfxVolume, Is.Zero);

        popup.SetActive(true);
        Assert.That(Slider("BgmSlider").value, Is.EqualTo(.25f));
    }

    [UnityTest]
    public IEnumerator Defaults_RestoreUiAndSettingsAfterFullscreenSelection()
    {
        yield return null;

        Slider("MasterSlider").value = .1f;
        var toggle = popup.GetComponentInChildren<UnityEngine.UI.Toggle>();
        toggle.isOn = true;
        Assert.That(SettingsService.Fullscreen, Is.True);

        Button("RestoreDefaultsButton").onClick.Invoke();
        Assert.That(Slider("MasterSlider").value, Is.EqualTo(1));
        Assert.That(Slider("BgmSlider").value, Is.EqualTo(1));
        Assert.That(Slider("SfxSlider").value, Is.EqualTo(1));
        Assert.That(toggle.isOn, Is.False);

        Button("CloseSettingsButton").onClick.Invoke();
        Assert.That(popup.activeSelf, Is.False);
        Assert.That(SettingsService.Read(SettingsService.PreferenceKey).fullscreen, Is.False);
    }

    [UnityTest]
    public IEnumerator Sliders_HaveVisibleHandlesAndClickableTracks()
    {
        canvasOwner = new GameObject(
            "Settings Test Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasOwner.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasOwner.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        popup.transform.SetParent(canvasOwner.transform, false);
        eventOwner = new GameObject("Settings Test Events", typeof(UnityEngine.EventSystems.EventSystem));

        yield return null;
        Canvas.ForceUpdateCanvases();

        foreach (var slider in popup.GetComponentsInChildren<UnityEngine.UI.Slider>())
        {
            Assert.That(
                slider.handleRect.rect.height,
                Is.GreaterThan(0).And.LessThan(slider.GetComponent<RectTransform>().rect.height));
            var rect = slider.GetComponent<RectTransform>();
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var events = eventOwner.GetComponent<UnityEngine.EventSystems.EventSystem>();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = point }, hits);
            Assert.That(hits.Any(hit =>
                hit.gameObject.GetComponentInParent<UnityEngine.UI.Slider>() == slider), Is.True, slider.name);
        }
    }

    private UnityEngine.UI.Slider Slider(string name) =>
        popup.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Single(s => s.name == name);
    private UnityEngine.UI.Button Button(string name) =>
        popup.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(s => s.name == name);
}
