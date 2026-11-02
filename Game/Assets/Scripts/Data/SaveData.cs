using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string savedAt;
    // Time.timeScale이 반영된 누적 시간(초).
    public float playTime;

    public PhaseData phase = new PhaseData();
    public EconomyData economy = new EconomyData();
    public ManagementData management = new ManagementData();
    public ResearchData research = new ResearchData();
    public MarketingData marketing = new MarketingData();
    public AffectionData affection = new AffectionData();
    public StoryData story = new StoryData();
}

[Serializable]
public class PhaseData
{
    public int day = 1;
    public GamePhase phase = GamePhase.Morning;
}

[Serializable]
public class EconomyData
{
    public int money = 100000;
}

[Serializable]
public class RiceCakeStock
{
    public RiceCakeType type;
    // 판매 후에도 생산량은 유지하고 remainingQuantity만 차감한다.
    public int preparedQuantity;
    public int remainingQuantity;
}

[Serializable]
public class ManagementData
{
    public int reputation;
    public List<RiceCakeStock> riceCakeStocks = new List<RiceCakeStock>();
}

[Serializable]
public class ResearchData
{
    public List<string> completedResearchIds = new List<string>();
}

// 캐릭터 하나의 마케팅 사용 이력과 적용일.
[Serializable]
public class CharacterMarketingData
{
    // 주간 초기화는 usedThisWeek만 해제하고 firstUsed는 유지한다.
    public bool usedThisWeek;
    public bool firstUsed;
    // 0은 적용일 미지정.
    public int applyDay;
}

// 캐릭터별 마케팅 상태를 묶는다.
[Serializable]
public class MarketingData
{
    public CharacterMarketingData chaerin = new CharacterMarketingData();
    public CharacterMarketingData soeun = new CharacterMarketingData();
    // 서경은 데이터만 준비되어 있으며 마케팅 사용 기능은 미구현이다.
    public CharacterMarketingData seogyeong = new CharacterMarketingData();
}

// 캐릭터별 호감도를 저장한다. 유효 범위는 0~100.
[Serializable]
public class AffectionData
{
    public int chaerin;
    public int soeun;
    public int seogyeong;
}

// 완료 이벤트와 현재 대사 진행 위치를 저장한다.
[Serializable]
public class StoryData
{
    public List<string> completedEventIds = new List<string>();
    public string currentRoute;

    public string currentEventId;
    // -1은 진행 위치가 지정되지 않은 초기 상태.
    public int lineIndex = -1;
    public List<int> choiceHistory = new List<int>();
}