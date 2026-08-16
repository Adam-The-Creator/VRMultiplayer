namespace Assets.Database.DatabaseManagement.SQLiteDB
{
    [System.Serializable]
    public class Session
    {
        public string id;
        public string name;
        public string description;
        public string startDate;
        public string endDate;
        public bool showBoy = true;
        public bool showGirl = true;
        public bool multiplayer = false;
        public Session(
            string id = "",
            string name = "",
            string description = "",
            string startDate = "",
            string endDate = "",
            bool showBoy = true,
            bool showGirl = true,
            bool multiplayer = false
        )
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.startDate = startDate;
            this.endDate = endDate;
            this.showBoy = showBoy;
            this.showGirl = showGirl;
            this.multiplayer = multiplayer;
        }
    }
}
