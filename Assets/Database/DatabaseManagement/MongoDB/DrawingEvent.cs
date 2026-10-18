using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    /// <summary>
    /// A change of a player's tools or menus during drawing. Strokes and erasures are not repeated here, they are
    /// already described by <see cref="Line"/> and its history.
    /// </summary>
    [System.Serializable]
    public class DrawingEvent
    {

        public DrawingEvent() { }

        public DrawingEvent(DrawingEventType type, string playerID, Hand hand, long utcMs, string value)
        {
            this.type = type;
            this.playerID = playerID;
            this.hand = hand;
            this.utcMs = utcMs;
            this.value = value;
        }

        [Tooltip("What happened")]
        public DrawingEventType type;

        [Tooltip("The ID of the player who caused the event")]
        public string playerID;

        [Tooltip("The hand whose brush or menu the event belongs to")]
        public Hand hand;

        [Tooltip("UTC time in Unix milliseconds")]
        public long utcMs;

        [Tooltip("Details: the new mode name, or the new colors/widths of the brush. Null when there are none")]
        public string value;

    }
}
