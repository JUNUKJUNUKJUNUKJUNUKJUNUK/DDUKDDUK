using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private SaveData saveData;

    public PhaseManager Phase { get; private set; }
    public EconomyManager Economy { get; private set; }
    public ManagementManager Management { get; private set; }
    public ResearchManager Research { get; private set; }
    public MarketingManager Marketing { get; private set; }
    public AffectionManager Affection { get; private set; }
    public StoryManager Story { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize(new SaveData());
    }

    private void Update()
    {
        if (saveData != null)
            saveData.playTime += Time.deltaTime;
    }

    private void Initialize(SaveData data)
    {
        saveData = data ?? throw new ArgumentNullException(nameof(data));

        Phase = new PhaseManager(saveData.phase);
        Economy = new EconomyManager(saveData.economy);
        Management = new ManagementManager(saveData.management);
        Research = new ResearchManager(saveData.research);
        Marketing = new MarketingManager(saveData.marketing, Phase);
        Affection = new AffectionManager(saveData.affection);
        Story = new StoryManager(saveData.story);
    }

    public void LoadState(SaveData loadedData)
    {
        if (loadedData == null)
            throw new ArgumentNullException(nameof(loadedData));

        EnsureSaveData(loadedData);
        ValidateSaveData(loadedData);

        saveData = loadedData;

        Economy.LoadData(saveData.economy);
        Management.LoadData(saveData.management);
        Research.LoadData(saveData.research);
        Marketing.LoadData(saveData.marketing);
        Affection.LoadData(saveData.affection);
        Story.LoadData(saveData.story);
        Phase.LoadData(saveData.phase);
    }

    private static void EnsureSaveData(SaveData data)
    {
        if (data.phase == null)
            data.phase = new PhaseData();

        if (data.economy == null)
            data.economy = new EconomyData();

        if (data.management == null)
            data.management = new ManagementData();

        if (data.research == null)
            data.research = new ResearchData();

        if (data.marketing == null)
            data.marketing = new MarketingData();

        if (data.affection == null)
            data.affection = new AffectionData();

        if (data.story == null)
            data.story = new StoryData();
    }

    // 모든 저장값을 확인한 뒤에만 현재 게임 상태를 교체할 수 있도록 비정상 데이터는 예외로 거부한다.
    private static void ValidateSaveData(SaveData data)
    {
        if (data.phase.day < 1)
            throw new ArgumentException("저장된 날짜는 1 이상이어야 합니다.", nameof(data));

        if (!Enum.IsDefined(typeof(GamePhase), data.phase.phase))
            throw new ArgumentException("저장된 시간대가 올바르지 않습니다.", nameof(data));

        if (data.economy.money < 0)
            throw new ArgumentException("저장된 자금은 음수일 수 없습니다.", nameof(data));

        if (data.management.reputation < 0)
            throw new ArgumentException("저장된 평판은 음수일 수 없습니다.", nameof(data));

        if (data.playTime < 0f)
            throw new ArgumentException("저장된 플레이 시간은 음수일 수 없습니다.", nameof(data));

        if (data.affection.chaerin < 0 || data.affection.chaerin > 100
            || data.affection.soeun < 0 || data.affection.soeun > 100
            || data.affection.seogyeong < 0 || data.affection.seogyeong > 100)
        {
            throw new ArgumentException("저장된 호감도는 0~100 범위여야 합니다.", nameof(data));
        }

        // 누락된 재고 목록은 기존 ManagementManager.LoadData에서 빈 목록으로 복구한다.
        if (data.management.riceCakeStocks == null)
            return;

        HashSet<RiceCakeType> stockTypes = new HashSet<RiceCakeType>();
        foreach (RiceCakeStock stock in data.management.riceCakeStocks)
        {
            if (stock == null)
                throw new ArgumentException("저장된 재고 목록에 빈 항목이 있습니다.", nameof(data));

            if (!Enum.IsDefined(typeof(RiceCakeType), stock.type))
                throw new ArgumentException("저장된 떡 종류가 올바르지 않습니다.", nameof(data));

            if (stock.preparedQuantity < 0 || stock.remainingQuantity < 0
                || stock.remainingQuantity > stock.preparedQuantity)
            {
                throw new ArgumentException("저장된 재고 수량이 올바르지 않습니다.", nameof(data));
            }

            if (!stockTypes.Add(stock.type))
                throw new ArgumentException("저장된 재고 목록에 같은 떡 종류가 중복되어 있습니다.", nameof(data));
        }
    }

    public string GetSaveJson()
    {
        return JsonUtility.ToJson(saveData, true);
    }

    public void LoadJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON이 비어 있습니다.", nameof(json));

        SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
        LoadState(loadedData);
    }
}