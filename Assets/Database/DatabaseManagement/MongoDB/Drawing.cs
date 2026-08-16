using System.Collections.Generic;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Drawing
    {

        public Drawing() { }

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
        }

        [Tooltip("Metadata of the drawing")]
        public Metadata metadata;

        [Tooltip("List of lines in the drawing")]
        public List<Line> lines;

        [Tooltip("List of tracked behaviors of players during the drawing")]
        public List<TrackedBehavior> trackedBehaviors;

        [Tooltip("List of models placed in the drawing")]
        public List<PlacedModel> placedModels;

    }
}