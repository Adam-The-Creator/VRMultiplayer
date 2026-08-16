using System.Collections.Generic;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Line
    {

        public Line() { }

        public Line(
            string id,
            List<Point> points,
            float startWidth,
            float endWidth,
            LineColor startColor,
            LineColor endColor,
            Hand hand,
            string userID,
            List<LineEvent> history,
            Status status = Status.DRAWN
        )
        {
            this.id = id;
            this.points = points;
            this.startWidth = startWidth;
            this.endWidth = endWidth;
            this.startColor = startColor;
            this.endColor = endColor;
            this.hand = hand;
            this.userID = userID;
            this.history = history;
            this.status = status;
        }

        [Tooltip("Unique identifier of the line")]
        public string id;

        [Tooltip("List of points that make up the line")]
        public List<Point> points;

        [Tooltip("Width of the line at the start")]
        public float startWidth;

        [Tooltip("Width of the line at the end")]
        public float endWidth;

        [Tooltip("Color of the line at the start")]
        public LineColor startColor;

        [Tooltip("Color of the line at the end")]
        public LineColor endColor;

        [Tooltip("Hand used to draw the line")]
        public Hand hand;

        [Tooltip("ID of the user who created the line")]
        public string userID;

        /// <summary>
        /// History of line is a series of events that happened to the line. List can be represented as a Stack.
        /// </summary>
        [Tooltip("History of events that happened to the line")]
        public List<LineEvent> history;

        [Tooltip("Status of the line, indicating whether it is drawn, erased, or modified")]
        public Status status;

    }
}