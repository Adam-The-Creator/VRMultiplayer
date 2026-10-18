using System.Collections.Generic;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Drawing
    {

        public Drawing()
        {
            this.metadata = new Metadata();
            this.lines = new List<Line>();
            this.trackedBehaviors = new List<TrackedBehavior>();
            this.placedModels = new List<PlacedModel>();
            this.events = new List<DrawingEvent>();
        }

        public Drawing(
            Metadata metadata,
            List<Line> lines,
            List<TrackedBehavior> trackedBehaviors,
            List<PlacedModel> placedModels
        )
        {
            this.metadata = metadata;
            this.lines = lines;
            this.trackedBehaviors = trackedBehaviors;
            this.placedModels = placedModels;
            this.events = new List<DrawingEvent>();
        }

        [Tooltip("Metadata of the drawing")]
        public Metadata metadata;

        [Tooltip("List of lines in the drawing")]
        public List<Line> lines;

        [Tooltip("List of tracked behaviors of players during the drawing")]
        public List<TrackedBehavior> trackedBehaviors;

        [Tooltip("List of models placed in the drawing")]
        public List<PlacedModel> placedModels;

        [Tooltip("Changes of the players' tools and menus during the drawing (absent in drawings saved before)")]
        public List<DrawingEvent> events;

    }
}