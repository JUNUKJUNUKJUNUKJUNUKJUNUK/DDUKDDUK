using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DdukDduk.MainMenu;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 시작 화면의 임시 동작과 슬롯 표시 조건을 검증하며 실제 게임 시작·저장·불러오기는 수행하지 않는다.
public class MainMenuTests
{
    private Scene scene;
    private Scene originalActiveScene;
    private MainMenuController menu;

    // 실제 생성된 시작 화면 씬을 추가로 열어 Inspector 연결과 런타임 동작을 함께 검증한다.
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity", LoadSceneMode.Additive);
        scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
        SceneManager.SetActiveScene(scene);
        menu = Find<MainMenuController>("MainMenuCanvas");
        yield return null;
        yield return null;
    }

    // 테스트에서 연 시작 화면만 내리고 원래 활성 씬을 복구한다.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
            SceneManager.SetActiveScene(originalActiveScene);
        if (scene.IsValid() && scene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(scene);
    }

    // 기본 버튼 상태와 Canvas·EventSystem 연결, 새로 하기 버튼의 실제 레이캐스트 가능 여부를 확인한다.
    [UnityTest]
    public IEnumerator InitialScene_HasUsableButtonsAndNoAvailableSave()
    {
        Assert.That(Find<UnityEngine.UI.Button>("NewGameButton").interactable, Is.True);
        Assert.That(Find<UnityEngine.UI.Button>("ContinueButton").interactable, Is.False);
        Assert.That(Find<UnityEngine.UI.Button>("SettingsButton").interactable, Is.True);
        Assert.That(Find<UnityEngine.UI.Button>("QuitButton").interactable, Is.True);
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
        Assert.That(Find<Transform>("SettingsPanel").gameObject.activeSelf, Is.False);
        Assert.That(Find<UnityEngine.UI.GraphicRaycaster>("MainMenuCanvas"), Is.Not.Null);
        var button = Find<UnityEngine.UI.Button>("NewGameButton");
        var rect = button.GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(Find<EventSystem>("EventSystem"));
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = new List<RaycastResult>();
        Find<EventSystem>("EventSystem").RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(), Is.SameAs(button));
        yield return null;
        Capture("main-menu.png");
    }

    // 새로 하기는 선택 피드백만 표시하고 씬이나 GameManager를 변경하지 않는지 확인한다.
    [Test]
    public void NewGameClick_OnlyShowsFeedback()
    {
        GameManager previous = GameManager.Instance;
        Find<UnityEngine.UI.Button>("NewGameButton").onClick.Invoke();

        Assert.That(Find<TMP_Text>("FeedbackText").text, Is.EqualTo("새로 하기를 선택했습니다."));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
        Assert.That(GameManager.Instance, Is.SameAs(previous));
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
    }

    // 저장 표시가 없는 경우 호출이 들어와도 슬롯 패널이 열리지 않는지 확인한다.
    [Test]
    public void ContinueWithoutSave_DoesNotOpenSlots()
    {
        Find<UnityEngine.UI.Button>("ContinueButton").onClick.Invoke();

        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
    }

    // 임시 저장 슬롯이 있을 때만 이어 하기가 활성화되고 선택도 피드백만 표시하는지 확인한다.
    [UnityTest]
    public IEnumerator ContinueWithSave_OpensSlotsAndSelectionOnlyShowsFeedback()
    {
        menu.Slots[1].hasSaveData = true;
        menu.Slots[1].summary = "3일차 · 아침";
        menu.RefreshSlots();

        var resume = Find<UnityEngine.UI.Button>("ContinueButton");
        Assert.That(resume.interactable, Is.True);
        resume.onClick.Invoke();

        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.True);
        Assert.That(Find<UnityEngine.UI.Button>("Slot1Button").interactable, Is.False);
        Assert.That(Find<UnityEngine.UI.Button>("Slot2Button").interactable, Is.True);
        Assert.That(Find<UnityEngine.UI.Button>("Slot3Button").interactable, Is.False);
        yield return null;
        yield return null;
        Capture("continue-slots.png");
        GameManager previous = GameManager.Instance;

        Find<UnityEngine.UI.Button>("Slot2Button").onClick.Invoke();

        Assert.That(Find<TMP_Text>("FeedbackText").text, Is.EqualTo("슬롯 2번을 선택했습니다."));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
        Assert.That(GameManager.Instance, Is.SameAs(previous));
        Find<UnityEngine.UI.Button>("CloseSlotsButton").onClick.Invoke();
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
    }

    // 저장 표시를 모두 해제하고 갱신하면 이어 하기와 열려 있던 슬롯 패널이 비활성화되는지 확인한다.
    [Test]
    public void RefreshAfterRemovingSave_DisablesContinueAndClosesSlots()
    {
        menu.Slots[0].hasSaveData = true;
        menu.RefreshSlots();
        menu.OpenSlots();

        menu.Slots[0].hasSaveData = false;
        menu.RefreshSlots();

        Assert.That(Find<UnityEngine.UI.Button>("ContinueButton").interactable, Is.False);
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
    }

    // 환경 설정은 제목과 돌아가기만 있는 빈 패널을 열고 닫으며 슬롯 패널과 동시에 열리지 않는지 확인한다.
    [UnityTest]
    public IEnumerator SettingsClick_OpensAndClosesEmptyPanel()
    {
        menu.Slots[0].hasSaveData = true;
        menu.RefreshSlots();
        menu.OpenSlots();

        Find<UnityEngine.UI.Button>("SettingsButton").onClick.Invoke();

        var panel = Find<Transform>("SettingsPanel").gameObject;
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
        Assert.That(panel.GetComponentsInChildren<TMP_Text>().Length, Is.EqualTo(2));
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Slider>().Length, Is.Zero);
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Toggle>().Length, Is.Zero);
        yield return null;
        yield return null;
        Capture("settings-panel.png");

        Find<UnityEngine.UI.Button>("CloseSettingsButton").onClick.Invoke();
        Assert.That(panel.activeSelf, Is.False);
    }

    // 비활성 패널까지 포함해 테스트 씬 내부에서만 이름으로 연결된 컴포넌트를 찾는다.
    private T Find<T>(string name) where T : Component
    {
        var value = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name);
        Assert.That(value, Is.Not.Null, name);
        var component = value.GetComponent<T>();
        Assert.That(component, Is.Not.Null, name);
        return component;
    }

    // 화면 출력이 생략되는 배치 환경에서도 실제 Canvas를 렌더 타깃에 그려 시각 검증 이미지를 만든다.
    private void Capture(string filename)
    {
        string directory = Environment.GetEnvironmentVariable("DDUKDDUK_MENU_PREVIEW_DIR");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var canvas = Find<Canvas>("MainMenuCanvas");
        var camera = Find<Camera>("Main Camera");
        var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>())
                text.ForceMeshUpdate();
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                camera.Render();
            else
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, filename), image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = previousTarget;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousDistance;
            Canvas.ForceUpdateCanvases();
            RenderTexture.ReleaseTemporary(target);
            if (image != null) UnityEngine.Object.Destroy(image);
        }
    }
}
