using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class SadChildCoordinate
    {

        public SadChildCoordinate() { }

        public SadChildCoordinate(float x, float y, float z, float forwardX, float forwardY, float forwardZ)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.forwardX = forwardX;
            this.forwardY = forwardY;
            this.forwardZ = forwardZ;
        }

        public SadChildCoordinate(Vector3 position, Vector3 forward)
        {
            this.x = position.x;
            this.y = position.y;
            this.z = position.z;
            this.forwardX = forward.x;
            this.forwardY = forward.y;
            this.forwardZ = forward.z;
        }

        public float x;

        public float y;

        public float z;

        public float forwardX;

        public float forwardY;

        public float forwardZ;

    }
}