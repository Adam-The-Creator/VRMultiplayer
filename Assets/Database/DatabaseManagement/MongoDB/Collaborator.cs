using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Collaborator
    {

        public Collaborator() { }

        public Collaborator(string id, string joined)
        {
            this.id = id;
            this.joined = joined;
        }

        [Tooltip("The ID of the collaborator.")]
        public string id;

        [Tooltip("The date when the collaborator joined.")]
        public string joined;

    }
}