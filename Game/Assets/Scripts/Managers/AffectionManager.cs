using System;

public class AffectionManager
{
    private AffectionData data;

    public AffectionManager(AffectionData data)
    {
        LoadData(data);
    }

    public void LoadData(AffectionData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));
    }

    public int GetAffection(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Chaerin:
                return data.chaerin;

            case CharacterId.Soeun:
                return data.soeun;

            case CharacterId.Seogyeong:
                return data.seogyeong;

            default:
                throw new ArgumentOutOfRangeException(nameof(id));
        }
    }

    public void AddAffection(CharacterId id, int amount)
    {
        long sum = (long)GetAffection(id) + amount;
        int newAffection = (int)Math.Max(0L, Math.Min(100L, sum));

        switch (id)
        {
            case CharacterId.Chaerin:
                data.chaerin = newAffection;
                break;

            case CharacterId.Soeun:
                data.soeun = newAffection;
                break;

            case CharacterId.Seogyeong:
                data.seogyeong = newAffection;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(id));
        }
    }
}