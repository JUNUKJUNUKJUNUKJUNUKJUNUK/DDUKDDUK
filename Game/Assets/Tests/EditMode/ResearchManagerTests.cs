using System;
using System.Collections.Generic;
using NUnit.Framework;

// 연구 완료 기록의 조회·추가, 중복 방지, 잘못된 ID 처리와 데이터 교체를 검증한다.
public class ResearchManagerTests
{
    private ResearchData data;
    private ResearchManager manager;

    // 등록된 연구 6개에 대해 동일한 검증을 각각 실행하도록 입력 목록을 제공한다.
    private static readonly string[] ResearchIds =
    {
        "RCP-01", "RCP-02", "RCP-03", "RCP-04", "DSP-01", "DSP-02"
    };

    // 각 테스트가 서로 영향을 주지 않도록 완료 기록이 없는 새 데이터와 Manager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new ResearchData();
        manager = new ResearchManager(data);
    }

    // 모든 유효한 연구는 초기 상태에서 미완료이며 조회만으로 완료 기록이 추가되지 않는지 확인한다.
    [TestCaseSource(nameof(ResearchIds))]
    public void IsResearchCompleted_WithUncompletedResearch_ReturnsFalseWithoutAddingRecord(string id)
    {
        Assert.That(manager.IsResearchCompleted(id), Is.False);
        Assert.That(data.completedResearchIds, Is.Empty);
    }

    // 유효한 연구를 완료하면 원본 데이터에 한 번 기록되고 다른 연구의 완료 여부에는 영향을 주지 않는지 확인한다.
    [TestCaseSource(nameof(ResearchIds))]
    public void CompleteResearch_WithValidId_RecordsCompletionWithoutAffectingOtherResearch(string id)
    {
        manager.CompleteResearch(id);

        Assert.That(manager.IsResearchCompleted(id), Is.True);
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { id }));
        foreach (string otherId in ResearchIds)
        {
            if (otherId != id)
                Assert.That(manager.IsResearchCompleted(otherId), Is.False);
        }
    }

    // 동일 연구를 반복 완료해도 중복 기록이 생기지 않고 다른 연구의 기존 기록도 유지되는지 확인한다.
    [TestCaseSource(nameof(ResearchIds))]
    public void CompleteResearch_WithAlreadyCompletedId_PreservesExistingRecordsWithoutDuplicates(string id)
    {
        string otherId = id == "RCP-01" ? "DSP-01" : "RCP-01";
        manager.CompleteResearch(otherId);
        manager.CompleteResearch(id);

        manager.CompleteResearch(id);
        manager.CompleteResearch(id);

        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { otherId, id }));
        Assert.That(manager.IsResearchCompleted(otherId), Is.True);
        Assert.That(manager.IsResearchCompleted(id), Is.True);
    }

    // 레시피 연구와 진열 연구를 모두 완료해도 각각의 기록과 조회 결과가 함께 유지되는지 확인한다.
    [Test]
    public void CompleteResearch_WithAllValidIds_PreservesEveryCompletion()
    {
        foreach (string id in ResearchIds)
            manager.CompleteResearch(id);

        Assert.That(data.completedResearchIds, Is.EquivalentTo(ResearchIds));
        foreach (string id in ResearchIds)
            Assert.That(manager.IsResearchCompleted(id), Is.True);
    }

    // null·빈 값·미등록 ID·대소문자나 공백이 다른 ID는 조회와 완료 모두에서 거부하고 기존 기록을 유지하는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("UNKNOWN")]
    [TestCase("RCP-00")]
    [TestCase("RCP-05")]
    [TestCase("DSP-03")]
    [TestCase("rcp-01")]
    [TestCase(" RCP-01")]
    [TestCase("RCP-01 ")]
    public void ResearchOperations_WithInvalidId_ThrowAndPreserveCompletedRecords(string id)
    {
        manager.CompleteResearch("RCP-01");
        manager.CompleteResearch("DSP-01");

        Assert.Throws<ArgumentException>(() => manager.IsResearchCompleted(id));
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01", "DSP-01" }));

        Assert.Throws<ArgumentException>(() => manager.CompleteResearch(id));
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01", "DSP-01" }));
        Assert.That(manager.IsResearchCompleted("RCP-01"), Is.True);
        Assert.That(manager.IsResearchCompleted("DSP-01"), Is.True);
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ResearchManager(null));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 기록을 조회하고 기존 데이터에 연구를 추가할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentData()
    {
        manager.CompleteResearch("RCP-01");

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(manager.IsResearchCompleted("RCP-01"), Is.True);
        manager.CompleteResearch("DSP-01");
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01", "DSP-01" }));
    }

    // 현재 생성자의 복구 동작에 따라 null 완료 목록을 빈 목록으로 만들고 연구를 완료할 수 있는지 확인한다.
    [Test]
    public void Constructor_WithNullCompletedList_InitializesUsableEmptyList()
    {
        ResearchData initialData = new ResearchData { completedResearchIds = null };

        ResearchManager initialManager = new ResearchManager(initialData);

        Assert.That(initialData.completedResearchIds, Is.Not.Null.And.Empty);
        Assert.That(initialManager.IsResearchCompleted("RCP-01"), Is.False);
        initialManager.CompleteResearch("RCP-01");
        Assert.That(initialData.completedResearchIds, Is.EqualTo(new[] { "RCP-01" }));
    }

    // 데이터 교체 시 null 완료 목록을 복구하고 이후 연구 완료는 새 데이터에만 기록되는지 확인한다.
    [Test]
    public void LoadData_WithNullCompletedList_InitializesUsableEmptyList()
    {
        manager.CompleteResearch("RCP-01");
        ResearchData loadedData = new ResearchData { completedResearchIds = null };

        manager.LoadData(loadedData);

        Assert.That(loadedData.completedResearchIds, Is.Not.Null.And.Empty);
        Assert.That(manager.IsResearchCompleted("RCP-01"), Is.False);
        manager.CompleteResearch("DSP-01");
        Assert.That(loadedData.completedResearchIds, Is.EqualTo(new[] { "DSP-01" }));
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01" }));
    }

    // 이미 완료된 연구가 있는 데이터로 생성하면 기록을 초기화하지 않고 완료 여부를 정확히 조회하는지 확인한다.
    [TestCaseSource(nameof(ResearchIds))]
    public void Constructor_WithExistingCompletion_PreservesStoredRecord(string id)
    {
        ResearchData initialData = new ResearchData
        {
            completedResearchIds = new List<string> { id }
        };

        ResearchManager initialManager = new ResearchManager(initialData);

        Assert.That(initialManager.IsResearchCompleted(id), Is.True);
        Assert.That(initialData.completedResearchIds, Is.EqualTo(new[] { id }));
        foreach (string otherId in ResearchIds)
        {
            if (otherId != id)
                Assert.That(initialManager.IsResearchCompleted(otherId), Is.False);
        }
    }

    // 데이터 교체 시 기록을 합치지 않고 새 데이터의 기록으로 조회하며 이후 완료도 새 데이터에만 반영되는지 확인한다.
    [Test]
    public void LoadData_WithExistingCompletions_ReplacesReadAndWriteTarget()
    {
        manager.CompleteResearch("RCP-01");
        ResearchData loadedData = new ResearchData
        {
            completedResearchIds = new List<string> { "DSP-01" }
        };

        manager.LoadData(loadedData);

        Assert.That(manager.IsResearchCompleted("RCP-01"), Is.False);
        Assert.That(manager.IsResearchCompleted("DSP-01"), Is.True);
        manager.CompleteResearch("DSP-01");
        manager.CompleteResearch("RCP-02");

        Assert.That(loadedData.completedResearchIds, Is.EqualTo(new[] { "DSP-01", "RCP-02" }));
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01" }));
    }

    // 여러 번 데이터를 교체해도 가장 최근 데이터만 사용하고 이전 데이터들의 기록은 유지되는지 확인한다.
    [Test]
    public void LoadData_Repeatedly_UsesOnlyLatestData()
    {
        manager.CompleteResearch("RCP-01");
        ResearchData firstLoadedData = new ResearchData
        {
            completedResearchIds = new List<string> { "RCP-02" }
        };
        ResearchData lastLoadedData = new ResearchData
        {
            completedResearchIds = new List<string> { "DSP-01" }
        };

        manager.LoadData(firstLoadedData);
        manager.LoadData(lastLoadedData);
        manager.CompleteResearch("DSP-02");

        Assert.That(manager.IsResearchCompleted("RCP-01"), Is.False);
        Assert.That(manager.IsResearchCompleted("RCP-02"), Is.False);
        Assert.That(manager.IsResearchCompleted("DSP-01"), Is.True);
        Assert.That(manager.IsResearchCompleted("DSP-02"), Is.True);
        Assert.That(lastLoadedData.completedResearchIds, Is.EqualTo(new[] { "DSP-01", "DSP-02" }));
        Assert.That(firstLoadedData.completedResearchIds, Is.EqualTo(new[] { "RCP-02" }));
        Assert.That(data.completedResearchIds, Is.EqualTo(new[] { "RCP-01" }));
    }
}
