using System.Numerics;
using Raylib_cs;

namespace AdvEditRework.UI;

public class Container
{
    public Rectangle Rectangle { get; set; }
    public Rectangle Body { get; set; }
    public Vector2 ContentSize { get; set; }
    public Vector2 Scroll { get; set; }
    public int ZIndex { get; set; }
    public bool Open { get; set; }
}