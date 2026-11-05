using System;

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
    }
}
