using System;

// 시간대를 순서대로 진행하고 날짜·시간대 변경 이벤트를 알린다.
public class PhaseManager
{
    private PhaseData data;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;

    public PhaseManager(PhaseData data)
    {
        LoadData(data);
    }

    // 불러오기는 시간 진행이 아니므로 변경 이벤트를 발생시키지 않는다.
    public void LoadData(PhaseData newData)
    {
        data = newData ?? throw new ArgumentNullException(nameof(newData));
    }

    public int GetDay()
    {
        return data.day;
    }

    public void AdvancePhase()
    {
        switch (data.phase)
        {
            case GamePhase.Morning:
                SetPhase(GamePhase.Lunch);
                break;

            case GamePhase.Lunch:
                SetPhase(GamePhase.Closing);
                break;

            case GamePhase.Closing:
                SetPhase(GamePhase.Evening);
                break;

            case GamePhase.Evening:
                AdvanceNextDay();
                break;
        }
    }

    private void SetPhase(GamePhase newPhase)
    {
        data.phase = newPhase;
        OnPhaseChanged?.Invoke(data.phase);
    }

    private void AdvanceNextDay()
    {
        data.day++;
        // 새 날짜를 먼저 알린다. 이 콜백 시점의 시간대는 아직 Evening이다.
        OnDayChanged?.Invoke(data.day);
        SetPhase(GamePhase.Morning);
    }
}
