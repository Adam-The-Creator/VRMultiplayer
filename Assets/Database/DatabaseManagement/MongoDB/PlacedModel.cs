using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class PlacedModel
    {
        public string modelName;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW;
        public float scaleX;
        public float scaleY;
        public float scaleZ;

        public PlacedModel() { }

        public PlacedModel(string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            modelName = name;
            positionX = position.x;
            positionY = position.y;
            positionZ = position.z;
            rotationX = rotation.x;
            rotationY = rotation.y;
            rotationZ = rotation.z;
            rotationW = rotation.w;
            scaleX = scale.x;
            scaleY = scale.y;
            scaleZ = scale.z;
        }

        public Vector3 GetPosition() => new(positionX, positionY, positionZ);
        public Quaternion GetRotation() => new(rotationX, rotationY, rotationZ, rotationW);
        public Vector3 GetScale() => new(scaleX, scaleY, scaleZ);
    }
}
