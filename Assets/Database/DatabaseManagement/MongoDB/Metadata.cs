using System.Collections.Generic;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Metadata
    {

        public Metadata() { }

        public Metadata(
            string id,
            string owner,
            HashSet<Collaborator> collaborators,
            string version,
            string date,
            GameType gameType
        )
        {
            this.id = id;
            this.owner = owner;
            this.collaborators = collaborators;
            this.version = version;
            this.date = date;
            this.gameType = gameType;
        }

        [Tooltip("The unique identifier of the drawing.")]
        public string id;

        [Tooltip("The unique identifier of the owner of the drawing.")]
        public string owner;

        [Tooltip("The list of collaborators on the drawing.")]
        public HashSet<Collaborator> collaborators;

        [Tooltip("The version of the drawing.")]
        public string version;

        [Tooltip("The date the drawing was created.")]
        public string date;

        [Tooltip("The type of game the drawing was created in.")]
        public GameType gameType;

        [Tooltip("The session ID of the drawing.")]
        public string sessionID;

        [Tooltip("Whether to show the boy character in the drawing.")]
        public bool showBoy;

        [Tooltip("Whether to show the girl character in the drawing.")]
        public bool showGirl;

        [Tooltip("The coordinates of the sad child(ren) character(s) in the drawing.")]
        public SadChildCoordinate sadChildCoordinates;

    }
}