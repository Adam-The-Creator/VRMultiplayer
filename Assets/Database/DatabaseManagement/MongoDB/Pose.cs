using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Pose
    {

        public Pose() { }

        public Pose(
            float pX,
            float pY,
            float pZ,
            float rX,
            float rY,
            float rZ,
            float rW,
            string timestamp
        )
        {
            this.pX = pX;
            this.pY = pY;
            this.pZ = pZ;
            this.rX = rX;
            this.rY = rY;
            this.rZ = rZ;
            this.rW = rW;
            this.timestamp = timestamp;
        }

        public Pose(Vector3 position, Quaternion rotation, string timestamp)
        {
            this.pX = position.x;
            this.pY = position.y;
            this.pZ = position.z;
            this.rX = rotation.x;
            this.rY = rotation.y;
            this.rZ = rotation.z;
            this.rW = rotation.w;
            this.timestamp = timestamp;
        }

        /// <summary>A tracked pose: it carries utcMs only, the string timestamp would cost more than the pose.</summary>
        public Pose(Vector3 position, Quaternion rotation, long utcMs)
        {
            this.pX = position.x;
            this.pY = position.y;
            this.pZ = position.z;
            this.rX = rotation.x;
            this.rY = rotation.y;
            this.rZ = rotation.z;
            this.rW = rotation.w;
            this.utcMs = utcMs;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(pX, pY, pZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rX, rY, rZ, rW);
        }

        public float pX;

        public float pY;

        public float pZ;

        public float rX;

        public float rY;

        public float rZ;

        public float rW;

        [Tooltip("Legacy stamp, null for poses recorded with utcMs")]
        public string timestamp;

        [Tooltip("UTC time in Unix milliseconds. 0 = not recorded")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long utcMs;

    }
}