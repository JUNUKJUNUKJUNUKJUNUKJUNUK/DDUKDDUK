using System;
using System.Collections.Generic;

// 평판과 종류별 떡의 생산량·남은 재고를 관리한다.
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

    // 생산량을 다시 확정하면 판매 후 남은 재고도 해당 수량으로 덮어쓴다.
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

    // 판매는 남은 재고만 차감하며 자금 정산은 호출 측에서 처리한다.
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