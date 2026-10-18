namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public enum DrawingEventType : int
    {
        MENU_OPEN       = 0,
        MENU_CLOSE      = 1,
        MODE_CHANGE     = 2,
        COLOR_CHANGE    = 3,
        WIDTH_CHANGE    = 4
    }
}
