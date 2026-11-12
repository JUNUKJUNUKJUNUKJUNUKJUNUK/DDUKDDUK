using System;
using NUnit.Framework;

// 자금 조회·입출금의 정상 동작, 오류 시 잔액 보존, 데이터 교체를 검증한다.
public class EconomyManagerTests
{
    private EconomyData data;
    private EconomyManager manager;

    // 각 테스트가 서로 영향을 주지 않도록 잔액 100인 새 데이터와 Manager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        data = new EconomyData { money = 100 };
        manager = new EconomyManager(data);
    }

    // 기본 EconomyData로 시작하면 초기 자금이 100,000원인지 확인한다.
    [Test]
    public void Constructor_WithDefaultData_StartsWithInitialMoney()
    {
        Assert.That(new EconomyManager(new EconomyData()).GetMoney(), Is.EqualTo(100000));
    }

    // 잔액이 0, 일반 금액, int 최댓값일 때 전달된 값을 그대로 조회하는지 확인한다.
    [TestCase(0)]
    [TestCase(100)]
    [TestCase(int.MaxValue)]
    public void GetMoney_ReturnsProvidedBalance(int money)
    {
        data.money = money;
        Assert.That(manager.GetMoney(), Is.EqualTo(money));
    }

    // 0원 추가, 일반 입금, int 최댓값까지의 입금이 조회값과 원본 데이터에 정확히 반영되는지 확인한다.
    [TestCase(0, 100)]
    [TestCase(25, 125)]
    [TestCase(2147483547, int.MaxValue)]
    public void AddMoney_WithNonNegativeAmount_UpdatesBalance(int amount, int expected)
    {
        manager.AddMoney(amount);

        Assert.That(manager.GetMoney(), Is.EqualTo(expected));
        Assert.That(data.money, Is.EqualTo(expected));
    }

    // 음수 입금은 예외로 거부하고 기존 잔액을 변경하지 않는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void AddMoney_WithNegativeAmount_ThrowsAndPreservesBalance(int amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AddMoney(amount));
        Assert.That(data.money, Is.EqualTo(100));
    }

    // 입금 결과가 int 범위를 넘으면 OverflowException이 발생하고 잔액이 보존되는지 확인한다.
    [Test]
    public void AddMoney_WhenBalanceWouldOverflow_ThrowsAndPreservesBalance()
    {
        data.money = int.MaxValue;

        Assert.Throws<OverflowException>(() => manager.AddMoney(1));
        Assert.That(data.money, Is.EqualTo(int.MaxValue));
    }

    // 0원 지출, 일부 지출, 전액 지출이 성공하고 잔액이 정확히 차감되는지 확인한다.
    [TestCase(0, 100)]
    [TestCase(25, 75)]
    [TestCase(100, 0)]
    public void TrySpendMoney_WithAffordableAmount_SucceedsAndDeductsBalance(int amount, int expected)
    {
        Assert.That(manager.TrySpendMoney(amount), Is.True);
        Assert.That(manager.GetMoney(), Is.EqualTo(expected));
        Assert.That(data.money, Is.EqualTo(expected));
    }

    // 음수 지출이나 잔액을 초과하는 지출은 false를 반환하고 잔액을 유지하는지 확인한다.
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    [TestCase(101)]
    [TestCase(int.MaxValue)]
    public void TrySpendMoney_WithInvalidOrUnaffordableAmount_FailsAndPreservesBalance(int amount)
    {
        Assert.That(manager.TrySpendMoney(amount), Is.False);
        Assert.That(data.money, Is.EqualTo(100));
    }

    // 잔액이 0이면 0원 지출만 성공하고 양수 지출은 실패하며 잔액은 0으로 유지되는지 확인한다.
    [Test]
    public void TrySpendMoney_WithZeroBalance_OnlyAllowsZeroSpend()
    {
        data.money = 0;

        Assert.That(manager.TrySpendMoney(0), Is.True);
        Assert.That(manager.TrySpendMoney(1), Is.False);
        Assert.That(data.money, Is.Zero);
    }

    // int 최댓값의 잔액도 전액 지출할 수 있고 결과가 정확히 0인지 확인한다.
    [Test]
    public void TrySpendMoney_WithMaximumBalance_CanSpendEntireBalance()
    {
        data.money = int.MaxValue;

        Assert.That(manager.TrySpendMoney(int.MaxValue), Is.True);
        Assert.That(data.money, Is.Zero);
    }

    // 데이터 없이 Manager를 생성하면 ArgumentNullException으로 거부하는지 확인한다.
    [Test]
    public void Constructor_WithNullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EconomyManager(null));
    }

    // null 데이터 교체를 거부한 뒤에도 기존 데이터로 자금 변경을 계속할 수 있는지 확인한다.
    [Test]
    public void LoadData_WithNullData_ThrowsAndPreservesCurrentData()
    {
        Assert.Throws<ArgumentNullException>(() => manager.LoadData(null));

        manager.AddMoney(10);
        Assert.That(manager.GetMoney(), Is.EqualTo(110));
        Assert.That(data.money, Is.EqualTo(110));
    }

    // 데이터 교체 후 조회와 입출금은 새 데이터에 반영되고 이전 데이터는 유지되는지 확인한다.
    [Test]
    public void LoadData_WithNewData_ReplacesReadAndWriteTarget()
    {
        EconomyData loadedData = new EconomyData { money = 250 };

        manager.LoadData(loadedData);
        Assert.That(manager.GetMoney(), Is.EqualTo(250));

        manager.AddMoney(50);
        Assert.That(manager.TrySpendMoney(20), Is.True);

        Assert.That(manager.GetMoney(), Is.EqualTo(280));
        Assert.That(loadedData.money, Is.EqualTo(280));
        Assert.That(data.money, Is.EqualTo(100));
    }
}
