using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

// 실제 임시 파일을 사용해 슬롯의 지연 생성, 교체·읽기와 실패 시 파일 보존을 검증한다.
public class SaveFileServiceTests
{
    private string testDirectory;
    private string saveDirectory;
    private SaveFileService service;

    // 실제 플레이어 저장 경로 대신 테스트마다 독립된 임시 폴더를 준비한다.
    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "DDUKDDUK_SaveFiles_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        saveDirectory = Path.Combine(testDirectory, "Saves");
        service = new SaveFileService(saveDirectory);
    }

    // 이 테스트가 생성한 임시 폴더만 제거한다.
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);
    }

    // 생성만으로 저장 폴더나 슬롯 파일이 만들어지지 않는지 확인한다.
    [Test]
    public void Constructor_DoesNotCreateSaveDirectory()
    {
        Assert.That(service.DirectoryPath, Is.EqualTo(Path.GetFullPath(saveDirectory)));
        Assert.That(Directory.Exists(saveDirectory), Is.False);
    }

    // 기본 경로가 Unity 영구 데이터 경로 아래의 Saves 폴더로 설정되는지 확인한다.
    [Test]
    public void Constructor_WithDefaultPath_UsesPersistentDataDirectory()
    {
        SaveFileService defaultService = new SaveFileService();

        Assert.That(defaultService.DirectoryPath,
            Is.EqualTo(Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Saves"))));
    }

    // 비어 있는 저장 경로는 생성 시 거부하는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Constructor_WithEmptyDirectory_Throws(string directory)
    {
        Assert.Throws<ArgumentException>(() => new SaveFileService(directory));
    }

    // 저장하지 않은 슬롯은 존재하지 않으며 존재 조회도 저장 폴더를 만들지 않는지 확인한다.
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Exists_BeforeFirstSave_ReturnsFalseWithoutCreatingDirectory(int slot)
    {
        Assert.That(service.Exists(slot), Is.False);
        Assert.That(Directory.Exists(saveDirectory), Is.False);
    }

    // 첫 저장 때 폴더와 선택한 슬롯 파일만 생성되고 다른 슬롯 파일이나 임시 파일은 남지 않는지 확인한다.
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Save_ForFirstTime_CreatesOnlyRequestedSlot(int slot)
    {
        const string json = "{\"money\":100}";

        service.Save(slot, json);

        Assert.That(Directory.Exists(saveDirectory), Is.True);
        Assert.That(File.ReadAllText(SlotPath(slot)), Is.EqualTo(json));
        Assert.That(Directory.GetFiles(saveDirectory), Is.EquivalentTo(new[] { SlotPath(slot) }));
        for (int other = 1; other <= SaveFileService.SlotCount; other++)
            Assert.That(service.Exists(other), Is.EqualTo(other == slot));
    }

    // 한글·이모지·줄바꿈이 포함된 JSON을 파일에 저장하고 동일한 문자열로 읽는지 확인한다.
    [Test]
    public void SaveAndLoad_WithUnicodeJson_PreservesEntireText()
    {
        string json = "{\n  \"name\": \"한글 떡집 🍡\",\n  \"money\": 12345\n}";

        service.Save(1, json);

        Assert.That(service.Load(1), Is.EqualTo(json));
    }

    // 같은 슬롯을 다시 저장하면 이전 내용이 교체되고 임시 파일은 남지 않는지 확인한다.
    [Test]
    public void Save_WithExistingSlot_ReplacesPreviousContents()
    {
        service.Save(1, "{\"money\":100}");

        service.Save(1, "{\"money\":200}");

        Assert.That(service.Load(1), Is.EqualTo("{\"money\":200}"));
        Assert.That(Directory.GetFiles(saveDirectory), Is.EquivalentTo(new[] { SlotPath(1) }));
    }

    // 세 슬롯을 저장한 뒤 하나를 교체해도 다른 슬롯의 내용은 유지되는지 확인한다.
    [Test]
    public void Save_WithMultipleSlots_KeepsFilesIndependent()
    {
        service.Save(1, "{\"slot\":1}");
        service.Save(2, "{\"slot\":2}");
        service.Save(3, "{\"slot\":3}");

        service.Save(2, "{\"slot\":2,\"updated\":true}");

        Assert.That(service.Load(1), Is.EqualTo("{\"slot\":1}"));
        Assert.That(service.Load(2), Is.EqualTo("{\"slot\":2,\"updated\":true}"));
        Assert.That(service.Load(3), Is.EqualTo("{\"slot\":3}"));
        Assert.That(Directory.GetFiles(saveDirectory).Length, Is.EqualTo(3));
    }

    // 범위 밖 슬롯은 저장·읽기·조회 모두 거부하고 저장 폴더도 만들지 않는지 확인한다.
    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(4)]
    [TestCase(int.MaxValue)]
    public void FileOperations_WithInvalidSlot_ThrowWithoutCreatingDirectory(int slot)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Save(slot, "{}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Load(slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Exists(slot));
        Assert.That(Directory.Exists(saveDirectory), Is.False);
    }

    // 비어 있는 JSON을 처음 저장하려 하면 거부하고 폴더나 파일은 만들지 않는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Save_WithEmptyJson_ThrowsWithoutCreatingDirectory(string json)
    {
        Assert.Throws<ArgumentException>(() => service.Save(1, json));
        Assert.That(Directory.Exists(saveDirectory), Is.False);
    }

    // 기존 슬롯에 비어 있는 JSON을 저장하려 하면 거부하고 이전 내용을 보존하는지 확인한다.
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void Save_WithEmptyJson_PreservesExistingFile(string json)
    {
        service.Save(1, "{\"money\":100}");

        Assert.Throws<ArgumentException>(() => service.Save(1, json));

        Assert.That(service.Load(1), Is.EqualTo("{\"money\":100}"));
        Assert.That(Directory.GetFiles(saveDirectory), Is.EquivalentTo(new[] { SlotPath(1) }));
    }

    // 저장 폴더가 없을 때 읽기는 예외로 실패하고 폴더를 생성하지 않는지 확인한다.
    [Test]
    public void Load_WithMissingDirectory_ThrowsWithoutCreatingDirectory()
    {
        Assert.Throws<DirectoryNotFoundException>(() => service.Load(1));
        Assert.That(Directory.Exists(saveDirectory), Is.False);
    }

    // 저장 폴더는 있지만 요청한 슬롯 파일이 없으면 예외로 실패하고 다른 슬롯은 유지하는지 확인한다.
    [Test]
    public void Load_WithMissingSlot_ThrowsAndPreservesOtherSlot()
    {
        service.Save(2, "{\"slot\":2}");

        Assert.Throws<FileNotFoundException>(() => service.Load(1));

        Assert.That(service.Exists(1), Is.False);
        Assert.That(service.Load(2), Is.EqualTo("{\"slot\":2}"));
    }

    // 슬롯 파일과 이름이 같은 폴더를 저장 파일로 판단하지 않는지 확인한다.
    [Test]
    public void Exists_WithDirectoryAtSlotPath_ReturnsFalse()
    {
        Directory.CreateDirectory(SlotPath(1));

        Assert.That(service.Exists(1), Is.False);
    }

    // 이전 실행의 임시 파일은 저장 슬롯으로 판단하거나 불러오지 않고 새 저장 때에도 임의로 삭제하지 않는지 확인한다.
    [Test]
    public void FileOperations_WithStaleTemporaryFile_IgnoreUnfinishedSave()
    {
        Directory.CreateDirectory(saveDirectory);
        string temporaryPath = SlotPath(1) + ".old.tmp";
        File.WriteAllText(temporaryPath, "unfinished");

        Assert.That(service.Exists(1), Is.False);
        Assert.Throws<FileNotFoundException>(() => service.Load(1));

        service.Save(1, "{}");

        Assert.That(service.Load(1), Is.EqualTo("{}"));
        Assert.That(File.ReadAllText(temporaryPath), Is.EqualTo("unfinished"));
        Assert.That(Directory.GetFiles(saveDirectory).Length, Is.EqualTo(2));
    }

    // 대상 경로가 폴더라 저장이 실패하면 이번 임시 파일을 정리하고 다른 슬롯은 보존하는지 확인한다.
    [Test]
    public void Save_WithDirectoryAtSlotPath_ThrowsAndCleansTemporaryFile()
    {
        service.Save(2, "{\"slot\":2}");
        Directory.CreateDirectory(SlotPath(1));

        Assert.Catch<IOException>(() => service.Save(1, "{}"));

        Assert.That(Directory.Exists(SlotPath(1)), Is.True);
        Assert.That(service.Load(2), Is.EqualTo("{\"slot\":2}"));
        Assert.That(Directory.GetFiles(saveDirectory), Is.EquivalentTo(new[] { SlotPath(2) }));
    }

    // 저장 폴더 경로가 파일로 막혀 있으면 저장을 거부하고 그 파일을 훼손하지 않는지 확인한다.
    [Test]
    public void Save_WithFileAtDirectoryPath_ThrowsAndPreservesBlockingFile()
    {
        File.WriteAllText(saveDirectory, "existing file");

        Assert.Catch<IOException>(() => service.Save(1, "{}"));

        Assert.That(File.ReadAllText(saveDirectory), Is.EqualTo("existing file"));
        Assert.That(Directory.GetFiles(testDirectory), Is.EquivalentTo(new[] { saveDirectory }));
    }

    // Windows에서 읽기 전용 슬롯의 교체가 실패하면 기존 내용이 유지되고 임시 파일은 정리되는지 확인한다.
    [Test]
    [Platform("Win")]
    public void Save_WithReadOnlyDestination_PreservesExistingFileAndCleansTemporaryFile()
    {
        service.Save(1, "{\"money\":100}");
        string path = SlotPath(1);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            Exception error = Assert.Catch(() => service.Save(1, "{\"money\":200}"));

            Assert.That(error is IOException || error is UnauthorizedAccessException, Is.True);
            Assert.That(service.Load(1), Is.EqualTo("{\"money\":100}"));
            Assert.That(Directory.GetFiles(saveDirectory), Is.EquivalentTo(new[] { path }));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    // Windows에서 다른 처리가 독점 사용 중인 파일은 읽기에 실패하고 내용은 유지하는지 확인한다.
    [Test]
    [Platform("Win")]
    public void Load_WithExclusivelyLockedFile_ThrowsAndPreservesContents()
    {
        service.Save(1, "{\"money\":100}");
        using (FileStream locked = File.Open(SlotPath(1), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Catch<IOException>(() => service.Load(1));
        }

        Assert.That(service.Load(1), Is.EqualTo("{\"money\":100}"));
    }

    private string SlotPath(int slot)
    {
        return Path.Combine(saveDirectory, "slot-" + slot + ".json");
    }
}
