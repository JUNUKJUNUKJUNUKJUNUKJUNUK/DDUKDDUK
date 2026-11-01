using System;

public class PhaseManager
{
    private PhaseData data;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;

    public PhaseManager(PhaseData data)
    {
        LoadData(data);
    }

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
                SetPhase(GamePhase.Night);
                break;

            case GamePhase.Night:
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
        OnDayChanged?.Invoke(data.day);
        SetPhase(GamePhase.Morning);
    }
}
