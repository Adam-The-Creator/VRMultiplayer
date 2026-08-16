using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class LineEvent
    {

        public LineEvent() { }

        public LineEvent(LineEventType eventType, string invoker, string timestamp, Hand hand)
        {
            this.eventType = eventType;
            this.invoker = invoker;
            this.timestamp = timestamp;
            this.hand = hand;
        }

        public LineEventType eventType;

        /// <summary>
        /// The ID of the player who invoked the event
        /// </summary>
        [Tooltip("The ID of the player who invoked the event")]
        public string invoker;

        [Tooltip("The timestamp when the event was invoked")]
        public string timestamp;

        [Tooltip("The hand used to invoke the event")]
        public Hand hand;

    }
}