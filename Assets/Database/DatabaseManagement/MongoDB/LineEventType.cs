namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public enum LineEventType : int
    {
        ERASE   = 0,
        DRAW    = 1,
        UNDO    = 2
    }
}