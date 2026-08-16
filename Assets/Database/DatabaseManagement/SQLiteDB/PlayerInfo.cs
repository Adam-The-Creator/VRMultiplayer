namespace Assets.Database.DatabaseManagement.SQLiteDB
{
    [System.Serializable]
    public class PlayerInfo
    {
        public string id;
        public string name;
        public string gender;
        public int age;
        public string dominantHand;

        public PlayerInfo(
            string id = "",
            string name = "",
            string gender = "",
            int age = 0,
            string dominantHand = ""
        )
        {
            this.id = id;
            this.name = name;
            this.gender = gender;
            this.age = age;
            this.dominantHand = dominantHand;
        }
    }
}
