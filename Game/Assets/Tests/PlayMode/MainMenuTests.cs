using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DdukDduk.MainMenu;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 새 게임의 초기화·프롤로그 진입과 시작 화면의 임시 슬롯 표시를 검증한다.
public class MainMenuTests
{
    private Scene scene;
    private Scene originalActiveScene;
    private MainMenuController menu;
    private GameManager testManager;

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
        if (!originalActiveScene.IsValid() || !originalActiveScene.isLoaded)
            SceneManager.SetActiveScene(SceneManager.CreateScene("MainMenuTestsCleanup"));
        if (scene.IsValid() && scene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(scene);
        var prologue = SceneManager.GetSceneByPath("Assets/Scenes/Prologue.unity");
        if (prologue.IsValid() && prologue.isLoaded)
            yield return SceneManager.UnloadSceneAsync(prologue);
        if (testManager != null) Object.Destroy(testManager.gameObject);
        yield return null;
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
    }

    // Manager가 없는 시작 화면과 기존 진행이 있는 경우 모두 초기화 후 프롤로그로 이동한다.
    [UnityTest]
    public IEnumerator NewGameClick_InitializesStateAndOpensPrologue()
    {
        Assert.That(GameManager.Instance == null, Is.True);
        Find<UnityEngine.UI.Button>("NewGameButton").onClick.Invoke();
        testManager = GameManager.Instance;
        yield return null;
        AssertPrologue();

        testManager.Economy.AddMoney(12345);
        testManager.Phase.AdvancePhase();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity", LoadSceneMode.Single);
        scene = SceneManager.GetActiveScene();
        yield return null;
        Find<UnityEngine.UI.Button>("NewGameButton").onClick.Invoke();
        yield return null;
        Assert.That(GameManager.Instance, Is.SameAs(testManager));
        AssertPrologue();
    }

    private void AssertPrologue()
    {
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/Scenes/Prologue.unity"));
        var data = JsonUtility.FromJson<SaveData>(testManager.GetSaveJson());
        Assert.That(data.phase.day, Is.EqualTo(1));
        Assert.That(data.phase.phase, Is.EqualTo(GamePhase.Morning));
        Assert.That(data.economy.money, Is.EqualTo(100000));
        var label = Object.FindFirstObjectByType<TextMeshProUGUI>();
        Assert.That(label.text, Is.EqualTo("대충 프롤로그 씬"));
        Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.Center));
        Assert.That(label.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(label.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
        label.ForceMeshUpdate();
        Assert.That(label.isTextOverflowing, Is.False);
        Assert.That(label.font.HasCharacters(label.text), Is.True);
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

    // 공통 설정 패널의 세 음량과 화면 모드 항목을 열고 닫으며 슬롯 패널과 동시에 열리지 않는지 확인한다.
    [UnityTest]
    public IEnumerator SettingsClick_OpensAndClosesSharedPanel()
    {
        menu.Slots[0].hasSaveData = true;
        menu.RefreshSlots();
        menu.OpenSlots();

        Find<UnityEngine.UI.Button>("SettingsButton").onClick.Invoke();

        var panel = Find<Transform>("SettingsPanel").gameObject;
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(Find<Transform>("SlotPanel").gameObject.activeSelf, Is.False);
        Assert.That(panel.GetComponent<SettingsPanelController>(), Is.Not.Null);
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Slider>().Length, Is.EqualTo(3));
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Toggle>().Length, Is.EqualTo(1));
        yield return null;
        yield return null;

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
}
