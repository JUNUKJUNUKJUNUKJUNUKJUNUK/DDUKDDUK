using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// 저장 JSON과 불러오기, Manager 연결 유지, 비정상 데이터 거부 및 현재 상태 보존을 검증한다.
public class GameManagerTests
{
    private GameObject owner;
    private GameManager manager;

    // 비정상 저장 데이터의 종류와 LoadState·LoadJson 진입점을 각각 검증할 입력을 제공한다.
    private static IEnumerable<object[]> InvalidSaveCases
    {
        get
        {
            string[] fields =
            {
                "ZeroDay", "NegativeDay", "UndefinedPhase", "NegativeMoney", "NegativeReputation",
                "NegativePreparedQuantity", "NegativeRemainingQuantity", "RemainingExceedsPrepared",
                "UndefinedStockType", "NullStock", "DuplicateStock",
                "ChaerinBelowZero", "ChaerinAboveHundred",
                "SoeunBelowZero", "SoeunAboveHundred",
                "SeogyeongBelowZero", "SeogyeongAboveHundred", "NegativePlayTime"
            };
            foreach (string field in fields)
            {
                yield return new object[] { field, false };
                // Unity의 객체 직렬화가 null 항목을 변환할 수 있어 실제 null 참조 검증은 LoadState에서 수행한다.
                if (field != "NullStock")
                    yield return new object[] { field, true };
            }
        }
    }

    // 테스트 전용 GameManager를 생성하고 Update를 멈춰 프레임에 따른 playTime 변화 없이 저장값을 비교한다.
    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManager.Instance == null, Is.True, "기존 GameManager가 있으면 테스트가 상태를 변경하지 않습니다.");
        owner = new GameObject("GameManagerSaveLoadTests");
        manager = owner.AddComponent<GameManager>();
        manager.enabled = false;
    }

    // 테스트 전용 객체를 제거하고 프레임이 끝날 때까지 기다려 다음 테스트에 싱글턴 객체가 남지 않도록 한다.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (owner != null)
            UnityEngine.Object.Destroy(owner);
        yield return null;
    }

    // 초기 저장 JSON에 모든 기본 데이터와 유효한 미진행 대사 위치 -1이 보존되는지 확인한다.
    [Test]
    public void GetSaveJson_WithInitialState_PreservesAllDefaultValues()
    {
        AssertSavedState(new SaveData());
    }

    // 저장된 모든 하위 상태와 메타데이터를 불러오고 각 Manager의 조회에도 반영되는지 확인한다.
    [Test]
    public void LoadState_WithPopulatedData_RestoresAllState()
    {
        manager.LoadState(CreatePopulatedData());

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // 외부에서 생성한 JSON도 모든 하위 상태와 메타데이터를 잃지 않고 불러오는지 확인한다.
    [Test]
    public void LoadJson_WithPopulatedJson_RestoresAllState()
    {
        string json = JsonUtility.ToJson(CreatePopulatedData(), true);

        manager.LoadJson(json);

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // 저장 후 다른 상태로 교체했다가 JSON을 다시 불러오면 원래 진행 기록이 모두 복원되는지 확인한다.
    [Test]
    public void SaveAndLoadJson_AfterReplacingState_RestoresOriginalSnapshot()
    {
        manager.LoadState(CreatePopulatedData());
        string json = manager.GetSaveJson();
        manager.LoadState(new SaveData());

        manager.LoadJson(json);

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // 불러오기 전 얻어 둔 Manager 참조가 유지되고 같은 참조로 새 데이터의 자금을 변경할 수 있는지 확인한다.
    [Test]
    public void LoadState_PreservesManagerReferencesAndUpdatesTheirData()
    {
        PhaseManager phase = manager.Phase;
        EconomyManager economy = manager.Economy;
        ManagementManager management = manager.Management;
        ResearchManager research = manager.Research;
        MarketingManager marketing = manager.Marketing;
        AffectionManager affection = manager.Affection;
        StoryManager story = manager.Story;

        manager.LoadState(CreatePopulatedData());

        Assert.That(manager.Phase, Is.SameAs(phase));
        Assert.That(manager.Economy, Is.SameAs(economy));
        Assert.That(manager.Management, Is.SameAs(management));
        Assert.That(manager.Research, Is.SameAs(research));
        Assert.That(manager.Marketing, Is.SameAs(marketing));
        Assert.That(manager.Affection, Is.SameAs(affection));
        Assert.That(manager.Story, Is.SameAs(story));
        AssertPublicState(CreatePopulatedData());

        economy.AddMoney(5);
        SaveData expected = CreatePopulatedData();
        expected.economy.money += 5;
        AssertSavedState(expected);
    }

    // Manager를 통한 변경이 다음 저장에 반영되고 변경 대상이 아닌 기록은 유지되는지 확인한다.
    [Test]
    public void GetSaveJson_AfterManagerChanges_PreservesUpdatedAndUnchangedFields()
    {
        manager.LoadState(CreatePopulatedData());

        manager.Economy.AddMoney(10);
        manager.Management.AddReputation(5);
        manager.Management.SetPreparedQuantity(RiceCakeType.Garaetteok, 20);
        Assert.That(manager.Management.TrySellRiceCake(RiceCakeType.Garaetteok, 2), Is.True);
        manager.Research.CompleteResearch("RCP-02");
        manager.Marketing.UseMarketing(MarketingManager.SoeunId);
        manager.Affection.AddAffection(CharacterId.Chaerin, 5);
        manager.Phase.AdvancePhase();

        SaveData expected = CreatePopulatedData();
        expected.economy.money += 10;
        expected.management.reputation += 5;
        expected.management.riceCakeStocks[0].preparedQuantity = 20;
        expected.management.riceCakeStocks[0].remainingQuantity = 18;
        expected.research.completedResearchIds.Add("RCP-02");
        expected.marketing.chaerin.usedThisWeek = false;
        expected.marketing.soeun.usedThisWeek = false;
        expected.marketing.soeun.firstUsed = true;
        expected.marketing.soeun.applyDay = 8;
        expected.affection.chaerin += 5;
        expected.phase.day = 8;
        expected.phase.phase = GamePhase.Morning;
        AssertSavedState(expected);
        AssertPublicState(expected);
    }

    // null 저장 데이터는 거부하고 현재 저장 내용과 각 Manager의 상태를 유지하는지 확인한다.
    [Test]
    public void LoadState_WithNullData_ThrowsAndPreservesCurrentState()
    {
        manager.LoadState(CreatePopulatedData());

        Assert.Throws<ArgumentNullException>(() => manager.LoadState(null));

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // null·빈 문자열·공백·잘못된 JSON은 거부하고 현재 저장 내용과 각 Manager의 상태를 유지하는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("{ broken json")]
    public void LoadJson_WithInvalidInput_ThrowsAndPreservesCurrentState(string json)
    {
        manager.LoadState(CreatePopulatedData());

        Assert.Catch<ArgumentException>(() => manager.LoadJson(json));

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // 누락된 최상위 하위 객체는 현재 복구 동작에 따라 기본 데이터로 채우고 다른 저장값은 유지하는지 확인한다.
    [TestCase("phase")]
    [TestCase("economy")]
    [TestCase("management")]
    [TestCase("research")]
    [TestCase("marketing")]
    [TestCase("affection")]
    [TestCase("story")]
    public void LoadState_WithMissingSection_RestoresDefaultSection(string section)
    {
        SaveData incoming = CreatePopulatedData();
        SaveData expected = CreatePopulatedData();
        switch (section)
        {
            case "phase": incoming.phase = null; expected.phase = new PhaseData(); break;
            case "economy": incoming.economy = null; expected.economy = new EconomyData(); break;
            case "management": incoming.management = null; expected.management = new ManagementData(); break;
            case "research": incoming.research = null; expected.research = new ResearchData(); break;
            case "marketing": incoming.marketing = null; expected.marketing = new MarketingData(); break;
            case "affection": incoming.affection = null; expected.affection = new AffectionData(); break;
            case "story": incoming.story = null; expected.story = new StoryData(); break;
            default: throw new ArgumentException(nameof(section));
        }

        manager.LoadState(incoming);

        AssertSavedState(expected);
        AssertPublicState(expected);
    }

    // 각 Manager의 기존 복구 동작이 불러오기에도 적용되어 누락된 목록과 캐릭터 객체를 기본값으로 만드는지 확인한다.
    [TestCase("stocks")]
    [TestCase("researchIds")]
    [TestCase("chaerinMarketing")]
    [TestCase("soeunMarketing")]
    [TestCase("seogyeongMarketing")]
    public void LoadState_WithMissingNestedData_RestoresSupportedDefaults(string field)
    {
        SaveData incoming = CreatePopulatedData();
        SaveData expected = CreatePopulatedData();
        switch (field)
        {
            case "stocks":
                incoming.management.riceCakeStocks = null;
                expected.management.riceCakeStocks.Clear();
                break;
            case "researchIds":
                incoming.research.completedResearchIds = null;
                expected.research.completedResearchIds.Clear();
                break;
            case "chaerinMarketing":
                incoming.marketing.chaerin = null;
                expected.marketing.chaerin = new CharacterMarketingData();
                break;
            case "soeunMarketing":
                incoming.marketing.soeun = null;
                expected.marketing.soeun = new CharacterMarketingData();
                break;
            case "seogyeongMarketing":
                incoming.marketing.seogyeong = null;
                expected.marketing.seogyeong = new CharacterMarketingData();
                break;
            default: throw new ArgumentException(nameof(field));
        }

        manager.LoadState(incoming);

        AssertSavedState(expected);
        AssertPublicState(expected);
    }

    // 불러오기 자체는 전환 이벤트를 발생시키지 않고 이후 정상 진행에서는 기존 구독으로 각 이벤트가 한 번 발생하는지 확인한다.
    [Test]
    public void LoadState_PreservesPhaseSubscriptionsWithoutEmittingLoadEvents()
    {
        List<int> dayEvents = new List<int>();
        List<GamePhase> phaseEvents = new List<GamePhase>();
        manager.Phase.OnDayChanged += day => dayEvents.Add(day);
        manager.Phase.OnPhaseChanged += phase => phaseEvents.Add(phase);

        manager.LoadState(CreatePopulatedData());

        Assert.That(dayEvents, Is.Empty);
        Assert.That(phaseEvents, Is.Empty);

        manager.Phase.AdvancePhase();

        Assert.That(dayEvents, Is.EqualTo(new[] { 8 }));
        Assert.That(phaseEvents, Is.EqualTo(new[] { GamePhase.Morning }));
        Assert.That(manager.Marketing.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.False);
    }

    // 반복해서 불러온 뒤 날짜 변경은 가장 최근 데이터에만 반영되고 마케팅 주간 초기화도 정상 동작하는지 확인한다.
    [Test]
    public void LoadState_Repeatedly_ProgressesOnlyLatestStateAndResetsMarketing()
    {
        SaveData oldData = CreatePopulatedData();
        SaveData latestData = CreatePopulatedData();
        latestData.phase.day = 14;
        latestData.marketing.chaerin.applyDay = 15;
        manager.LoadState(oldData);
        manager.LoadState(latestData);
        manager.LoadState(latestData);

        manager.Phase.AdvancePhase();

        Assert.That(manager.Phase.GetDay(), Is.EqualTo(15));
        Assert.That(latestData.phase.day, Is.EqualTo(15));
        Assert.That(latestData.marketing.chaerin.usedThisWeek, Is.False);
        Assert.That(latestData.marketing.chaerin.firstUsed, Is.True);
        Assert.That(latestData.marketing.chaerin.applyDay, Is.EqualTo(15));
        Assert.That(manager.Marketing.IsMarketingActive(MarketingManager.ChaerinId), Is.True);
        Assert.That(oldData.phase.day, Is.EqualTo(7));
        Assert.That(oldData.phase.phase, Is.EqualTo(GamePhase.Night));
        Assert.That(oldData.marketing.chaerin.usedThisWeek, Is.True);
    }

    // 비정상 값과 손상된 재고 목록은 거부하고 현재 저장 내용과 각 Manager의 상태를 유지하는지 확인한다.
    [TestCaseSource(nameof(InvalidSaveCases))]
    public void Load_WithInvalidSaveData_ThrowsAndPreservesCurrentState(string field, bool viaJson)
    {
        manager.LoadState(CreatePopulatedData());
        SaveData invalid = CreatePopulatedData();
        invalid.economy.money = 98765;
        switch (field)
        {
            case "ZeroDay": invalid.phase.day = 0; break;
            case "NegativeDay": invalid.phase.day = -1; break;
            case "UndefinedPhase": invalid.phase.phase = (GamePhase)999; break;
            case "NegativeMoney": invalid.economy.money = -1; break;
            case "NegativeReputation": invalid.management.reputation = -1; break;
            case "NegativePreparedQuantity": invalid.management.riceCakeStocks[0].preparedQuantity = -1; break;
            case "NegativeRemainingQuantity": invalid.management.riceCakeStocks[0].remainingQuantity = -1; break;
            case "RemainingExceedsPrepared": invalid.management.riceCakeStocks[0].remainingQuantity = 11; break;
            case "UndefinedStockType": invalid.management.riceCakeStocks[0].type = (RiceCakeType)999; break;
            case "NullStock": invalid.management.riceCakeStocks.Add(null); break;
            case "DuplicateStock":
                invalid.management.riceCakeStocks.Add(new RiceCakeStock
                {
                    type = RiceCakeType.Garaetteok, preparedQuantity = 2, remainingQuantity = 2
                });
                break;
            case "ChaerinBelowZero": invalid.affection.chaerin = -1; break;
            case "ChaerinAboveHundred": invalid.affection.chaerin = 101; break;
            case "SoeunBelowZero": invalid.affection.soeun = -1; break;
            case "SoeunAboveHundred": invalid.affection.soeun = 101; break;
            case "SeogyeongBelowZero": invalid.affection.seogyeong = -1; break;
            case "SeogyeongAboveHundred": invalid.affection.seogyeong = 101; break;
            case "NegativePlayTime": invalid.playTime = -1f; break;
            default: throw new ArgumentException(nameof(field));
        }

        if (viaJson)
        {
            string json = JsonUtility.ToJson(invalid, true);
            Assert.Catch<ArgumentException>(() => manager.LoadJson(json));
        }
        else
        {
            Assert.Catch<ArgumentException>(() => manager.LoadState(invalid));
        }

        AssertSavedState(CreatePopulatedData());
        AssertPublicState(CreatePopulatedData());
    }

    // 모든 저장 필드를 기본값과 다른 값으로 채워 누락이나 초기화가 드러나는 독립적인 기대 데이터를 만든다.
    private static SaveData CreatePopulatedData()
    {
        return new SaveData
        {
            saveVersion = 1,
            savedAt = "2026-11-02T00:00:00+09:00",
            playTime = 123.5f,
            phase = new PhaseData { day = 7, phase = GamePhase.Night },
            economy = new EconomyData { money = 123456 },
            management = new ManagementData
            {
                reputation = 42,
                riceCakeStocks = new List<RiceCakeStock>
                {
                    new RiceCakeStock { type = RiceCakeType.Garaetteok, preparedQuantity = 10, remainingQuantity = 7 },
                    new RiceCakeStock { type = RiceCakeType.Jeolpyeon, preparedQuantity = 5, remainingQuantity = 3 }
                }
            },
            research = new ResearchData { completedResearchIds = new List<string> { "RCP-01", "DSP-01" } },
            marketing = new MarketingData
            {
                chaerin = new CharacterMarketingData { usedThisWeek = true, firstUsed = true, applyDay = 8 }
            },
            affection = new AffectionData { chaerin = 10, soeun = 50, seogyeong = 100 },
            story = new StoryData
            {
                completedEventIds = new List<string> { "event-01", "event-02" },
                currentRoute = "Common",
                currentEventId = "event-03",
                lineIndex = 4,
                choiceHistory = new List<int> { 0, 2 }
            }
        };
    }

    // 독립적으로 만든 기대 데이터를 같은 Unity 직렬화기로 변환해 모든 저장 필드가 일치하는지 확인한다.
    private void AssertSavedState(SaveData expected)
    {
        Assert.That(manager.GetSaveJson(), Is.EqualTo(JsonUtility.ToJson(expected, true)));
    }

    // 저장 JSON뿐 아니라 Manager의 공개 조회도 같은 데이터 상태를 가리키는지 확인한다.
    private void AssertPublicState(SaveData expected)
    {
        Assert.That(manager.Phase.GetDay(), Is.EqualTo(expected.phase.day));
        Assert.That(manager.Economy.GetMoney(), Is.EqualTo(expected.economy.money));
        Assert.That(manager.Management.GetReputation(), Is.EqualTo(expected.management.reputation));
        foreach (RiceCakeType type in Enum.GetValues(typeof(RiceCakeType)))
        {
            RiceCakeStock stock = expected.management.riceCakeStocks.Find(item => item.type == type);
            Assert.That(manager.Management.GetPreparedQuantity(type), Is.EqualTo(stock == null ? 0 : stock.preparedQuantity));
            Assert.That(manager.Management.GetRemainingQuantity(type), Is.EqualTo(stock == null ? 0 : stock.remainingQuantity));
        }
        foreach (string id in new[] { "RCP-01", "RCP-02", "RCP-03", "RCP-04", "DSP-01", "DSP-02" })
            Assert.That(manager.Research.IsResearchCompleted(id), Is.EqualTo(expected.research.completedResearchIds.Contains(id)));
        Assert.That(manager.Affection.GetAffection(CharacterId.Chaerin), Is.EqualTo(expected.affection.chaerin));
        Assert.That(manager.Affection.GetAffection(CharacterId.Soeun), Is.EqualTo(expected.affection.soeun));
        Assert.That(manager.Affection.GetAffection(CharacterId.Seogyeong), Is.EqualTo(expected.affection.seogyeong));
        AssertMarketingState(MarketingManager.ChaerinId, expected.marketing.chaerin, expected.phase.day);
        AssertMarketingState(MarketingManager.SoeunId, expected.marketing.soeun, expected.phase.day);
    }

    // 마케팅의 주간 사용·최초 사용·적용 여부가 불러온 날짜와 저장 데이터에 맞는지 확인한다.
    private void AssertMarketingState(string id, CharacterMarketingData expected, int day)
    {
        Assert.That(manager.Marketing.IsMarketingUsedThisWeek(id), Is.EqualTo(expected.usedThisWeek));
        Assert.That(manager.Marketing.IsMarketingFirstUsed(id), Is.EqualTo(expected.firstUsed));
        Assert.That(manager.Marketing.IsMarketingActive(id), Is.EqualTo(expected.applyDay > 0 && expected.applyDay == day));
    }
}
