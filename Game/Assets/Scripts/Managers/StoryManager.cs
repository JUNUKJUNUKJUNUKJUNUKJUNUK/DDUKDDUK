using System;
using System.Collections.Generic;
using UnityEngine;

// 스토리 진행 데이터의 보관과 교체를 담당한다.
public class StoryManager
{
    private StoryData data;

    public StoryManager(StoryData data)
    {
        LoadData(data);
    }

    public void LoadData(StoryData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));

        if (data.completedEventIds == null)
            data.completedEventIds = new List<string>();

        if (data.choiceHistory == null)
            data.choiceHistory = new List<int>();

        if (data.currentRoute == null)
            data.currentRoute = string.Empty;

        if (data.currentEventId == null)
            data.currentEventId = string.Empty;
    }

    private StoryData Data => data;

    // 세이브 복원 시 미연시 대화 도중이었는지 여부
    public bool IsSavedInDialogue => Data != null && !string.IsNullOrEmpty(Data.currentEventId) && Data.lineIndex >= 0;

    // 현재 고정된 엔딩 루트를 Route 열거형으로 반환
    public Route CurrentRouteEnum
    {
        get
        {
            if (Data == null || string.IsNullOrEmpty(Data.currentRoute))
                return Route.None;

            return Enum.TryParse(Data.currentRoute, true, out Route parsedRoute) ? parsedRoute : Route.None;
        }
    }

    // 엔딩데이트 개방 조건 판정 (10주차 토요일 = 69일차 이상 & 루트 미확정 & 기준 호감도 이상)
    public bool CanStartEndingDate(int currentDay, int affectionScore, int requiredAffection = 55)
    {
        return string.IsNullOrEmpty(Data.currentRoute)
               && currentDay >= 69
               && affectionScore >= requiredAffection;
    }

    // 데이트 이벤트 요청 처리 (엔딩데이트 완료 후 일반 데이트 요청 시 진지한 이야기로 전환)
    public string RequestDateEvent(CharacterId characterId, string requestedEventId)
    {
        return RequestDateEvent(characterId.ToString(), requestedEventId);
    }

    public string RequestDateEvent(string characterKey, string requestedEventId)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
            throw new ArgumentException("캐릭터 키가 비어 있습니다.", nameof(characterKey));

        if (string.IsNullOrWhiteSpace(requestedEventId))
            throw new ArgumentException("이벤트 ID가 비어 있습니다.", nameof(requestedEventId));

        string normalizedKey = characterKey.ToLower();
        string seriousTalkId = $"{normalizedKey}_serious_talk";

        if (!string.IsNullOrEmpty(Data.currentRoute) &&
            Data.currentRoute.ToLower() == normalizedKey &&
            !IsEventCompleted(seriousTalkId))
        {
            Debug.Log($"[StoryManager] 엔딩 루트 진입 상태: 일반 데이트 대신 '{seriousTalkId}'(진지한 이야기)로 전환합니다.");
            StartDialogue(seriousTalkId, 0);
            return seriousTalkId;
        }

        StartDialogue(requestedEventId, 0);
        return requestedEventId;
    }

    // 대화 시작 및 진행 줄 번호 기록
    public void StartDialogue(string eventId, int lineIndex)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("이벤트 ID가 비어 있습니다.", nameof(eventId));

        if (lineIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "대사 줄 번호는 0 이상이어야 합니다.");

        Data.currentEventId = eventId;
        Data.lineIndex = lineIndex;
    }

    // 대화 종료 및 완료 기록
    public void FinishDialogue(string eventId, CharacterId characterId, bool isEndingDate = false)
    {
        FinishDialogue(eventId, characterId.ToString(), isEndingDate);
    }

    public void FinishDialogue(string eventId, string characterKey = "", bool isEndingDate = false)
    {
        if (!string.IsNullOrWhiteSpace(eventId) && !IsEventCompleted(eventId))
        {
            Data.completedEventIds.Add(eventId);

            if (isEndingDate && !string.IsNullOrWhiteSpace(characterKey))
            {
                Data.currentRoute = characterKey.ToLower();
            }
        }

        Data.currentEventId = string.Empty;
        Data.lineIndex = -1;
        Data.choiceHistory.Clear();
    }

    // 오늘 아침 영업을 대체할 스토리 이벤트 ID 반환 (D+2일: _solution, D+3일: _final_meet, 없으면 "")
    public string GetReplacedStoryEventId()
    {
        if (string.IsNullOrEmpty(Data.currentRoute))
            return string.Empty;

        string routeKey = Data.currentRoute.ToLower();
        string seriousTalkId = $"{routeKey}_serious_talk";
        string solutionId = $"{routeKey}_solution";
        string finalMeetId = $"{routeKey}_final_meet";

        if (!IsEventCompleted(seriousTalkId) || IsEventCompleted(finalMeetId))
            return string.Empty;

        return !IsEventCompleted(solutionId) ? solutionId : finalMeetId;
    }

    // 방앗간 영업 대체일 여부 판정
    public bool IsBusinessReplacedByStory()
    {
        return !string.IsNullOrEmpty(GetReplacedStoryEventId());
    }

    // 최종 고백 결과 판정 (70 미만 실패 / 70~99 성공 / 100 대성공)
    public EndingResult EvaluateEnding(int affectionScore)
    {
        if (affectionScore >= 100)
            return EndingResult.GreatSuccess;

        if (affectionScore >= 70)
            return EndingResult.Success;

        return EndingResult.Fail;
    }

    public bool IsEventCompleted(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && Data.completedEventIds.Contains(eventId);
    }
}