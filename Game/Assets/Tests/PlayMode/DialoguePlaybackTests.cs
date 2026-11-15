using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DdukDduk.Dialogue;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public class DialoguePlaybackTests
{
    private GameObject root;
    private DialogueUIController ui;
    private TMP_Text body;
    private float timeScale;

    // 실제 프리팹으로 출력·모드·복귀 입력을 함께 검사하며 대기 시간만 짧게 조정한다.
    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        root = Object.Instantiate(Resources.Load<GameObject>("UI/DialoguePanel"));
        ui = root.GetComponent<DialogueUIController>();
        body = root.transform.Find("DialogueContent/DialogueBox/DialogueText").GetComponent<TMP_Text>();
        SetField(ui, "autoAdvanceDelay", .2f);
        SetField(ui, "skipAdvanceDelay", .05f);
        SetField(ui, "charactersPerSecond", 20f);
    }

    // 생성한 UI를 제거하고 테스트 전 시간 배율로 되돌린다.
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = timeScale;
        Object.DestroyImmediate(root);
    }

    // Inspector 전용 설정과 테스트 씬 참조를 테스트에서만 주입한다.
    private static void SetField(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    // 두 진행 모드는 서로 배타적이며 같은 버튼을 다시 누르면 수동으로 돌아가야 한다.
    [Test]
    public void ModesAreExclusiveAndToggleOff()
    {
        ui.ShowLine("화자", "대사", true);
        ui.RequestAuto();
        Assert.AreEqual(DialoguePlaybackMode.Auto, ui.PlaybackMode);
        StringAssert.Contains("켜짐", root.transform.Find("DialogueContent/Toolbar/AutoButton/Label").GetComponent<TMP_Text>().text);
        ui.RequestAuto();
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
        ui.RequestAuto();
        ui.RequestSkip();
        Assert.AreEqual(DialoguePlaybackMode.Skip, ui.PlaybackMode);
        Assert.IsFalse(ui.IsRevealing);
        Assert.AreEqual("자동 넘김", root.transform.Find("DialogueContent/Toolbar/AutoButton/Label").GetComponent<TMP_Text>().text);
        ui.RequestSkip();
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
    }

    // 자동 넘김은 문장 출력 중에는 요청하지 않고 출력 완료 후 별도 대기 시간이 지나야 요청한다.
    [UnityTest]
    public IEnumerator AutoWaitsForRevealAndDelay()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        ui.ShowLine("화자", new string('가', 40));
        ui.RequestAuto();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(0, requests);
        ui.CompleteLine();
        yield return new WaitForSecondsRealtime(.08f);
        Assert.AreEqual(0, requests);
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(1, requests);
    }

    // 다음 대사를 아직 받지 못한 경우 프레임마다 같은 문장의 요청을 반복하지 않아야 한다.
    [UnityTest]
    public IEnumerator PendingRequestIsSentOnlyOnce()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        ui.ShowLine("화자", "대사", true);
        ui.RequestSkip();
        yield return new WaitForSecondsRealtime(.3f);
        ui.RequestNextLine();
        Assert.AreEqual(1, requests);
        ui.ShowLine("화자", "다음 대사", true);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(2, requests);
    }

    // 읽은 문장은 즉시 표시하면서 연속 진행하지만 읽지 않은 문장을 받으면 멈추고 순차 출력한다.
    [UnityTest]
    public IEnumerator SkipStopsAtUnreadLine()
    {
        int requests = 0;
        ui.NextLineRequested += () =>
        {
            requests++;
            ui.ShowLine("화자", new string('가', 40), false);
        };
        ui.ShowLine("화자", "읽은 대사", true);
        ui.RequestSkip();
        Assert.IsFalse(ui.IsRevealing);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(1, requests);
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
        Assert.IsTrue(ui.IsRevealing);
        ui.RequestSkip();
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
    }

    // 모든 패널과 숨기기는 모드를 즉시 해제하고 복귀 후 시간이 지나도 자동 요청을 재개하지 않아야 한다.
    [UnityTest]
    public IEnumerator PanelsAndHideReturnToManual()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        System.Action[] open = { ui.OpenSave, ui.OpenLoad, ui.OpenHistory, ui.OpenSettings, ui.HideDialogue };
        foreach (bool skip in new[] { false, true })
        {
            foreach (var action in open)
            {
                ui.ShowLine("화자", "대사", true);
                ui.CompleteLine();
                if (skip)
                {
                    ui.RequestSkip();
                }
                else
                {
                    ui.RequestAuto();
                }
                action();
                Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
                ui.ClosePanels();
                ui.ShowDialogue();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.AreEqual(0, requests);
            }
        }
    }

    // 대화 영역을 수동 클릭하면 자동 모드를 해제하고 기존 첫 클릭 완성 규칙을 유지한다.
    [Test]
    public void ManualClickCancelsAutoAndCompletesOnly()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        ui.ShowLine("화자", "출력 중인 대사입니다.");
        ui.RequestAuto();
        ui.OnDialogueClicked();
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
        Assert.IsFalse(ui.IsRevealing);
        Assert.AreEqual(0, requests);
    }

    // 복귀 영역은 화면 모서리와 중앙 모두에서 가장 위에 잡히며 버튼·문구 없이 클릭 하나만 소비한다.
    [UnityTest]
    public IEnumerator InvisibleFullScreenRestoreConsumesClick()
    {
        var eventObject = new GameObject("TestEventSystem", typeof(EventSystem));
        try
        {
            int requests = 0;
            ui.NextLineRequested += () => requests++;
            ui.ShowLine("화자", "대사", true);
            ui.CompleteLine();
            ui.RequestAuto();
            ui.HideDialogue();
            ui.RestoreFromPointer();
            Assert.IsTrue(ui.IsHidden, "숨긴 프레임에는 복귀하지 않아야 한다.");
            yield return null;

            var input = root.transform.Find("RestoreInput").gameObject;
            Assert.IsNull(root.transform.Find("RestoreButton"));
            Assert.AreEqual(0, input.GetComponentsInChildren<UnityEngine.UI.Button>().Length);
            Assert.AreEqual(0, input.GetComponentsInChildren<TMP_Text>().Length);
            Assert.AreEqual(0f, input.GetComponent<UnityEngine.UI.Image>().color.a);
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>());
            Vector2[] points = { new Vector2(.01f, .01f), new Vector2(.99f, .99f), new Vector2(.5f, .5f) };
            foreach (var point in points)
            {
                pointer.position = new Vector2(Screen.width * point.x, Screen.height * point.y);
                var hits = new List<RaycastResult>();
                eventObject.GetComponent<EventSystem>().RaycastAll(pointer, hits);
                Assert.IsNotEmpty(hits);
                Assert.AreEqual(input, hits[0].gameObject);
            }

            ExecuteEvents.Execute(input, pointer, ExecuteEvents.pointerClickHandler);
            Assert.IsFalse(ui.IsHidden);
            Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
            ui.OnDialogueClicked();
            Assert.AreEqual(0, requests, "복귀 클릭과 같은 프레임에는 진행하지 않아야 한다.");
            yield return null;
            ui.OnDialogueClicked();
            Assert.AreEqual(1, requests);
        }
        finally
        {
            Object.DestroyImmediate(eventObject);
        }
    }

    // 대사 공급자가 없으면 자동 진행은 한 번의 연결 대기 안내 후 수동 상태로 돌아간다.
    [UnityTest]
    public IEnumerator MissingStoryProviderStopsPlayback()
    {
        ui.ShowLine("화자", "대사", true);
        ui.RequestSkip();
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
        StringAssert.Contains("연결 대기", root.transform.Find("DialogueContent/Feedback").GetComponent<TMP_Text>().text);
    }

    // 종료 또는 선택지 대기를 알리면 다음 요청 예약이 취소되어 추가 진행이 없어야 한다.
    [UnityTest]
    public IEnumerator EndAndChoiceStopFurtherRequests()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        ui.ShowLine("화자", "대사", true);
        ui.RequestSkip();
        ui.StopPlayback();
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(0, requests);
        ui.RequestAuto();
        ui.EndDialogue();
        ui.RequestNextLine();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(0, requests);
        Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
    }

    // 게임 시간 배율이 0이어도 빨리 감기의 실제 시간 간격이 동작해야 한다.
    [UnityTest]
    public IEnumerator PlaybackUsesUnscaledTime()
    {
        int requests = 0;
        ui.NextLineRequested += () => requests++;
        Time.timeScale = 0;
        ui.ShowLine("화자", "대사", true);
        ui.RequestSkip();
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(1, requests);
    }

    // 테스트 씬 공급자가 임시 대사를 끝까지 진행한 뒤 멈추고 숨김 중 테스트 버튼도 감추는지 확인한다.
    [UnityTest]
    public IEnumerator SampleDriverEndsAndHidesItsGuide()
    {
        var guide = new GameObject("TestGuide", typeof(RectTransform), typeof(CanvasGroup));
        guide.SetActive(false);
        try
        {
            var driver = guide.AddComponent<DialogueUITestDriver>();
            var status = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            status.transform.SetParent(guide.transform, false);
            SetField(driver, "dialogueUI", ui);
            SetField(driver, "guideGroup", guide.GetComponent<CanvasGroup>());
            SetField(driver, "testStatus", status.GetComponent<TMP_Text>());
            guide.SetActive(true);
            yield return null;
            ui.HideDialogue();
            yield return null;
            yield return null;
            Assert.AreEqual(0f, guide.GetComponent<CanvasGroup>().alpha);
            ui.ShowDialogue();
            ui.RequestSkip();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(DialoguePlaybackMode.Manual, ui.PlaybackMode);
            StringAssert.Contains("대화 끝", status.GetComponent<TMP_Text>().text);
        }
        finally
        {
            Object.DestroyImmediate(guide);
        }
    }
}
