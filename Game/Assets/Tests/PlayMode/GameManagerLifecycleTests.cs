using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// GameManager의 초기화, 중복 제거, 씬 간 유지, 프레임별 시간 누적과 파괴 후 재생성을 검증한다.
public class GameManagerLifecycleTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private readonly List<Scene> createdScenes = new List<Scene>();
    private Scene originalActiveScene;
    private float originalTimeScale;

    // 기존 GameManager가 없는지 확인하고 테스트별 객체·씬 목록과 시간 배율 복구값을 준비한다.
    [SetUp]
    public void SetUp()
    {
        createdObjects.Clear();
        createdScenes.Clear();
        originalActiveScene = SceneManager.GetActiveScene();
        originalTimeScale = Time.timeScale;
        Assert.That(GameManager.Instance == null, Is.True, "기존 GameManager가 있으면 테스트가 상태를 변경하지 않습니다.");
        Time.timeScale = 1f;
    }

    // 테스트 전용 객체·씬을 제거하고 원래 활성 씬과 시간 배율을 복구해 다른 테스트에 영향을 주지 않도록 한다.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTimeScale;
        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
                UnityEngine.Object.Destroy(createdObject);
        }
        yield return null;

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
            SceneManager.SetActiveScene(originalActiveScene);

        foreach (Scene scene in createdScenes)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                    yield return unload;
            }
        }
    }

    // Awake 직후 첫 인스턴스가 등록되고 모든 Manager와 새 게임의 기본 상태가 초기화되는지 확인한다.
    [Test]
    public void Awake_WithFirstInstance_RegistersSingletonAndInitializesManagers()
    {
        GameManager manager = CreateManager();

        Assert.That(GameManager.Instance, Is.SameAs(manager));
        Assert.That(manager.Phase, Is.Not.Null);
        Assert.That(manager.Economy, Is.Not.Null);
        Assert.That(manager.Management, Is.Not.Null);
        Assert.That(manager.Research, Is.Not.Null);
        Assert.That(manager.Marketing, Is.Not.Null);
        Assert.That(manager.Affection, Is.Not.Null);
        Assert.That(manager.Story, Is.Not.Null);
        Assert.That(manager.GetSaveJson(), Is.EqualTo(JsonUtility.ToJson(new SaveData(), true)));
    }

    // 중복 GameManager는 프레임 종료 시 객체 전체가 제거되고 기존 인스턴스·Manager·진행 상태는 유지되는지 확인한다.
    [UnityTest]
    public IEnumerator Awake_WithDuplicateInstance_DestroysDuplicateAndPreservesOriginalState()
    {
        GameManager original = CreateManager();
        SaveData state = new SaveData { playTime = 12f };
        state.economy.money = 12345;
        state.phase.day = 4;
        original.LoadState(state);
        EconomyManager economy = original.Economy;
        string savedBeforeDuplicate = original.GetSaveJson();

        GameManager duplicate = CreateManager();
        GameObject duplicateObject = duplicate.gameObject;

        Assert.That(GameManager.Instance, Is.SameAs(original));

        yield return null;

        Assert.That(duplicate == null, Is.True);
        Assert.That(duplicateObject == null, Is.True);
        Assert.That(original != null, Is.True);
        Assert.That(GameManager.Instance, Is.SameAs(original));
        Assert.That(original.Economy, Is.SameAs(economy));
        Assert.That(original.GetSaveJson(), Is.EqualTo(savedBeforeDuplicate));
    }

    // 생성된 씬을 내리고 다른 씬을 활성화해도 동일 인스턴스와 진행 상태가 유지되는지 확인한다.
    [UnityTest]
    public IEnumerator SceneTransition_UnloadingSourceScene_PreservesManagerAndState()
    {
        Scene source = CreateTestScene("Source");
        SceneManager.SetActiveScene(source);
        GameManager manager = CreateManager();
        SaveData state = new SaveData();
        state.economy.money = 12345;
        state.phase.day = 4;
        manager.LoadState(state);
        string savedBeforeTransition = manager.GetSaveJson();

        Scene destination = CreateTestScene("Destination");
        SceneManager.SetActiveScene(destination);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(source);
        Assert.That(unload, Is.Not.Null);

        yield return unload;

        Assert.That(source.isLoaded, Is.False);
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(destination));
        Assert.That(manager != null, Is.True);
        Assert.That(GameManager.Instance, Is.SameAs(manager));
        Assert.That(manager.gameObject.scene, Is.Not.EqualTo(source));
        Assert.That(manager.GetSaveJson(), Is.EqualTo(savedBeforeTransition));
        Assert.That(manager.Economy.GetMoney(), Is.EqualTo(12345));
    }

    // 실제 프레임이 진행될 때 Update가 플레이 시간을 증가시키고 증가한 값이 저장 JSON에도 반영되는지 확인한다.
    [UnityTest]
    public IEnumerator Update_WhileEnabled_IncreasesSavedPlayTime()
    {
        GameManager manager = CreateManager();
        manager.enabled = true;
        float initialPlayTime = ReadSavedData(manager).playTime;

        yield return null;
        yield return null;

        manager.enabled = false;
        Assert.That(ReadSavedData(manager).playTime, Is.GreaterThan(initialPlayTime));
    }

    // Update가 비활성화되어 있으면 프레임이 지나도 플레이 시간이 증가하지 않는지 확인한다.
    [UnityTest]
    public IEnumerator Update_WhileDisabled_PreservesPlayTime()
    {
        GameManager manager = CreateManager();
        float initialPlayTime = ReadSavedData(manager).playTime;

        yield return null;
        yield return null;

        Assert.That(ReadSavedData(manager).playTime, Is.EqualTo(initialPlayTime));
    }

    // 시간 배율이 0이면 프레임은 진행되어도 Time.deltaTime 기반의 플레이 시간이 증가하지 않는지 확인한다.
    [UnityTest]
    public IEnumerator Update_WithZeroTimeScale_PreservesPlayTime()
    {
        GameManager manager = CreateManager();
        manager.enabled = true;
        Time.timeScale = 0f;
        yield return null;
        float pausedPlayTime = ReadSavedData(manager).playTime;

        yield return null;
        yield return null;

        manager.enabled = false;
        Assert.That(ReadSavedData(manager).playTime, Is.EqualTo(pausedPlayTime));
    }

    // 불러오기 후에는 새 데이터의 플레이 시간만 증가하고 이전 데이터의 시간은 더 이상 변경되지 않는지 확인한다.
    [UnityTest]
    public IEnumerator Update_AfterLoadState_IncreasesOnlyLoadedDataPlayTime()
    {
        GameManager manager = CreateManager();
        SaveData previousData = new SaveData { playTime = 10f };
        manager.LoadState(previousData);
        manager.enabled = true;
        yield return null;
        yield return null;
        manager.enabled = false;
        float previousPlayTime = previousData.playTime;
        Assert.That(previousPlayTime, Is.GreaterThan(10f));
        SaveData loadedData = new SaveData { playTime = 123.5f };

        manager.LoadState(loadedData);
        manager.enabled = true;
        yield return null;
        yield return null;
        manager.enabled = false;

        Assert.That(loadedData.playTime, Is.GreaterThan(123.5f));
        Assert.That(previousData.playTime, Is.EqualTo(previousPlayTime));
        Assert.That(ReadSavedData(manager).playTime, Is.EqualTo(loadedData.playTime));
    }

    // 기존 객체를 파괴한 뒤 새 객체를 만들면 남은 정적 참조가 생성을 막지 않고 새 게임 상태로 초기화되는지 확인한다.
    [UnityTest]
    public IEnumerator DestroyAndRecreate_RegistersNewInstanceWithFreshState()
    {
        GameManager previous = CreateManager();
        previous.Economy.AddMoney(12345);
        EconomyManager previousEconomy = previous.Economy;

        UnityEngine.Object.Destroy(previous.gameObject);
        yield return null;

        Assert.That(previous == null, Is.True);
        // Unity의 파괴된 객체 비교 규칙으로 인스턴스가 더 이상 유효하지 않은지 확인한다.
        Assert.That(GameManager.Instance == null, Is.True);

        GameManager replacement = CreateManager();

        Assert.That(GameManager.Instance, Is.SameAs(replacement));
        Assert.That(replacement.Economy, Is.Not.SameAs(previousEconomy));
        Assert.That(replacement.GetSaveJson(), Is.EqualTo(JsonUtility.ToJson(new SaveData(), true)));
    }

    // Awake는 실제로 실행하되 시간 누적이 필요한 테스트에서만 Update를 켜도록 전용 객체를 생성한다.
    private GameManager CreateManager()
    {
        GameObject owner = new GameObject("GameManagerLifecycleTests");
        createdObjects.Add(owner);
        GameManager manager = owner.AddComponent<GameManager>();
        manager.enabled = false;
        return manager;
    }

    // Build Settings를 변경하지 않고 씬 생성·제거를 검증할 수 있도록 이름이 겹치지 않는 임시 씬을 만든다.
    private Scene CreateTestScene(string suffix)
    {
        Scene scene = SceneManager.CreateScene("GameManagerLifecycle_" + suffix + "_" + Guid.NewGuid().ToString("N"));
        createdScenes.Add(scene);
        return scene;
    }

    // 실제 저장 JSON에서 플레이 시간을 읽어 private 필드에 접근하지 않고 시간 누적 결과를 확인한다.
    private static SaveData ReadSavedData(GameManager manager)
    {
        return JsonUtility.FromJson<SaveData>(manager.GetSaveJson());
    }
}
