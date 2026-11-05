using System;

// 캐릭터별 호감도를 관리하고 증감 결과를 0~100으로 제한한다.
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
        // 극단적인 변화량도 오버플로 없이 계산한 뒤 범위를 제한한다.
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