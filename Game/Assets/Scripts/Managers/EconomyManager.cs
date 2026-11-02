using System;

// 자금의 조회·추가·지출을 관리한다.
public class EconomyManager
{
    private EconomyData data;

    public EconomyManager(EconomyData data)
    {
        LoadData(data);
    }

    public void LoadData(EconomyData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));
    }

    public int GetMoney()
    {
        return data.money;
    }

    public void AddMoney(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        // 정수 범위를 넘으면 잔액을 변경하지 않고 예외로 거부한다.
        data.money = checked(data.money + amount);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || data.money < amount)
            return false;

        data.money -= amount;
        return true;
    }
}