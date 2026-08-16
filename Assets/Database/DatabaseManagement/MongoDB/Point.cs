using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class Point
    {

        public Point() { }

        public Point(float x, float y, float z, string timestamp)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.timestamp = timestamp;
        }

        public Point(Vector3 point, string timestamp)
        {
            this.x = point.x;
            this.y = point.y;
            this.z = point.z;
            this.timestamp = timestamp;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public float x;

        public float y;

        public float z;

        public string timestamp;

    }
}