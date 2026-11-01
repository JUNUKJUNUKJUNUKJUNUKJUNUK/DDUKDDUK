using System;
using System.Collections.Generic;
using NUnit.Framework;

// 캐릭터별 호감도 조회·증감, 0~100 제한, 잘못된 입력 처리와 데이터 교체를 검증한다.
public class AffectionManagerTests
{
    private AffectionData data;
    private AffectionManager manager;

    // 세 캐릭터 각각에 대해 일반 증감, 상·하한 경계와 극단적인 변화량을 검증할 입력을 제공한다.
    private static IEnumerable<object[]> AffectionChangeCases
    {
        get
        {
            int[,] cases =
            {
                { 50, 10, 60 },
                { 50, -10, 40 },
                { 50, 0, 50 },
                { 0, 0, 0 },
                { 100, 0, 100 },
                { 50, -50, 0 },
                { 50, 50, 100 },
                { 50, -51, 0 },
                { 50, 51, 100 },
                { 0, -1, 0 },
                { 0, 1, 1 },
                { 100, 1, 100 },
                { 100, -1, 99 },
                { 0, int.MaxValue, 100 },
                { 50, int.MaxValue, 100 },
                { 100, int.MaxValue, 100 },
                { 0, int.MinValue, 0 },
                { 50, int.MinValue, 0 },
                { 100, int.MinValue, 0 }
            };

            foreach (CharacterId id in Enum.GetValues(typeof(CharacterId)))
            {
                for (int index = 0; index < cases.GetLength(0); index++)
                    yield return new object[] { id, cases[index, 0], cases[index, 1], cases[index, 2] };
            }
        }
    }

    // 각 테스트가 독립적으로 실행되고 캐릭터 연결 오류도 드러나도록 서로 다른 호감도 값을 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new AffectionData { chaerin = 20, soeun = 50, seogyeong = 80 };
        manager = new AffectionManager(data);
    }

    // 기본 데이터로 생성하면 세 캐릭터의 초기 호감도가 각각 0인지 확인한다.
    [TestCase(CharacterId.Chaerin)]
    [TestCase(CharacterId.Soeun)]
    [TestCase(CharacterId.Seogyeong)]
    public void Constructor_WithDefaultData_StartsWithZeroAffection(CharacterId id)
    {
        AffectionManager initialManager = new AffectionManager(new AffectionData());

        Assert.That(initialManager.GetAffection(id), Is.Zero);
    }

    // 캐릭터별로 다른 값을 넣었을 때 해당 캐릭터의 호감도를 정확히 조회하는지 확인한다.
    [TestCase(CharacterId.Chaerin, 20)]
    [TestCase(CharacterId.Soeun, 50)]
    [TestCase(CharacterId.Seogyeong, 80)]
    public void GetAffection_ReturnsCorrectCharacterValue(CharacterId id, int expected)
    {
        Assert.That(manager.GetAffection(id), Is.EqualTo(expected));
    }

    // 변화량을 적용한 결과가 0~100으로 제한되고 원본 데이터의 해당 캐릭터만 변경되는지 확인한다.
    [TestCaseSource(nameof(AffectionChangeCases))]
    public void AddAffection_WithVariousAmounts_ClampsResultAndPreservesOtherCharacters(
        CharacterId id, int initial, int amount, int expected)
    {
        data = new AffectionData { chaerin = initial, soeun = initial, seogyeong = initial };
        manager = new AffectionManager(data);
        Assert.That(manager.GetAffection(id), Is.EqualTo(initial));

        manager.AddAffection(id, amount);

        Assert.That(manager.GetAffection(id), Is.EqualTo(expected));
        Assert.That(data.chaerin, Is.EqualTo(id == CharacterId.Chaerin ? expected : initial));
        Assert.That(data.soeun, Is.EqualTo(id == CharacterId.Soeun ? expected : initial));
        Assert.That(data.seogyeong, Is.EqualTo(id == CharacterId.Seogyeong ? expected : initial));
    }

    // 상한이나 하한에 도달한 뒤에는 제한된 현재 값에서 다음 증감이 시작되는지 확인한다.
    [TestCase(CharacterId.Chaerin)]
    [TestCase(CharacterId.Soeun)]
    [TestCase(CharacterId.Seogyeong)]
    public void AddAffection_AfterReachingLimits_ContinuesFromClampedValue(CharacterId id)
    {
        manager.AddAffection(id, 200);
        Assert.That(manager.GetAffection(id), Is.EqualTo(100));

        manager.AddAffection(id, -30);
        Assert.That(manager.GetAffection(id), Is.EqualTo(70));

        manager.AddAffection(id, -200);
        Assert.That(manager.GetAffection(id), Is.Zero);

        manager.AddAffection(id, 5);
        Assert.That(manager.GetAffection(id), Is.EqualTo(5));
    }

    // 정의되지 않은 캐릭터는 조회와 양수·음수·0 증감 모두에서 거부하고 세 캐릭터의 기존 값을 유지하는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public void AffectionOperations_WithUndefinedCharacter_ThrowAndPreserveAllValues(int value)
    {
        CharacterId invalidId = (CharacterId)value;

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetAffection(invalidId));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AddAffection(invalidId, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AddAffection(invalidId, -10));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AddAffection(invalidId, 0));

        Assert.That(data.chaerin, Is.EqualTo(20));
        Assert.That(data.soeun, Is.EqualTo(50));
        Assert.That(data.seogyeong, Is.EqualTo(80));
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AffectionManager(null));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 값을 조회하고 기존 데이터에 증감을 반영할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentData()
    {
        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(manager.GetAffection(CharacterId.Chaerin), Is.EqualTo(20));
        Assert.That(manager.GetAffection(CharacterId.Soeun), Is.EqualTo(50));
        Assert.That(manager.GetAffection(CharacterId.Seogyeong), Is.EqualTo(80));

        manager.AddAffection(CharacterId.Chaerin, 5);

        Assert.That(data.chaerin, Is.EqualTo(25));
        Assert.That(data.soeun, Is.EqualTo(50));
        Assert.That(data.seogyeong, Is.EqualTo(80));
    }

    // 데이터 교체 후 세 캐릭터의 조회가 모두 새 값을 사용하고 증감은 새 데이터의 해당 캐릭터에만 반영되는지 확인한다.
    [TestCase(CharacterId.Chaerin)]
    [TestCase(CharacterId.Soeun)]
    [TestCase(CharacterId.Seogyeong)]
    public void LoadData_WithNewData_ReplacesReadAndWriteTarget(CharacterId id)
    {
        AffectionData loadedData = new AffectionData { chaerin = 10, soeun = 40, seogyeong = 70 };

        manager.LoadData(loadedData);

        Assert.That(manager.GetAffection(CharacterId.Chaerin), Is.EqualTo(10));
        Assert.That(manager.GetAffection(CharacterId.Soeun), Is.EqualTo(40));
        Assert.That(manager.GetAffection(CharacterId.Seogyeong), Is.EqualTo(70));

        manager.AddAffection(id, 5);

        Assert.That(loadedData.chaerin, Is.EqualTo(id == CharacterId.Chaerin ? 15 : 10));
        Assert.That(loadedData.soeun, Is.EqualTo(id == CharacterId.Soeun ? 45 : 40));
        Assert.That(loadedData.seogyeong, Is.EqualTo(id == CharacterId.Seogyeong ? 75 : 70));
        Assert.That(manager.GetAffection(CharacterId.Chaerin), Is.EqualTo(loadedData.chaerin));
        Assert.That(manager.GetAffection(CharacterId.Soeun), Is.EqualTo(loadedData.soeun));
        Assert.That(manager.GetAffection(CharacterId.Seogyeong), Is.EqualTo(loadedData.seogyeong));
        Assert.That(data.chaerin, Is.EqualTo(20));
        Assert.That(data.soeun, Is.EqualTo(50));
        Assert.That(data.seogyeong, Is.EqualTo(80));
    }
}
