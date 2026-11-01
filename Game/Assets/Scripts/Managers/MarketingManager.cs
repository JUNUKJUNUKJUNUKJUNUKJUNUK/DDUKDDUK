using System;

public class MarketingManager
{
    public const string ChaerinId = "MKT-CH";
    public const string SoeunId = "MKT-SE";

    private MarketingData data;
    private readonly PhaseManager phaseManager;

    public MarketingManager(MarketingData data, PhaseManager phaseManager)
    {
        this.phaseManager = phaseManager
            ?? throw new ArgumentNullException(nameof(phaseManager));

        LoadData(data);

        phaseManager.OnDayChanged += HandleDayChanged;
    }

    public void LoadData(MarketingData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));

        if (data.chaerin == null)
            data.chaerin = new CharacterMarketingData();

        if (data.soeun == null)
            data.soeun = new CharacterMarketingData();

        if (data.seogyeong == null)
            data.seogyeong = new CharacterMarketingData();
    }

    public bool IsMarketingUsedThisWeek(string marketingId)
    {
        return GetMarketingData(marketingId).usedThisWeek;
    }

    public bool IsMarketingFirstUsed(string marketingId)
    {
        return GetMarketingData(marketingId).firstUsed;
    }

    public void UseMarketing(string marketingId)
    {
        CharacterMarketingData marketing = GetMarketingData(marketingId);

        if (marketing.usedThisWeek)
        {
            throw new InvalidOperationException(
                "이번 주에 이미 사용한 마케팅입니다.");
        }

        int applyDay = GetNextBusinessDay(phaseManager.GetDay());

        marketing.usedThisWeek = true;
        marketing.firstUsed = true;
        marketing.applyDay = applyDay;
    }

    public bool IsMarketingActive(string marketingId)
    {
        CharacterMarketingData marketing = GetMarketingData(marketingId);

        return marketing.applyDay > 0
            && marketing.applyDay == phaseManager.GetDay();
    }

    private CharacterMarketingData GetMarketingData(string marketingId)
    {
        switch (marketingId)
        {
            case ChaerinId:
                return data.chaerin;

            case SoeunId:
                return data.soeun;

            default:
                throw new ArgumentException(
                    $"등록되지 않은 마케팅 ID: {marketingId}",
                    nameof(marketingId));
        }
    }

    private void HandleDayChanged(int day)
    {
        if (day % 7 != 1)
            return;

        data.chaerin.usedThisWeek = false;
        data.soeun.usedThisWeek = false;
        data.seogyeong.usedThisWeek = false;
    }

    private static int GetNextBusinessDay(int currentDay)
    {
        int weekday = (currentDay - 1) % 7;

        if (weekday == 4)
            return checked(currentDay + 3);

        if (weekday == 5)
            return checked(currentDay + 2);

        return checked(currentDay + 1);
    }
}