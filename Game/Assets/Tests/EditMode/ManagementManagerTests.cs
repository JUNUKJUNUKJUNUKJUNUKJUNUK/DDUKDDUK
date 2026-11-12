using System;
using System.Collections.Generic;
using NUnit.Framework;

// 평판 증가와 떡 재고의 생산·판매, 데이터 보호 및 데이터 교체를 검증한다.
public class ManagementManagerTests
{
    private ManagementData data;
    private ManagementManager manager;

    // TestCaseSource가 모든 떡 종류에 동일한 검증을 각각 실행하도록 입력 목록을 제공한다.
    private static Array RiceCakeTypes => Enum.GetValues(typeof(RiceCakeType));

    // 각 테스트가 서로 영향을 주지 않도록 평판이 0이고 재고가 없는 새 데이터와 Manager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new ManagementData();
        manager = new ManagementManager(data);
    }

    // 기본 데이터로 시작하면 초기 평판이 0인지 확인한다.
    [Test]
    public void Constructor_WithDefaultData_StartsWithZeroReputation()
    {
        Assert.That(manager.GetReputation(), Is.Zero);
        Assert.That(data.reputation, Is.Zero);
    }

    // 평판이 0, 일반 값, int 최댓값인 데이터로 생성하면 저장된 평판을 그대로 조회하는지 확인한다.
    [TestCase(0)]
    [TestCase(35)]
    [TestCase(int.MaxValue)]
    public void GetReputation_ReturnsProvidedValue(int reputation)
    {
        manager = new ManagementManager(new ManagementData { reputation = reputation });

        Assert.That(manager.GetReputation(), Is.EqualTo(reputation));
    }

    // 0 추가, 일반 증가, int 최댓값에 도달하는 증가가 조회값과 원본 데이터에 정확히 반영되는지 확인한다.
    [TestCase(0, 0, 0)]
    [TestCase(20, 0, 20)]
    [TestCase(0, 15, 15)]
    [TestCase(20, 15, 35)]
    [TestCase(0, int.MaxValue, int.MaxValue)]
    [TestCase(2147483646, 1, int.MaxValue)]
    public void AddReputation_WithNonNegativeAmount_UpdatesReputation(int initial, int amount, int expected)
    {
        data.reputation = initial;

        manager.AddReputation(amount);

        Assert.That(manager.GetReputation(), Is.EqualTo(expected));
        Assert.That(data.reputation, Is.EqualTo(expected));
    }

    // 음수 평판 추가는 예외로 거부하고 기존 평판을 유지하는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void AddReputation_WithNegativeAmount_ThrowsAndPreservesReputation(int amount)
    {
        data.reputation = 35;

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AddReputation(amount));

        Assert.That(manager.GetReputation(), Is.EqualTo(35));
        Assert.That(data.reputation, Is.EqualTo(35));
    }

    // 평판 합계가 int 범위를 넘으면 OverflowException이 발생하고 기존 평판을 유지하는지 확인한다.
    [TestCase(int.MaxValue, 1)]
    [TestCase(1, int.MaxValue)]
    public void AddReputation_WhenValueWouldOverflow_ThrowsAndPreservesReputation(int initial, int amount)
    {
        data.reputation = initial;

        Assert.Throws<OverflowException>(() => manager.AddReputation(amount));

        Assert.That(manager.GetReputation(), Is.EqualTo(initial));
        Assert.That(data.reputation, Is.EqualTo(initial));
    }

    // 여러 번 추가한 평판이 이전 결과에 누적되는지 확인한다.
    [Test]
    public void AddReputation_Repeatedly_AccumulatesReputation()
    {
        manager.AddReputation(10);
        manager.AddReputation(20);
        manager.AddReputation(5);

        Assert.That(manager.GetReputation(), Is.EqualTo(35));
        Assert.That(data.reputation, Is.EqualTo(35));
    }

    // 데이터 교체 후 평판 조회와 추가는 새 데이터에 반영되고 이전 데이터의 평판은 유지되는지 확인한다.
    [Test]
    public void LoadData_WithNewData_ReplacesReputationReadAndWriteTarget()
    {
        data.reputation = 10;
        ManagementData loadedData = new ManagementData { reputation = 40 };

        manager.LoadData(loadedData);
        Assert.That(manager.GetReputation(), Is.EqualTo(40));

        manager.AddReputation(5);

        Assert.That(manager.GetReputation(), Is.EqualTo(45));
        Assert.That(loadedData.reputation, Is.EqualTo(45));
        Assert.That(data.reputation, Is.EqualTo(10));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 평판을 조회하고 추가할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentReputation()
    {
        data.reputation = 35;

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));
        Assert.That(manager.GetReputation(), Is.EqualTo(35));

        manager.AddReputation(5);

        Assert.That(manager.GetReputation(), Is.EqualTo(40));
        Assert.That(data.reputation, Is.EqualTo(40));
    }

    // 평판 추가는 재고를 변경하지 않고 생산량 설정과 판매도 평판을 변경하지 않는지 확인한다.
    [Test]
    public void ReputationAndStockOperations_KeepTheirValuesIndependent()
    {
        data.reputation = 10;
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);
        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 3), Is.True);
        Assert.That(manager.GetReputation(), Is.EqualTo(10));

        manager.AddReputation(5);

        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(7));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));

        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 20);
        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 5), Is.True);

        Assert.That(manager.GetReputation(), Is.EqualTo(15));
        Assert.That(data.reputation, Is.EqualTo(15));
    }

    // 모든 떡 종류에 대해 미등록 재고를 조회하면 두 수량이 0이고 재고 항목이 생성되지 않는지 확인한다.
    [TestCaseSource(nameof(RiceCakeTypes))]
    public void GetQuantities_WithUnregisteredType_ReturnsZeroWithoutCreatingStock(RiceCakeType type)
    {
        Assert.That(manager.GetPreparedQuantity(type), Is.Zero);
        Assert.That(manager.GetRemainingQuantity(type), Is.Zero);
        Assert.That(data.riceCakeStocks, Is.Empty);
    }

    // 모든 떡 종류에 대해 생산량 설정 시 해당 종류의 재고가 하나 생성되고 생산량과 남은 수량이 함께 설정되는지 확인한다.
    [TestCaseSource(nameof(RiceCakeTypes))]
    public void SetPreparedQuantity_WithNewType_SetsBothQuantities(RiceCakeType type)
    {
        manager.SetPreparedQuantity(type, 10);

        Assert.That(manager.GetPreparedQuantity(type), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(type), Is.EqualTo(10));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));
        Assert.That(data.riceCakeStocks[0].type, Is.EqualTo(type));
    }

    // 생산량의 경계값인 0과 int 최댓값을 두 수량에 정확히 저장하는지 확인한다.
    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public void SetPreparedQuantity_WithBoundaryQuantity_StoresExactQuantity(int quantity)
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, quantity);

        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(quantity));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(quantity));
    }

    // 판매 후 생산량을 다시 설정하면 기존 항목을 재사용하고 두 수량을 새 값으로 덮어쓰는지 확인한다.
    [TestCase(0)]
    [TestCase(6)]
    [TestCase(20)]
    public void SetPreparedQuantity_AfterSale_ReplacesBothQuantitiesWithoutDuplicateStock(int quantity)
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);
        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 3), Is.True);
        RiceCakeStock originalStock = data.riceCakeStocks[0];

        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, quantity);

        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(quantity));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(quantity));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));
        Assert.That(data.riceCakeStocks[0], Is.SameAs(originalStock));
    }

    // 모든 떡 종류에 대해 일부 판매가 성공하고 생산량은 유지되며 남은 수량만 감소하는지 확인한다.
    [TestCaseSource(nameof(RiceCakeTypes))]
    public void TrySellRiceCake_WithEnoughStock_OnlyDeductsRemainingQuantity(RiceCakeType type)
    {
        manager.SetPreparedQuantity(type, 10);

        Assert.That(manager.TrySellRiceCake(type, 3), Is.True);
        Assert.That(manager.GetPreparedQuantity(type), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(type), Is.EqualTo(7));
    }

    // 모든 떡 종류에 대해 전량 판매 후 남은 수량이 0이 되고 추가 판매는 거부되는지 확인한다.
    [TestCaseSource(nameof(RiceCakeTypes))]
    public void TrySellRiceCake_WithEntireStock_SucceedsThenRejectsFurtherSale(RiceCakeType type)
    {
        manager.SetPreparedQuantity(type, 10);

        Assert.That(manager.TrySellRiceCake(type, 10), Is.True);
        Assert.That(manager.TrySellRiceCake(type, 1), Is.False);
        Assert.That(manager.GetPreparedQuantity(type), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(type), Is.Zero);
    }

    // int 최댓값의 재고도 전량 판매할 수 있고 생산량은 유지되며 남은 수량은 0이 되는지 확인한다.
    [Test]
    public void TrySellRiceCake_WithMaximumStock_CanSellEntireStock()
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, int.MaxValue);

        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, int.MaxValue), Is.True);
        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(int.MaxValue));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.Zero);
    }

    // 0 이하이거나 남은 재고보다 큰 판매 수량은 false를 반환하고 두 수량과 항목 수를 유지하는지 확인한다.
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    [TestCase(11)]
    [TestCase(int.MaxValue)]
    public void TrySellRiceCake_WithInvalidOrInsufficientQuantity_FailsAndPreservesStock(int quantity)
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);

        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, quantity), Is.False);
        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));
    }

    // 모든 떡 종류에 대해 미등록 재고 판매는 실패하고 재고 항목을 새로 만들지 않는지 확인한다.
    [TestCaseSource(nameof(RiceCakeTypes))]
    public void TrySellRiceCake_WithUnregisteredType_FailsWithoutCreatingStock(RiceCakeType type)
    {
        Assert.That(manager.TrySellRiceCake(type, 1), Is.False);
        Assert.That(data.riceCakeStocks, Is.Empty);
    }

    // 모든 떡을 등록한 뒤 한 종류의 생산량과 판매량을 바꿔도 다른 종류의 수량은 유지되는지 확인한다.
    [Test]
    public void StockOperations_WithMultipleTypes_KeepQuantitiesIndependent()
    {
        foreach (RiceCakeType type in RiceCakeTypes)
            manager.SetPreparedQuantity(type, 10);

        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 20);
        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 3), Is.True);

        foreach (RiceCakeType type in RiceCakeTypes)
        {
            bool isTarget = type == RiceCakeType.Garaetteok;
            Assert.That(manager.GetPreparedQuantity(type), Is.EqualTo(isTarget ? 20 : 10));
            Assert.That(manager.GetRemainingQuantity(type), Is.EqualTo(isTarget ? 17 : 10));
        }

        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(RiceCakeTypes.Length));
    }

    // 음수 생산량 설정은 기존 종류와 미등록 종류 모두에서 예외로 거부하고 기존 재고를 유지하는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void SetPreparedQuantity_WithNegativeQuantity_ThrowsAndPreservesStock(int quantity)
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => manager.SetPreparedQuantity(RiceCakeType.Garaetteok, quantity));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => manager.SetPreparedQuantity(RiceCakeType.Jeolpyeon, quantity));

        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));
    }

    // 정의되지 않은 떡 종류는 조회·생산·판매 모두에서 예외로 거부하고 기존 재고를 유지하는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public void StockOperations_WithUndefinedType_ThrowAndPreserveStock(int value)
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);
        RiceCakeType invalidType = (RiceCakeType)value;

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetPreparedQuantity(invalidType));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetRemainingQuantity(invalidType));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.SetPreparedQuantity(invalidType, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.TrySellRiceCake(invalidType, 1));

        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(10));
        Assert.That(data.riceCakeStocks.Count, Is.EqualTo(1));
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ManagementManager(null));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 재고로 판매를 계속할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentStock()
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);

        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 3), Is.True);
        Assert.That(data.riceCakeStocks[0].remainingQuantity, Is.EqualTo(7));
    }

    // 현재 Manager의 초기화 동작에 따라 null 재고 목록을 빈 목록으로 복구하고 생산을 진행할 수 있는지 확인한다.
    [Test]
    public void Constructor_WithNullStockList_InitializesUsableEmptyList()
    {
        ManagementData loadedData = new ManagementData { riceCakeStocks = null };
        ManagementManager loadedManager = new ManagementManager(loadedData);

        Assert.That(loadedData.riceCakeStocks, Is.Not.Null.And.Empty);
        loadedManager.SetPreparedQuantity(RiceCakeType.Garaetteok, 5);
        Assert.That(loadedManager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(5));
    }

    // 현재 Manager의 데이터 교체 동작에 따라 null 재고 목록을 복구하고 새 데이터에만 생산 결과를 반영하는지 확인한다.
    [Test]
    public void LoadData_WithNullStockList_InitializesUsableEmptyList()
    {
        ManagementData loadedData = new ManagementData { riceCakeStocks = null };

        manager.LoadData(loadedData);

        Assert.That(loadedData.riceCakeStocks, Is.Not.Null.And.Empty);
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 5);
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(5));
        Assert.That(data.riceCakeStocks, Is.Empty);
    }

    // 데이터 교체 시 저장된 생산량과 남은 수량을 보존하고 이후 판매·생산은 새 데이터에만 반영되는지 확인한다.
    [Test]
    public void LoadData_WithExistingStock_ReplacesReadAndWriteTargetWithoutResettingQuantities()
    {
        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 10);
        ManagementData loadedData = new ManagementData
        {
            riceCakeStocks = new List<RiceCakeStock>
            {
                new RiceCakeStock
                {
                    type = RiceCakeType.Garaetteok,
                    preparedQuantity = 20,
                    remainingQuantity = 12
                }
            }
        };

        manager.LoadData(loadedData);
        Assert.That(manager.GetPreparedQuantity(RiceCakeType.Garaetteok), Is.EqualTo(20));
        Assert.That(manager.GetRemainingQuantity(RiceCakeType.Garaetteok), Is.EqualTo(12));

        Assert.That(manager.TrySellRiceCake(RiceCakeType.Garaetteok, 2), Is.True);
        Assert.That(loadedData.riceCakeStocks[0].remainingQuantity, Is.EqualTo(10));
        Assert.That(data.riceCakeStocks[0].remainingQuantity, Is.EqualTo(10));

        manager.SetPreparedQuantity(RiceCakeType.Garaetteok, 7);
        Assert.That(loadedData.riceCakeStocks[0].preparedQuantity, Is.EqualTo(7));
        Assert.That(loadedData.riceCakeStocks[0].remainingQuantity, Is.EqualTo(7));
        Assert.That(data.riceCakeStocks[0].preparedQuantity, Is.EqualTo(10));
        Assert.That(data.riceCakeStocks[0].remainingQuantity, Is.EqualTo(10));
    }
}
