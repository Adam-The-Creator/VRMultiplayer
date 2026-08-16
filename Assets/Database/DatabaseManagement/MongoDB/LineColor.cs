using UnityEngine;

namespace Assets.Database.DatabaseManagement.MongoDB
{
    [System.Serializable]
    public class LineColor
    {

        public LineColor() { }

        public LineColor(float r, float g, float b, float a)
        {
            this.r = (int)(Mathf.Clamp01(r) * 255);
            this.g = (int)(Mathf.Clamp01(g) * 255);
            this.b = (int)(Mathf.Clamp01(b) * 255);
            this.a = a;
        }

        public LineColor(int r, int g, int b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public LineColor(Color color)
        {
            this.r = (int)(Mathf.Clamp01(color.r) * 255);
            this.g = (int)(Mathf.Clamp01(color.g) * 255);
            this.b = (int)(Mathf.Clamp01(color.b) * 255);
            this.a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        [Range(0, 255)] public int r;

        [Range(0, 255)] public int g;

        [Range(0, 255)] public int b;

        [Range(0f, 1f)] public float a;

    }
}