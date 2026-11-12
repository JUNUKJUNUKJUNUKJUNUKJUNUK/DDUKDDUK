using System;
using System.IO;
using UnityEngine;

// 슬롯별 JSON 파일의 쓰기·읽기를 담당하며 게임 상태의 변환과 검증은 호출 측에 맡긴다.
public class SaveFileService
{
    public const int SlotCount = 3;

    public string DirectoryPath { get; }

    public SaveFileService()
        : this(Path.Combine(Application.persistentDataPath, "Saves"))
    {
    }

    public SaveFileService(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("저장 경로가 비어 있습니다.", nameof(directoryPath));

        DirectoryPath = Path.GetFullPath(directoryPath);
    }

    public void Save(int slot, string json)
    {
        string path = GetSlotPath(slot);
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("저장할 JSON이 비어 있습니다.", nameof(json));

        Directory.CreateDirectory(DirectoryPath);

        // 기존 파일에 직접 쓰지 않고 같은 폴더의 임시 파일을 완성한 뒤 교체한다.
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            // 이번 요청의 임시 파일만 정리하며 교체 실패 시 기존 저장 파일은 삭제하지 않는다.
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public string Load(int slot)
    {
        return File.ReadAllText(GetSlotPath(slot));
    }

    // 존재 여부만 확인한다. JSON 형식과 저장값 검증은 GameManager.LoadJson에서 수행한다.
    public bool Exists(int slot)
    {
        return File.Exists(GetSlotPath(slot));
    }

    private string GetSlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount)
            throw new ArgumentOutOfRangeException(nameof(slot), "슬롯 번호는 1~3이어야 합니다.");

        return Path.Combine(DirectoryPath, "slot-" + slot + ".json");
    }
}
