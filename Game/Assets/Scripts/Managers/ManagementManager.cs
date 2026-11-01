using System;
using System.Collections.Generic;

public class ManagementManager
{
    private ManagementData data;

    public ManagementManager(ManagementData data)
    {
        LoadData(data);
    }

    public void LoadData(ManagementData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));

        if (data.riceCakeStocks == null)
            data.riceCakeStocks = new List<RiceCakeStock>();
    }

    // [평판]

    public int GetReputation()
    {
        return data.reputation;
    }

    public void AddReputation(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        data.reputation = checked(data.reputation + amount);
    }

    // [떡 재고]

    public int GetPreparedQuantity(RiceCakeType type)
    {
        ValidateType(type);

        RiceCakeStock stock = FindStock(type);
        return stock?.preparedQuantity ?? 0;
    }

    public int GetRemainingQuantity(RiceCakeType type)
    {
        ValidateType(type);

        RiceCakeStock stock = FindStock(type);
        return stock?.remainingQuantity ?? 0;
    }

    // 아침 생산 수량 확정 시 호출
    public void SetPreparedQuantity(RiceCakeType type, int quantity)
    {
        ValidateType(type);

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        RiceCakeStock stock = FindStock(type);

        if (stock == null)
        {
            stock = new RiceCakeStock { type = type };
            data.riceCakeStocks.Add(stock);
        }

        stock.preparedQuantity = quantity;
        stock.remainingQuantity = quantity;
    }

    // 판매 성공 시 남은 재고 차감
    public bool TrySellRiceCake(RiceCakeType type, int quantity)
    {
        ValidateType(type);

        if (quantity <= 0)
            return false;

        RiceCakeStock stock = FindStock(type);

        if (stock == null || stock.remainingQuantity < quantity)
            return false;

        stock.remainingQuantity -= quantity;
        return true;
    }

    private RiceCakeStock FindStock(RiceCakeType type)
    {
        return data.riceCakeStocks.Find(stock => stock.type == type);
    }

    private static void ValidateType(RiceCakeType type)
    {
        if (!Enum.IsDefined(typeof(RiceCakeType), type))
            throw new ArgumentOutOfRangeException(nameof(type));
    }
}