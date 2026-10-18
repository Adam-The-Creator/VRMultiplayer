using Newtonsoft.Json;
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

        public Point(Vector3 point, string timestamp, long utcMs, float pressure) : this(point, timestamp)
        {
            this.utcMs = utcMs;
            this.pressure = pressure;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public float x;

        public float y;

        public float z;

        [Tooltip("Legacy local-time stamp (yyyy_MM_dd_HH_mm_ss_fff), kept for older drawings and tools")]
        public string timestamp;

        // utcMs and pressure are only written to the JSON when they were recorded: 0 means "not recorded" (a drawing
        // saved before the fields existed), and it must stay absent instead of becoming a bogus 1970 time.

        [Tooltip("UTC time in Unix milliseconds. 0 = not recorded")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long utcMs;

        [Tooltip("Trigger value (0..1) when the point was drawn. 0 = not recorded")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float pressure;

    }
}