using System;
namespace UnityEngine
{
    public class Object : Il2CppSystem.Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { public MonoBehaviour(IntPtr pointer) { } }
    public class Transform : Component { }
    public class RectTransform : Transform { public Vector2 sizeDelta { get => default; set { } } public Vector2 anchoredPosition { get => default; set { } } public Rect rect => default; }
    public class Texture : Object { }
    public class Texture2D : Texture { public static Texture2D whiteTexture => null; }
    public struct Vector2 { public float x; public float y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white => default; }
    public struct Rect { public Rect(float x, float y, float width, float height) { } public float height => 0f; }
    public static class Screen { public static int width => 0; public static int height => 0; }
    public static class Time { public static int frameCount => 0; public static float unscaledTime => 0f; }
}
