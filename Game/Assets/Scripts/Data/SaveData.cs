using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string savedAt;
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

[Serializable]
public class CharacterMarketingData
{
    public bool usedThisWeek;
    public bool firstUsed;
    public int applyDay;
}

[Serializable]
public class MarketingData
{
    public CharacterMarketingData chaerin = new CharacterMarketingData();
    public CharacterMarketingData soeun = new CharacterMarketingData();
    public CharacterMarketingData seogyeong = new CharacterMarketingData();
}

[Serializable]
public class AffectionData
{
    public int chaerin;
    public int soeun;
    public int seogyeong;
}

[Serializable]
public class StoryData
{
    public List<string> completedEventIds = new List<string>();
    public string currentRoute;

    public string currentEventId;
    public int lineIndex = -1;
    public List<int> choiceHistory = new List<int>();
}