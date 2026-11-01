using System;

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