using System.Collections.Generic;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    /// <summary>
    /// Used for tracking player's behavior during drawing. Sampling frequency may causes large data.
    /// </summary>
    [System.Serializable]
    public class TrackedBehavior
    {

        public TrackedBehavior() { }

        public TrackedBehavior(
            string playerID,
            List<Pose> head,
            List<Pose> rightHand,
            List<Pose> leftHand,
            List<Pose> palyer
        )
        {
            this.playerID = playerID;
            this.head = head;
            this.rightHand = rightHand;
            this.leftHand = leftHand;
            this.palyer = palyer;
        }

        [Tooltip("The ID of the player whose behavior is being tracked.")]
        public string playerID;

        [Tooltip("The list of head poses of the player during drawing.")]
        public List<Pose> head;

        [Tooltip("The list of right hand poses of the player during drawing.")]
        public List<Pose> rightHand;

        [Tooltip("The list of left hand poses of the player during drawing.")]
        public List<Pose> leftHand;

        [Tooltip("The list of player poses of the player during drawing.")]
        public List<Pose> palyer;

    }
}