using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// 실제 파일 읽기 결과를 GameManager에 전달해 저장 왕복과 실패 시 게임 상태 보존을 검증한다.
public class SaveFileServiceIntegrationTests
{
    private string testDirectory;
    private SaveFileService service;
    private GameObject owner;
    private GameManager manager;

    // 실제 플레이어 파일 대신 독립된 임시 경로와 테스트 전용 GameManager를 준비한다.
    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManager.Instance == null, Is.True, "기존 GameManager가 있으면 상태를 변경하지 않습니다.");
        testDirectory = Path.Combine(Path.GetTempPath(), "DDUKDDUK_SaveFileIntegration_" + Guid.NewGuid().ToString("N"));
        service = new SaveFileService(testDirectory);
        owner = new GameObject("SaveFileServiceIntegrationTests");
        manager = owner.AddComponent<GameManager>();
        manager.enabled = false;
        SaveData state = new SaveData { savedAt = "2026-11-02T00:00:00Z", playTime = 123.5f };
        state.phase.day = 4;
        state.economy.money = 12345;
        state.management.reputation = 12;
        state.management.riceCakeStocks.Add(new RiceCakeStock
        {
            type = RiceCakeType.Garaetteok, preparedQuantity = 10, remainingQuantity = 7
        });
        state.research.completedResearchIds.Add("RCP-01");
        state.marketing.chaerin.firstUsed = true;
        state.marketing.chaerin.applyDay = 5;
        state.affection.chaerin = 30;
        state.story.currentEventId = "event-01";
        state.story.lineIndex = 3;
        state.story.choiceHistory.Add(1);
        manager.LoadState(state);
    }

    // 테스트 전용 객체와 임시 저장 경로만 정리한다.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (owner != null) UnityEngine.Object.Destroy(owner);
        if (testDirectory != null && Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);
        yield return null;
    }

    // 파일 저장 후 게임 상태를 바꿔도 파일을 읽어 적용하면 전체 저장 내용이 복원되는지 확인한다.
    [Test]
    public void SaveAndLoad_ThroughRealFile_RestoresGameState()
    {
        string expected = manager.GetSaveJson();
        service.Save(1, expected);
        manager.LoadState(new SaveData());

        manager.LoadJson(service.Load(1));

        Assert.That(manager.GetSaveJson(), Is.EqualTo(expected));
        Assert.That(manager.Economy.GetMoney(), Is.EqualTo(12345));
        Assert.That(manager.Research.IsResearchCompleted("RCP-01"), Is.True);
        Assert.That(manager.Affection.GetAffection(CharacterId.Chaerin), Is.EqualTo(30));
    }

    // 파일 읽기 실패로 JSON 적용까지 도달하지 못하면 현재 게임 상태가 유지되는지 확인한다.
    [Test]
    public void Load_WithMissingFile_PreservesGameState()
    {
        string expected = manager.GetSaveJson();

        Assert.Catch<IOException>(() => manager.LoadJson(service.Load(1)));

        Assert.That(manager.GetSaveJson(), Is.EqualTo(expected));
        Assert.That(Directory.Exists(testDirectory), Is.False);
    }

    // 파일 내용의 JSON 형식이 잘못되면 게임 상태와 원래 파일 내용이 모두 유지되는지 확인한다.
    [Test]
    public void Load_WithMalformedJson_PreservesGameStateAndFile()
    {
        string expected = manager.GetSaveJson();
        const string invalid = "{ broken json";
        service.Save(1, invalid);

        Assert.Catch<ArgumentException>(() => manager.LoadJson(service.Load(1)));

        Assert.That(manager.GetSaveJson(), Is.EqualTo(expected));
        Assert.That(service.Load(1), Is.EqualTo(invalid));
    }

    // 파일의 JSON 형식이 정상이더라도 저장값 검증에 실패하면 게임 상태와 원본 파일이 유지되는지 확인한다.
    [Test]
    public void Load_WithInvalidSaveValues_PreservesGameStateAndFile()
    {
        string expected = manager.GetSaveJson();
        SaveData invalidData = new SaveData();
        invalidData.economy.money = -1;
        string invalid = JsonUtility.ToJson(invalidData, true);
        service.Save(1, invalid);

        Assert.Catch<ArgumentException>(() => manager.LoadJson(service.Load(1)));

        Assert.That(manager.GetSaveJson(), Is.EqualTo(expected));
        Assert.That(service.Load(1), Is.EqualTo(invalid));
    }
}
