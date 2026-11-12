using System;
using System.Collections.Generic;
using NUnit.Framework;

// 마케팅 사용 제한, 영업일 적용, 날짜 이벤트 연동과 데이터 보호를 검증한다.
public class MarketingManagerTests
{
    private PhaseData phaseData;
    private PhaseManager phaseManager;
    private MarketingData data;
    private MarketingManager manager;

    // 두 마케팅 각각에 대해 1~14일차의 사용일과 기대 적용일을 제공한다.
    private static IEnumerable<object[]> BusinessDayCases
    {
        get
        {
            int[] expectedDays = { 2, 3, 4, 5, 8, 8, 8, 9, 10, 11, 12, 15, 15, 15 };
            foreach (string id in new[] { MarketingManager.ChaerinId, MarketingManager.SoeunId })
            {
                for (int day = 1; day <= expectedDays.Length; day++)
                    yield return new object[] { id, day, expectedDays[day - 1] };
            }
        }
    }

    // 각 테스트가 서로 영향을 주지 않도록 1일차 아침과 미사용 마케팅 데이터를 준비한다.
    [SetUp]
    public void SetUp()
    {
        phaseData = new PhaseData();
        phaseManager = new PhaseManager(phaseData);
        data = new MarketingData();
        manager = new MarketingManager(data, phaseManager);
    }

    // 초기 상태에서는 이번 주 사용, 최초 사용, 적용 중 여부가 모두 false인지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void Constructor_WithDefaultData_StartsUnusedAndInactive(string id)
    {
        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.False);
        Assert.That(manager.IsMarketingFirstUsed(id), Is.False);
        Assert.That(manager.IsMarketingActive(id), Is.False);
        Assert.That(GetCharacterData(data, id).applyDay, Is.Zero);
    }

    // 최초 사용 시 두 사용 플래그가 설정되고 다음 영업일에 적용되도록 기록되는지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void UseMarketing_ForFirstTime_RecordsUsageAndApplicationDay(string id)
    {
        manager.UseMarketing(id);

        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(id), Is.True);
        Assert.That(GetCharacterData(data, id).usedThisWeek, Is.True);
        Assert.That(GetCharacterData(data, id).firstUsed, Is.True);
        Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(2));
        Assert.That(manager.IsMarketingActive(id), Is.False);
    }

    // 같은 주에 다시 사용하면 예외로 거부하고 최초 사용 때 기록한 상태와 적용일을 유지하는지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void UseMarketing_TwiceInSameWeek_ThrowsAndPreservesState(string id)
    {
        manager.UseMarketing(id);
        phaseData.day = 3;

        Assert.Throws<InvalidOperationException>(() => manager.UseMarketing(id));

        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(id), Is.True);
        Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(2));
    }

    // 두 주의 모든 요일에 대해 금·토·일 사용은 다음 월요일, 나머지는 다음 날에 적용되는지 확인한다.
    [TestCaseSource(nameof(BusinessDayCases))]
    public void UseMarketing_OnEachWeekday_RecordsNextBusinessDay(string id, int day, int expectedDay)
    {
        phaseData.day = day;

        manager.UseMarketing(id);

        Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(expectedDay));
    }

    // 실제 시간대 진행으로 적용일 전에는 비활성, 당일에는 활성, 다음 날에는 비활성인지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void IsMarketingActive_ThroughDateProgression_IsTrueOnlyOnApplicationDay(string id)
    {
        manager.UseMarketing(id);
        Assert.That(manager.IsMarketingActive(id), Is.False);

        AdvanceOneDay();

        Assert.That(manager.IsMarketingActive(id), Is.True);
        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.True);

        AdvanceOneDay();

        Assert.That(manager.IsMarketingActive(id), Is.False);
        Assert.That(manager.IsMarketingFirstUsed(id), Is.True);
    }

    // 한 마케팅 사용이 다른 마케팅의 플래그와 적용일을 변경하거나 독립적인 사용을 막지 않는지 확인한다.
    [TestCase(MarketingManager.ChaerinId, MarketingManager.SoeunId)]
    [TestCase(MarketingManager.SoeunId, MarketingManager.ChaerinId)]
    public void UseMarketing_WithDifferentCharacters_KeepsUsageIndependent(string id, string otherId)
    {
        manager.UseMarketing(id);

        Assert.That(manager.IsMarketingUsedThisWeek(otherId), Is.False);
        Assert.That(manager.IsMarketingFirstUsed(otherId), Is.False);
        Assert.That(manager.IsMarketingActive(otherId), Is.False);
        Assert.That(GetCharacterData(data, otherId).applyDay, Is.Zero);

        Assert.DoesNotThrow(() => manager.UseMarketing(otherId));
        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.True);
        Assert.That(manager.IsMarketingUsedThisWeek(otherId), Is.True);
        Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(2));
        Assert.That(GetCharacterData(data, otherId).applyDay, Is.EqualTo(2));
    }

    // 주간 경계에서는 사용 제한만 초기화하고 최초 사용 기록과 월요일 적용일을 보존하는지 확인한다.
    [TestCase(7)]
    [TestCase(14)]
    public void OnDayChanged_AtWeekBoundary_ResetsUsageButPreservesHistoryAndApplicationDay(int day)
    {
        phaseData.day = day;
        manager.UseMarketing(MarketingManager.ChaerinId);
        manager.UseMarketing(MarketingManager.SoeunId);

        AdvanceOneDay();

        Assert.That(phaseManager.GetDay(), Is.EqualTo(day + 1));
        foreach (string id in new[] { MarketingManager.ChaerinId, MarketingManager.SoeunId })
        {
            Assert.That(manager.IsMarketingUsedThisWeek(id), Is.False);
            Assert.That(manager.IsMarketingFirstUsed(id), Is.True);
            Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(day + 1));
            Assert.That(manager.IsMarketingActive(id), Is.True);
        }
    }

    // 두 주의 일반 날짜 변경에서는 사용 플래그와 최초 사용 기록, 적용일이 초기화되지 않는지 확인한다.
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    public void OnDayChanged_WithinWeek_PreservesUsageState(int day)
    {
        phaseData.day = day;
        manager.UseMarketing(MarketingManager.ChaerinId);
        manager.UseMarketing(MarketingManager.SoeunId);
        int chaerinApplyDay = data.chaerin.applyDay;
        int soeunApplyDay = data.soeun.applyDay;

        AdvanceOneDay();

        Assert.That(manager.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.True);
        Assert.That(manager.IsMarketingUsedThisWeek(MarketingManager.SoeunId), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(MarketingManager.ChaerinId), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(MarketingManager.SoeunId), Is.True);
        Assert.That(data.chaerin.applyDay, Is.EqualTo(chaerinApplyDay));
        Assert.That(data.soeun.applyDay, Is.EqualTo(soeunApplyDay));
    }

    // 새 주에는 다시 사용할 수 있고 최초 사용 기록을 유지하면서 새 적용일을 기록하는지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void UseMarketing_AfterWeeklyReset_CanBeUsedAgain(string id)
    {
        phaseData.day = 7;
        manager.UseMarketing(id);
        AdvanceOneDay();

        Assert.DoesNotThrow(() => manager.UseMarketing(id));

        Assert.That(manager.IsMarketingUsedThisWeek(id), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(id), Is.True);
        Assert.That(GetCharacterData(data, id).applyDay, Is.EqualTo(9));
        Assert.That(manager.IsMarketingActive(id), Is.False);
    }

    // 날짜를 넘기지 않는 시간대 변경으로는 주간 사용 제한이 초기화되지 않는지 확인한다.
    [Test]
    public void AdvancePhase_WithinDay_DoesNotResetUsage()
    {
        phaseData.day = 8;
        manager.UseMarketing(MarketingManager.ChaerinId);

        phaseManager.AdvancePhase();

        Assert.That(phaseData.phase, Is.EqualTo(GamePhase.Lunch));
        Assert.That(manager.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.True);
        Assert.That(data.chaerin.applyDay, Is.EqualTo(9));
    }

    // 미등록 ID와 null·빈 문자열·공백은 모든 공개 작업에서 거부하고 기존 마케팅 데이터를 유지하는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("UNKNOWN")]
    [TestCase("mkt-ch")]
    public void MarketingOperations_WithInvalidId_ThrowAndPreserveState(string id)
    {
        manager.UseMarketing(MarketingManager.ChaerinId);

        Assert.Throws<ArgumentException>(() => manager.IsMarketingUsedThisWeek(id));
        Assert.Throws<ArgumentException>(() => manager.IsMarketingFirstUsed(id));
        Assert.Throws<ArgumentException>(() => manager.IsMarketingActive(id));
        Assert.Throws<ArgumentException>(() => manager.UseMarketing(id));

        Assert.That(data.chaerin.usedThisWeek, Is.True);
        Assert.That(data.chaerin.firstUsed, Is.True);
        Assert.That(data.chaerin.applyDay, Is.EqualTo(2));
        Assert.That(data.soeun.usedThisWeek, Is.False);
        Assert.That(data.soeun.firstUsed, Is.False);
        Assert.That(data.soeun.applyDay, Is.Zero);
    }

    // 마케팅 데이터 없이 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MarketingManager(null, phaseManager));
    }

    // 날짜 Manager 없이 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullPhaseManager_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MarketingManager(new MarketingData(), null));
    }

    // 현재 생성자의 복구 동작에 따라 누락된 캐릭터 데이터가 기본 객체로 생성되는지 확인한다.
    [Test]
    public void Constructor_WithMissingCharacterData_InitializesDefaultObjects()
    {
        MarketingData initialData = new MarketingData { chaerin = null, soeun = null, seogyeong = null };

        MarketingManager initialManager = new MarketingManager(initialData, phaseManager);

        Assert.That(initialData.chaerin, Is.Not.Null);
        Assert.That(initialData.soeun, Is.Not.Null);
        Assert.That(initialData.seogyeong, Is.Not.Null);
        Assert.That(initialManager.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.False);
        Assert.That(initialManager.IsMarketingUsedThisWeek(MarketingManager.SoeunId), Is.False);
    }

    // 데이터 교체 시 누락된 캐릭터 데이터를 복구하고 새 데이터에 사용 결과를 기록하는지 확인한다.
    [Test]
    public void LoadData_WithMissingCharacterData_InitializesUsableDefaultObjects()
    {
        MarketingData loadedData = new MarketingData { chaerin = null, soeun = null, seogyeong = null };

        manager.LoadData(loadedData);
        manager.UseMarketing(MarketingManager.ChaerinId);
        manager.UseMarketing(MarketingManager.SoeunId);

        Assert.That(loadedData.chaerin.usedThisWeek, Is.True);
        Assert.That(loadedData.soeun.usedThisWeek, Is.True);
        Assert.That(loadedData.seogyeong, Is.Not.Null);
        Assert.That(data.chaerin.usedThisWeek, Is.False);
        Assert.That(data.soeun.usedThisWeek, Is.False);
    }

    // 주 시작 날짜에 데이터를 불러와도 저장된 캐릭터 객체와 사용 상태가 임의로 초기화되지 않는지 확인한다.
    [Test]
    public void LoadData_WithExistingState_PreservesObjectsAndStoredUsage()
    {
        phaseData.day = 8;
        CharacterMarketingData chaerin = new CharacterMarketingData
        {
            usedThisWeek = true, firstUsed = true, applyDay = 8
        };
        MarketingData loadedData = new MarketingData { chaerin = chaerin };

        manager.LoadData(loadedData);

        Assert.That(loadedData.chaerin, Is.SameAs(chaerin));
        Assert.That(manager.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.True);
        Assert.That(manager.IsMarketingFirstUsed(MarketingManager.ChaerinId), Is.True);
        Assert.That(manager.IsMarketingActive(MarketingManager.ChaerinId), Is.True);
        Assert.That(chaerin.applyDay, Is.EqualTo(8));
    }

    // 데이터 교체 후 조회와 사용은 새 데이터에만 반영되고 이전 데이터는 유지되는지 확인한다.
    [Test]
    public void LoadData_WithNewData_ReplacesReadAndWriteTarget()
    {
        manager.UseMarketing(MarketingManager.ChaerinId);
        MarketingData loadedData = new MarketingData();

        manager.LoadData(loadedData);

        Assert.That(manager.IsMarketingUsedThisWeek(MarketingManager.ChaerinId), Is.False);
        Assert.That(manager.IsMarketingFirstUsed(MarketingManager.ChaerinId), Is.False);

        phaseData.day = 3;
        manager.UseMarketing(MarketingManager.ChaerinId);

        Assert.That(loadedData.chaerin.usedThisWeek, Is.True);
        Assert.That(loadedData.chaerin.firstUsed, Is.True);
        Assert.That(loadedData.chaerin.applyDay, Is.EqualTo(4));
        Assert.That(data.chaerin.usedThisWeek, Is.True);
        Assert.That(data.chaerin.applyDay, Is.EqualTo(2));
    }

    // 데이터 교체 후에도 날짜 이벤트 연결이 유지되고 주간 초기화는 새 데이터에만 반영되는지 확인한다.
    [Test]
    public void LoadData_BeforeWeeklyReset_UpdatesOnlyNewDataOnDayChanged()
    {
        phaseData.day = 7;
        manager.UseMarketing(MarketingManager.ChaerinId);
        manager.UseMarketing(MarketingManager.SoeunId);
        MarketingData loadedData = new MarketingData
        {
            chaerin = new CharacterMarketingData { usedThisWeek = true, firstUsed = true, applyDay = 8 },
            soeun = new CharacterMarketingData { usedThisWeek = true, firstUsed = true, applyDay = 8 }
        };

        manager.LoadData(loadedData);
        AdvanceOneDay();

        Assert.That(loadedData.chaerin.usedThisWeek, Is.False);
        Assert.That(loadedData.soeun.usedThisWeek, Is.False);
        Assert.That(loadedData.chaerin.firstUsed, Is.True);
        Assert.That(loadedData.soeun.firstUsed, Is.True);
        Assert.That(loadedData.chaerin.applyDay, Is.EqualTo(8));
        Assert.That(loadedData.soeun.applyDay, Is.EqualTo(8));
        Assert.That(data.chaerin.usedThisWeek, Is.True);
        Assert.That(data.soeun.usedThisWeek, Is.True);
    }

    // null 데이터 교체를 거부한 뒤에도 기존 데이터로 사용하고 날짜 이벤트로 주간 초기화를 받을 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentDataAndSubscription()
    {
        phaseData.day = 7;

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        manager.UseMarketing(MarketingManager.ChaerinId);
        Assert.That(data.chaerin.usedThisWeek, Is.True);

        AdvanceOneDay();

        Assert.That(data.chaerin.usedThisWeek, Is.False);
        Assert.That(data.chaerin.firstUsed, Is.True);
        Assert.That(data.chaerin.applyDay, Is.EqualTo(8));
    }

    // 적용일 계산 실패를 강제로 발생시켜 사용 플래그와 적용일이 모두 변경 전 상태를 유지하는지 확인한다.
    [TestCase(MarketingManager.ChaerinId)]
    [TestCase(MarketingManager.SoeunId)]
    public void UseMarketing_WhenApplicationDayCalculationFails_PreservesEntireState(string id)
    {
        // 날짜 진행의 오버플로가 아니라 적용일 계산 실패 시의 데이터 보호를 검증하기 위한 입력이다.
        phaseData.day = int.MaxValue;
        CharacterMarketingData character = GetCharacterData(data, id);

        Assert.Throws<OverflowException>(() => manager.UseMarketing(id));

        Assert.That(character.usedThisWeek, Is.False);
        Assert.That(character.firstUsed, Is.False);
        Assert.That(character.applyDay, Is.Zero);
    }

    // 실제 PhaseManager를 통해 아침부터 다음 날 아침까지 진행해 날짜 이벤트도 함께 발생시킨다.
    private void AdvanceOneDay()
    {
        for (int step = 0; step < 5; step++)
            phaseManager.AdvancePhase();
    }

    // 검증할 ID의 원본 데이터에 접근해 공개 조회값과 저장되는 상태를 함께 확인할 수 있도록 한다.
    private static CharacterMarketingData GetCharacterData(MarketingData source, string id)
    {
        switch (id)
        {
            case MarketingManager.ChaerinId:
                return source.chaerin;
            case MarketingManager.SoeunId:
                return source.soeun;
            default:
                throw new ArgumentException("테스트에서 지원하지 않는 마케팅 ID입니다.", nameof(id));
        }
    }
}
