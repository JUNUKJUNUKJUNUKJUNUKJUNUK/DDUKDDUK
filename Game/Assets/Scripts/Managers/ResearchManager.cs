using System;
using System.Collections.Generic;

// 유효한 연구 ID의 완료 여부를 관리하고 중복 기록을 방지한다.
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

    // 선행 조건·비용·행동 가능 여부는 호출 측에서 검증하며 여기서는 완료 기록만 처리한다.
    public void CompleteResearch(string researchId)
    {
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