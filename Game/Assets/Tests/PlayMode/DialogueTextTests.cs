using System.Collections;
using System.Reflection;
using DdukDduk.Dialogue;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class DialogueTextTests
{
    private GameObject root;
    private DialogueUIController ui;
    private TMP_Text body;
    private TMP_Text feedback;
    private float previousTimeScale;

    // 실제 프리팹을 생성해 직렬화 참조와 클릭 이벤트까지 포함한 실행 환경을 준비한다.
    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        root = Object.Instantiate(Resources.Load<GameObject>("UI/DialoguePanel"));
        ui = root.GetComponent<DialogueUIController>();
        body = root.transform.Find("DialogueContent/DialogueBox/DialogueText").GetComponent<TMP_Text>();
        feedback = root.transform.Find("DialogueContent/Feedback").GetComponent<TMP_Text>();
        typeof(DialogueUIController).GetField("charactersPerSecond", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, 20f);
    }

    // 테스트에서 생성한 UI를 제거하고 시간 배율을 원래대로 복구한다.
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = previousTimeScale;
        Object.DestroyImmediate(root);
    }

    // 시간이 지나면 일부 글자만 보이다가 전체 출력과 다음 안내 표시까지 완료되는지 확인한다.
    [UnityTest]
    public IEnumerator RevealsThenCompletes()
    {
        ui.ShowLine("화자", "대화 문구입니다.");
        Assert.AreEqual(0, body.maxVisibleCharacters);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(body.maxVisibleCharacters, 0);
        Assert.IsTrue(ui.IsRevealing);
        yield return new WaitForSecondsRealtime(.7f);
        Assert.IsFalse(ui.IsRevealing);
        Assert.IsTrue(root.transform.Find("DialogueContent/DialogueBox/NextHint").gameObject.activeSelf);
    }

    // 실제 프리팹 클릭 이벤트의 첫 호출은 문장만 완성하고 두 번째 호출부터 다음 요청으로 이어져야 한다.
    [Test]
    public void FirstClickCompletesSecondRequestsNext()
    {
        ui.ShowLine("화자", "대화 UI를 확인하는 임시 문구입니다.");
        var button = root.transform.Find("DialogueContent/DialogueBox").GetComponent<UnityEngine.UI.Button>();
        button.onClick.Invoke();
        Assert.IsFalse(ui.IsRevealing);
        Assert.AreEqual("", feedback.text);
        button.onClick.Invoke();
        StringAssert.Contains("연결 대기", feedback.text);
    }

    // 패널 또는 숨김 상태에서 글자 수와 클릭 진행이 멈추고 해제 후 다시 증가하는지 확인한다.
    [UnityTest]
    public IEnumerator PanelsAndHidePauseWithoutCatchUp()
    {
        System.Action[] open = { ui.OpenSave, ui.OpenLoad, ui.OpenHistory, ui.OpenSettings, ui.HideDialogue };
        foreach (var action in open)
        {
            ui.ShowLine("화자", new string('가', 100));
            yield return new WaitForSecondsRealtime(.1f);
            action();
            int count = body.maxVisibleCharacters;
            yield return new WaitForSecondsRealtime(.15f);
            ui.OnDialogueClicked();
            Assert.AreEqual(count, body.maxVisibleCharacters);
            Assert.AreEqual("", feedback.text);
            ui.ClosePanels();
            ui.ShowDialogue();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.Greater(body.maxVisibleCharacters, count);
            Assert.Less(body.maxVisibleCharacters, 100);
        }
    }

    // 출력 도중 문장을 교체해도 이전 코루틴이 새 문장에 글자를 추가하거나 완료 상태를 덮어쓰지 않아야 한다.
    [UnityTest]
    public IEnumerator ReplacementCancelsOldOutput()
    {
        ui.ShowLine("화자", new string('가', 80));
        yield return new WaitForSecondsRealtime(.1f);
        ui.ShowLine("새 화자", "새 대사");
        Assert.AreEqual(0, body.maxVisibleCharacters);
        ui.CompleteLine();
        int count = body.maxVisibleCharacters;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual("새 대사", body.text);
        Assert.AreEqual(count, body.maxVisibleCharacters);
        Assert.IsFalse(ui.IsRevealing);
    }

    // null 대사는 즉시 완료되고 화자가 없는 문장은 이름판을 숨겨야 한다.
    [Test]
    public void EmptyAndNarrationAreSupported()
    {
        ui.ShowLine(null, null);
        Assert.AreEqual("", body.text);
        Assert.IsFalse(ui.IsRevealing);
        Assert.IsFalse(root.transform.Find("DialogueContent/DialogueBox/NamePlate").gameObject.activeSelf);
        ui.ShowLine("화자", "대사");
        Assert.IsTrue(root.transform.Find("DialogueContent/DialogueBox/NamePlate").gameObject.activeSelf);
    }

    // 루트 재활성화 시 이전 출력 위치부터 재개하며 비활성 시간은 출력에 반영하지 않는다.
    [UnityTest]
    public IEnumerator DisableAndEnableResume()
    {
        ui.ShowLine("화자", new string('가', 80));
        yield return new WaitForSecondsRealtime(.1f);
        root.SetActive(false);
        int count = body.maxVisibleCharacters;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(count, body.maxVisibleCharacters);
        root.SetActive(true);
        yield return new WaitForSecondsRealtime(.1f);
        Assert.Greater(body.maxVisibleCharacters, count);
        Assert.IsTrue(ui.IsRevealing);
    }

    // 비활성 UI에도 문장을 전달할 수 있고 다시 켜면 준비된 문장이 출력되어야 한다.
    [UnityTest]
    public IEnumerator ShowLineWhileInactiveStartsOnEnable()
    {
        root.SetActive(false);
        ui.ShowLine("화자", "대화 문구입니다.");
        Assert.AreEqual(0, body.maxVisibleCharacters);
        root.SetActive(true);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(body.maxVisibleCharacters, 0);
        Assert.IsTrue(ui.IsRevealing);
    }

    // 게임 시간이 정지해도 UI의 글자 출력은 실제 경과 시간에 따라 진행되어야 한다.
    [UnityTest]
    public IEnumerator RevealsWithZeroTimeScale()
    {
        Time.timeScale = 0f;
        ui.ShowLine("화자", "대화 문구입니다.");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(body.maxVisibleCharacters, 0);
    }

    // TMP 서식 태그를 글자 수로 계산하지 않고 표시할 실제 문자 수만 사용해야 한다.
    [Test]
    public void RichTextUsesParsedCharacterCount()
    {
        ui.ShowLine("화자", "<b>대사</b>");
        ui.CompleteLine();
        Assert.AreEqual(2, body.maxVisibleCharacters);
        Assert.AreEqual("<b>대사</b>", body.text);
    }
}
