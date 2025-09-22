using System.Numerics;
using Raylib_cs;

namespace AdvEditRework;

public static class Converters
{
    public static Vector4 Vec4(this Color color) => new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
}