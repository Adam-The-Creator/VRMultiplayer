namespace Assets.Database.DatabaseManagement.SQLiteDB
{
    [System.Serializable]
    public class Player
    {
        public string id;
        public string username;
        public PlayerInfo playerInfo;
        public Drawing[] drawings;
        public string signedIn;
        public string created;
        public Role role;

        public Player(
            string id = "",
            string username = "",
            PlayerInfo playerInfo = null,
            Drawing[] drawings = null,
            string signedIn = "",
            string created = "",
            Role role = 0
        )
        {
            this.id = id;
            this.username = username;
            this.playerInfo = playerInfo;
            this.drawings = drawings;
            this.signedIn = signedIn;
            this.created = created;
            this.role = role;
        }
    }
}
