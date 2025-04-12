using System.Collections.Generic;

[System.Serializable]
public class UserData
{
    public string userId;
    public List<TaskListManager.TasklistItem> taskList = new List<TaskListManager.TasklistItem>();
    public int starAmt;

    public List<TileData> tileStates = new List<TileData>();

    public UserData()
    {
        taskList = new List<TaskListManager.TasklistItem>();
        tileStates = new List<TileData>();
    }
}

[System.Serializable]
public class TileData
{
    public int x;
    public int y;
    public GameManager.TileState state;
}
