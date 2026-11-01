using System;
using System.Collections.Generic;

public class ResearchManager
{
    private static readonly HashSet<string> ValidResearchIds =
        new HashSet<string>
        {
            "RCP-01",
            "RCP-02",
            "RCP-03",
            "RCP-04",
            "DSP-01",
            "DSP-02"
        };

    private ResearchData data;

    public ResearchManager(ResearchData data)
    {
        LoadData(data);
    }

    public void LoadData(ResearchData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));

        if (data.completedResearchIds == null)
            data.completedResearchIds = new List<string>();
    }

    public bool IsResearchCompleted(string researchId)
    {
        ValidateId(researchId);

        return data.completedResearchIds.Contains(researchId);
    }

    public void CompleteResearch(string researchId)
    {
        // 이 함수를 호출하는 상위 연구 실행 함수에서
        // ID 유효성, 선행 조건, 비용, 행동 가능 여부를 검증한다.
        // 여기서는 데이터 보호를 위해 ID 유효성만 다시 확인한다.
        ValidateId(researchId);

        if (data.completedResearchIds.Contains(researchId))
            return;

        data.completedResearchIds.Add(researchId);
    }

    private static void ValidateId(string researchId)
    {
        if (string.IsNullOrWhiteSpace(researchId))
        {
            throw new ArgumentException(
                "연구 ID는 비어 있을 수 없습니다.",
                nameof(researchId));
        }

        if (!ValidResearchIds.Contains(researchId))
        {
            throw new ArgumentException(
                $"존재하지 않는 연구 ID: {researchId}",
                nameof(researchId));
        }
    }
}