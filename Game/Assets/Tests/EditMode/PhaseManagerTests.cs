using System;
using System.Collections.Generic;
using NUnit.Framework;

// 시간대·날짜 진행, 변경 이벤트의 순서와 상태, 데이터 교체를 검증한다.
public class PhaseManagerTests
{
    private PhaseData data;
    private PhaseManager manager;

    // 각 테스트가 서로 영향을 주지 않도록 1일차 아침의 새 데이터와 Manager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new PhaseData();
        manager = new PhaseManager(data);
    }

    // 기본 PhaseData로 시작하면 1일차 아침인지 확인한다.
    [Test]
    public void Constructor_WithDefaultData_StartsOnFirstMorning()
    {
        Assert.That(manager.GetDay(), Is.EqualTo(1));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
    }

    // 주간 경계 전후를 포함해 전달된 날짜를 그대로 조회하는지 확인한다.
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(14)]
    [TestCase(15)]
    public void GetDay_ReturnsProvidedDay(int day)
    {
        manager = new PhaseManager(new PhaseData { day = day });

        Assert.That(manager.GetDay(), Is.EqualTo(day));
    }

    // 저녁 이전의 시간대에서는 정해진 다음 시간대로만 진행하고 날짜는 유지되는지 확인한다.
    [TestCase(GamePhase.Morning, GamePhase.Lunch)]
    [TestCase(GamePhase.Lunch, GamePhase.Closing)]
    [TestCase(GamePhase.Closing, GamePhase.Evening)]
    public void AdvancePhase_BeforeEvening_AdvancesPhaseWithoutChangingDay(
        GamePhase currentPhase, GamePhase expectedPhase)
    {
        data.phase = currentPhase;

        manager.AdvancePhase();

        Assert.That(data.phase, Is.EqualTo(expectedPhase));
        Assert.That(manager.GetDay(), Is.EqualTo(1));
    }

    // 일반 날짜와 주간 경계 모두에서 저녁 다음은 날짜가 1 증가한 아침인지 확인한다.
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(14)]
    public void AdvancePhase_FromEvening_AdvancesToNextMorning(int day)
    {
        data.day = day;
        data.phase = GamePhase.Evening;

        manager.AdvancePhase();

        Assert.That(manager.GetDay(), Is.EqualTo(day + 1));
        Assert.That(data.day, Is.EqualTo(day + 1));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
    }

    // 이벤트 구독자가 없어도 이틀 연속 시간대가 순환하고 저녁에서만 날짜가 증가하는지 확인한다.
    [Test]
    public void AdvancePhase_OverTwoDays_RepeatsEntireCycleWithoutSubscribers()
    {
        GamePhase[] expectedPhases =
        {
            GamePhase.Lunch, GamePhase.Closing, GamePhase.Evening,
            GamePhase.Morning
        };

        for (int cycle = 0; cycle < 2; cycle++)
        {
            for (int step = 0; step < expectedPhases.Length; step++)
            {
                manager.AdvancePhase();

                Assert.That(data.phase, Is.EqualTo(expectedPhases[step]));
                int expectedDay = 1 + cycle + (step == expectedPhases.Length - 1 ? 1 : 0);
                Assert.That(manager.GetDay(), Is.EqualTo(expectedDay));
            }
        }
    }

    // 모든 전환에서 시간대 이벤트는 정확히 한 번 발생하고, 저녁에서만 날짜 이벤트도 한 번 발생하는지 확인한다.
    [TestCase(GamePhase.Morning, GamePhase.Lunch, 1, 0)]
    [TestCase(GamePhase.Lunch, GamePhase.Closing, 1, 0)]
    [TestCase(GamePhase.Closing, GamePhase.Evening, 1, 0)]
    [TestCase(GamePhase.Evening, GamePhase.Morning, 2, 1)]
    public void AdvancePhase_EmitsExpectedEventsWithUpdatedState(
        GamePhase currentPhase, GamePhase expectedPhase, int expectedDay, int expectedDayEventCount)
    {
        data.phase = currentPhase;
        List<GamePhase> phaseEvents = new List<GamePhase>();
        List<int> dayEvents = new List<int>();
        manager.OnPhaseChanged += phase =>
        {
            phaseEvents.Add(phase);
            Assert.That(phase, Is.EqualTo(expectedPhase));
            Assert.That(data.phase, Is.EqualTo(expectedPhase));
            Assert.That(manager.GetDay(), Is.EqualTo(expectedDay));
        };
        manager.OnDayChanged += day =>
        {
            dayEvents.Add(day);
            Assert.That(day, Is.EqualTo(expectedDay));
            Assert.That(manager.GetDay(), Is.EqualTo(expectedDay));
        };

        manager.AdvancePhase();

        Assert.That(phaseEvents, Is.EqualTo(new[] { expectedPhase }));
        Assert.That(dayEvents.Count, Is.EqualTo(expectedDayEventCount));
    }

    // 날짜 이벤트 시점에는 새 날짜와 기존 Evening이 보이고, 이어지는 시간대 이벤트에서 Morning이 보이는지 확인한다.
    [Test]
    public void AdvancePhase_FromEvening_EmitsDayEventBeforeMorningEvent()
    {
        data.phase = GamePhase.Evening;
        List<string> eventOrder = new List<string>();
        manager.OnDayChanged += day =>
        {
            eventOrder.Add("day");
            Assert.That(day, Is.EqualTo(2));
            Assert.That(data.day, Is.EqualTo(2));
            Assert.That(data.phase, Is.EqualTo(GamePhase.Evening));
        };
        manager.OnPhaseChanged += phase =>
        {
            eventOrder.Add("phase");
            Assert.That(phase, Is.EqualTo(GamePhase.Morning));
            Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
            Assert.That(manager.GetDay(), Is.EqualTo(2));
        };

        manager.AdvancePhase();

        Assert.That(eventOrder, Is.EqualTo(new[] { "day", "phase" }));
    }

    // 데이터 교체는 날짜와 시간대를 그대로 적용하며 진행 이벤트를 발생시키지 않는지 확인한다.
    [Test]
    public void LoadData_WithNewState_RestoresDayAndPhaseWithoutEmittingEvents()
    {
        int phaseEventCount = 0;
        int dayEventCount = 0;
        manager.OnPhaseChanged += phase => phaseEventCount++;
        manager.OnDayChanged += day => dayEventCount++;
        PhaseData loadedData = new PhaseData { day = 8, phase = GamePhase.Evening };

        manager.LoadData(loadedData);

        Assert.That(manager.GetDay(), Is.EqualTo(8));
        Assert.That(loadedData.phase, Is.EqualTo(GamePhase.Evening));
        Assert.That(phaseEventCount, Is.Zero);
        Assert.That(dayEventCount, Is.Zero);
        Assert.That(data.day, Is.EqualTo(1));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
    }

    // 데이터 교체 후 진행은 새 데이터에만 반영되고 기존 이벤트 구독도 유지되는지 확인한다.
    [Test]
    public void LoadData_WithNewData_ReplacesProgressTargetAndPreservesSubscriptions()
    {
        List<GamePhase> phaseEvents = new List<GamePhase>();
        List<int> dayEvents = new List<int>();
        manager.OnPhaseChanged += phase => phaseEvents.Add(phase);
        manager.OnDayChanged += day => dayEvents.Add(day);
        PhaseData loadedData = new PhaseData { day = 8, phase = GamePhase.Evening };

        manager.LoadData(loadedData);
        manager.AdvancePhase();

        Assert.That(loadedData.phase, Is.EqualTo(GamePhase.Morning));
        Assert.That(loadedData.day, Is.EqualTo(9));
        Assert.That(manager.GetDay(), Is.EqualTo(9));
        Assert.That(phaseEvents, Is.EqualTo(new[] { GamePhase.Morning }));
        Assert.That(dayEvents, Is.EqualTo(new[] { 9 }));
        Assert.That(data.day, Is.EqualTo(1));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PhaseManager(null));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 데이터와 이벤트 구독으로 다음 날 진행을 계속할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentStateAndSubscriptions()
    {
        data.day = 7;
        data.phase = GamePhase.Evening;
        List<GamePhase> phaseEvents = new List<GamePhase>();
        List<int> dayEvents = new List<int>();
        manager.OnPhaseChanged += phase => phaseEvents.Add(phase);
        manager.OnDayChanged += day => dayEvents.Add(day);

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(manager.GetDay(), Is.EqualTo(7));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Evening));
        Assert.That(phaseEvents, Is.Empty);
        Assert.That(dayEvents, Is.Empty);

        manager.AdvancePhase();

        Assert.That(manager.GetDay(), Is.EqualTo(8));
        Assert.That(data.day, Is.EqualTo(8));
        Assert.That(data.phase, Is.EqualTo(GamePhase.Morning));
        Assert.That(phaseEvents, Is.EqualTo(new[] { GamePhase.Morning }));
        Assert.That(dayEvents, Is.EqualTo(new[] { 8 }));
    }
}
