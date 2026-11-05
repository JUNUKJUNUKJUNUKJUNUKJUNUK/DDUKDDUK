using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

// 스토리 데이터의 보관·교체, null 거부와 기존 상태 보존을 검증한다.
public class StoryManagerTests
{
    private StoryData data;
    private StoryManager manager;

    // 각 테스트가 서로 영향을 주지 않도록 진행 기록이 있는 새 데이터와 Manager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new StoryData
        {
            completedEventIds = new List<string> { "event-01", "event-02" },
            currentRoute = "Common",
            currentEventId = "event-03",
            lineIndex = 4,
            choiceHistory = new List<int> { 0, 1 }
        };
        manager = new StoryManager(data);
    }

    // 전달한 데이터 객체를 그대로 보관하고 기존 완료 기록·루트·이벤트·대사 위치·선택 이력을 유지하는지 확인한다.
    [Test]
    public void Constructor_WithExistingProgress_StoresProvidedDataWithoutChangingProgress()
    {
        Assert.That(GetStoredData(manager), Is.SameAs(data));
        Assert.That(data.completedEventIds, Is.EqualTo(new[] { "event-01", "event-02" }));
        Assert.That(data.currentRoute, Is.EqualTo("Common"));
        Assert.That(data.currentEventId, Is.EqualTo("event-03"));
        Assert.That(data.lineIndex, Is.EqualTo(4));
        Assert.That(data.choiceHistory, Is.EqualTo(new[] { 0, 1 }));
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StoryManager(null));
    }

    // 데이터 교체 시 새 객체와 그 진행 기록을 보관하고 이전 데이터의 진행 기록은 유지하는지 확인한다.
    [Test]
    public void LoadData_WithNewData_ReplacesStoredReferenceWithoutChangingProgress()
    {
        StoryData loadedData = new StoryData
        {
            completedEventIds = new List<string> { "event-10" },
            currentRoute = "Chaerin",
            currentEventId = "event-11",
            lineIndex = 8,
            choiceHistory = new List<int> { 2, 0 }
        };

        manager.LoadData(loadedData);

        Assert.That(GetStoredData(manager), Is.SameAs(loadedData));
        Assert.That(loadedData.completedEventIds, Is.EqualTo(new[] { "event-10" }));
        Assert.That(loadedData.currentRoute, Is.EqualTo("Chaerin"));
        Assert.That(loadedData.currentEventId, Is.EqualTo("event-11"));
        Assert.That(loadedData.lineIndex, Is.EqualTo(8));
        Assert.That(loadedData.choiceHistory, Is.EqualTo(new[] { 2, 0 }));
        Assert.That(data.completedEventIds, Is.EqualTo(new[] { "event-01", "event-02" }));
        Assert.That(data.currentRoute, Is.EqualTo("Common"));
        Assert.That(data.currentEventId, Is.EqualTo("event-03"));
        Assert.That(data.lineIndex, Is.EqualTo(4));
        Assert.That(data.choiceHistory, Is.EqualTo(new[] { 0, 1 }));
    }

    // null 데이터 교체를 거부하면 기존 객체와 진행 기록이 보존되는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentProgress()
    {
        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(GetStoredData(manager), Is.SameAs(data));
        Assert.That(data.completedEventIds, Is.EqualTo(new[] { "event-01", "event-02" }));
        Assert.That(data.currentRoute, Is.EqualTo("Common"));
        Assert.That(data.currentEventId, Is.EqualTo("event-03"));
        Assert.That(data.lineIndex, Is.EqualTo(4));
        Assert.That(data.choiceHistory, Is.EqualTo(new[] { 0, 1 }));
    }

    // 이미 교체한 뒤 null 입력이 실패해도 최초 데이터로 돌아가지 않고 가장 최근의 정상 데이터를 유지하는지 확인한다.
    [Test]
    public void LoadData_WithNullAfterReplacement_PreservesLatestValidData()
    {
        StoryData loadedData = new StoryData { currentEventId = "event-10", lineIndex = 3 };
        manager.LoadData(loadedData);

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(GetStoredData(manager), Is.SameAs(loadedData));
        Assert.That(loadedData.currentEventId, Is.EqualTo("event-10"));
        Assert.That(loadedData.lineIndex, Is.EqualTo(3));
    }

    // 공개 조회 API가 없어 테스트에서만 private data 참조를 읽으며 실제 구현에는 접근용 API를 추가하지 않는다.
    private static StoryData GetStoredData(StoryManager target)
    {
        FieldInfo field = typeof(StoryManager).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "StoryManager의 data 필드를 찾을 수 없습니다.");
        return (StoryData)field.GetValue(target);
    }
}
