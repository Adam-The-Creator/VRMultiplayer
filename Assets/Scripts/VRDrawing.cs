using System.Collections.Generic;
using UnityEngine;

namespace DrawingData
{
    [System.Serializable]
    public enum GameType
    {
        InnerChild          = 0,
        WordForest          = 1,
        Beach               = 2,
        Room                = 3,
        BilateralDrawing    = 4,
        SafePlace           = 5
    
        
    }

    [System.Serializable]
    public class SadChildCoordinates
    {
        public float x;
        public float y;
        public float z;

        public SadChildCoordinates(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new(x, y, z);
    }

    [System.Serializable]
    public class Metadata
    {
        public string userId;
        public string version;
        public string date;
        public GameType gameType = GameType.InnerChild;
        public string sessionId;
        public int showBoy;
        public int showGirl;
        public SadChildCoordinates sadChildCoordinates;
    }

    [System.Serializable]
    public class Point
    {
        public float x;
        public float y;
        public float z;
        public string timestamp;

        public Point(Vector3 v, string time)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            timestamp = time;
        }

        public Vector3 ToVector3() => new(x, y, z);
    }

    [System.Serializable]
    public class Line
    {
        public string id;
        public List<Point> points;
        public float startWidth;
        public float endWidth;
        public Color startColor;
        public Color endColor;
        public string hand;
    }

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

        public Vector3 GetPosition() => new Vector3(positionX, positionY, positionZ);
        public Quaternion GetRotation() => new Quaternion(rotationX, rotationY, rotationZ, rotationW);
        public Vector3 GetScale() => new Vector3(scaleX, scaleY, scaleZ);
    }

    [System.Serializable]
    public class Drawing
    {
        public Metadata metadata;
        public List<Line> lines;
        public List<PlacedModel> placedModels;
    }
}

public class VRDrawing : MonoBehaviour
{
    [Header("Drawing Data")]
    public DrawingData.Drawing drawing;
}
