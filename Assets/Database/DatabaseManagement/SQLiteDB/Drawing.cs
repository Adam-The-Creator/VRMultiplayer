using Assets.Database.DatabaseManagement.MongoDB;

namespace Assets.Database.DatabaseManagement.SQLiteDB
{
    [System.Serializable]
    public class Drawing
    {
        public string id;
        public string playerId;
        public string name;
        public string path;
        public GameType gameType;
        public string sessionId;


        public Drawing(
            string id = "",
            string playerId = "",
            string name = "",
            string path = "",
            GameType gameType = GameType.INNER_CHILD,
            string sessionId = null
        )
        {
            this.id = id;
            this.playerId = playerId;
            this.name = name;
            this.path = path;
            this.gameType = gameType;
            this.sessionId = sessionId;
        }
    }
}
