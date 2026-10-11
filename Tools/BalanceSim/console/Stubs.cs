// Minimal stand-ins for the few UnityEngine types the pure data files use, so they compile in a console app.
namespace UnityEngine
{
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
    }
    public static class Mathf
    {
        public static float Pow(float a, float b) => (float)System.Math.Pow(a, b);
        public static float Min(float a, float b) => System.Math.Min(a, b);
        public static float Max(float a, float b) => System.Math.Max(a, b);
        public static int Min(int a, int b) => System.Math.Min(a, b);
        public static int Max(int a, int b) => System.Math.Max(a, b);
    }
}
