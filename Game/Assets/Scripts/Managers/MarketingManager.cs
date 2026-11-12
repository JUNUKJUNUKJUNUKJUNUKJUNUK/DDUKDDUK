using System;

// 마케팅의 주간 사용 제한과 적용일을 관리한다.
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

        // 데이터 교체 시 중복 구독되지 않도록 생성 시 한 번만 연결한다.
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

        // 계산 실패 시 사용 상태를 유지하도록 적용일을 먼저 구한다.
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

    // 1·8·15일차에 사용 제한만 해제하고 최초 사용 이력과 적용일은 유지한다.
    private void HandleDayChanged(int day)
    {
        if (day % 7 != 1)
            return;

        data.chaerin.usedThisWeek = false;
        data.soeun.usedThisWeek = false;
        data.seogyeong.usedThisWeek = false;
    }

    // 1일차를 월요일로 보고 토·일요일을 건너뛴다.
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