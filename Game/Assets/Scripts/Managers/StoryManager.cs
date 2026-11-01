using System;

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
