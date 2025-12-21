using System.Numerics;
using AdvancedLib.Serialization.AI;
using Raylib_cs;

namespace AdvancedLib.RaylibExt;

public enum DragHandle
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

public enum HoverPart
{
    None,
    Zone,
    Target,
}

public static class AiExtensions
{
    public static void Draw(this AiZone zone, Color color)
    {
        if (zone.Shape == ZoneShape.Rectangle)
        {
            var zoneRect = new Rectangle(zone.X, zone.Y, zone.Width + 1, zone.Height + 1);
            zoneRect.Position *= 8 * AiZone.Precision;
            zoneRect.Size *= 8 * AiZone.Precision;
            Raylib.DrawRectangleLinesEx(zoneRect, 1, color);
            Raylib.DrawRectangleRec(zoneRect, color with { A = 96 });
        }
        else
        {
            var zoneRect = GetZoneRect(zone);

            zoneRect.Position *= 8 * AiZone.Precision;
            zoneRect.Size *= 8 * AiZone.Precision;
            DrawTriangle(zone, color);
        }
    }

    public static Rectangle GetZoneRect(this AiZone zone)
    {
        if (zone.Shape == ZoneShape.Rectangle) return new Rectangle(zone.X, zone.Y, zone.Width + 1, zone.Height + 1);
        var zoneRect = new Rectangle(zone.X, zone.Y, zone.Width + 1, zone.Width + 1);
        switch (zone.Shape)
        {
            case ZoneShape.TriangleTopRight:
                zoneRect.X -= zoneRect.Width - 1;
                break;
            case ZoneShape.TriangleBottomRight:
                zoneRect.X -= zoneRect.Width - 1;
                zoneRect.Y -= zoneRect.Width - 1;
                break;
            case ZoneShape.TriangleBottomLeft:
                zoneRect.Y -= zoneRect.Width - 1;
                break;
        }

        return zoneRect;
    }

    static void DrawTriangle(this AiZone zone, Color color)
    {
        var points = new Vector2[zone.Width * 2 + 5];
        var scale = AiZone.Precision * 8;
        var triRect = GetZoneRect(zone);
        Vector2 vertex, arm;
        Vector2 step;
        switch (zone.Shape)
        {
            case ZoneShape.TriangleTopLeft:
                vertex = triRect.Position;
                arm = vertex + new Vector2(0, triRect.Height);
                step = new Vector2(1, -1);
                break;
            case ZoneShape.TriangleTopRight:
                vertex = triRect.Position + new Vector2(triRect.Width, 0);
                arm = vertex + new Vector2(0, triRect.Height);
                step = new Vector2(-1, -1);
                break;
            case ZoneShape.TriangleBottomLeft:
                vertex = triRect.Position + new Vector2(0, triRect.Height);
                arm = vertex + new Vector2(0, -triRect.Height);
                step = new Vector2(1, 1);
                break;
            case ZoneShape.TriangleBottomRight:
                vertex = triRect.Position + new Vector2(triRect.Width, triRect.Height);
                arm = vertex + new Vector2(0, -triRect.Height);
                step = new Vector2(-1, 1);
                break;
            default: throw new ArgumentException();
        }

        vertex *= scale;
        arm *= scale;
        step *= scale;

        points[0] = vertex;
        Vector2 pos = arm;
        var even = true;
        for (int i = 1; i < points.Length - 1; i++)
        {
            points[i] = pos;
            if (even)
                pos.X += step.X;
            else
                pos.Y += step.Y;
            even = !even;
        }

        // HACK: reverse point order so fans are always wound counter-clockwise
        points[^1] = vertex;
        if (zone.Shape == ZoneShape.TriangleTopRight || zone.Shape == ZoneShape.TriangleBottomLeft)
        {
            Array.Reverse(points);
        }


        for (int i = 0; i < points.Length - 1; i++)
        {
            Raylib.DrawLineEx(points[i], points[i + 1], 1, color);
        }

        Raylib.DrawTriangleFan(points, points.Length, color with { A = 96 });
    }

    public static bool IsHovered(this AiZone zone, Vector2 tilePos)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == ZoneShape.Rectangle)
        {
            return Raylib.CheckCollisionPointRec(aiBlockPos.AsVector2(), GetZoneRect(zone));
        }

        return zone.IntersectsWithTriangle(aiBlockPos);
    }

    private static bool IntersectsWithTriangle(this AiZone zone, Vec2I aiBlockPos)
    {
        if (!Raylib.CheckCollisionPointRec(aiBlockPos.AsVector2(), GetZoneRect(zone)))
        {
            return false;
        }

        Rectangle zoneRect = GetZoneRect(zone);
        var x = (int)(aiBlockPos.X - zoneRect.X);
        var y = (int)(aiBlockPos.Y - zoneRect.Y);

        switch (zone.Shape)
        {
            case ZoneShape.TriangleTopLeft:
                return x + (y - 1) <= zone.Width - 1;
            case ZoneShape.TriangleTopRight:
                return x >= y;
            case ZoneShape.TriangleBottomRight:
                return (x + 0) + y >= zone.Width;
            case ZoneShape.TriangleBottomLeft:
                return x <= y;
            default:
                throw new InvalidOperationException();
        }
    }

    public static DragHandle GetResizeHandle(this AiZone zone, Vector2 tilePos)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == ZoneShape.Rectangle)
        {
            return GetResizeHandleRectangle(zone, aiBlockPos);
        }

        return GetResizeHandleTriangle(zone, aiBlockPos);
    }

    private static DragHandle GetResizeHandleRectangle(AiZone zone, Vec2I aiBlockPos)
    {
        DragHandle dragHandle;

        var zoneRect = GetZoneRect(zone);
        int zoneLeft = (int)(zoneRect.X);
        int zoneRight = (int)(zoneRect.X + zoneRect.Width);
        int zoneTop = (int)(zoneRect.Y);
        int zoneBottom = (int)(zoneRect.Y + zoneRect.Height);
        if (aiBlockPos.X > zoneLeft &&
            aiBlockPos.X < zoneRight - 1 &&
            aiBlockPos.Y > zoneTop &&
            aiBlockPos.Y < zoneBottom - 1)
        {
            dragHandle = DragHandle.None;
        }
        else
        {
            if (aiBlockPos.X == zoneLeft)
            {
                if (aiBlockPos.Y == zoneTop)
                {
                    dragHandle = DragHandle.TopLeft;
                }
                else if (aiBlockPos.Y == zoneBottom - 1)
                {
                    dragHandle = DragHandle.BottomLeft;
                }
                else
                {
                    dragHandle = DragHandle.Left;
                }
            }
            else if (aiBlockPos.X == zoneRight - 1)
            {
                if (aiBlockPos.Y == zoneTop)
                {
                    dragHandle = DragHandle.TopRight;
                }
                else if (aiBlockPos.Y == zoneBottom - 1)
                {
                    dragHandle = DragHandle.BottomRight;
                }
                else
                {
                    dragHandle = DragHandle.Right;
                }
            }
            else
            {
                if (aiBlockPos.Y == zoneTop)
                {
                    dragHandle = DragHandle.Top;
                }
                else
                {
                    dragHandle = DragHandle.Bottom;
                }
            }
        }

        return dragHandle;
    }

    private static DragHandle GetResizeHandleTriangle(AiZone zone, Vec2I aiBlockPos)
    {
        int diagonal;

        var zoneRect = GetZoneRect(zone);
        int zoneLeft = (int)(zoneRect.X);
        int zoneRight = (int)(zoneRect.X + zoneRect.Width);
        int zoneTop = (int)(zoneRect.Y);
        int zoneBottom = (int)(zoneRect.Y + zoneRect.Height);

        switch (zone.Shape)
        {
            case ZoneShape.TriangleTopLeft:
                diagonal = (aiBlockPos.X - zone.X) + (aiBlockPos.Y - zone.Y);
                if (diagonal >= zone.Width - 1) return DragHandle.BottomRight;
                if (aiBlockPos.X == zoneLeft) return DragHandle.Left;
                if (aiBlockPos.Y == zoneTop) return DragHandle.Top;
                break;
            case ZoneShape.TriangleTopRight:
                diagonal = (aiBlockPos.X - zone.X) - (aiBlockPos.Y - zone.Y);
                if (diagonal <= 1 - zone.Width) return DragHandle.BottomLeft;
                if (aiBlockPos.X == zoneRight - 1) return DragHandle.Right;
                if (aiBlockPos.Y == zoneTop) return DragHandle.Top;
                break;
            case ZoneShape.TriangleBottomRight:
                diagonal = (aiBlockPos.X - zone.X) + (aiBlockPos.Y - zone.Y);
                if (diagonal <= 1 - zone.Width) return DragHandle.TopLeft;
                if (aiBlockPos.X == zoneRight - 1) return DragHandle.Right;
                if (aiBlockPos.Y == zoneBottom - 1) return DragHandle.Bottom;
                break;
            case ZoneShape.TriangleBottomLeft:
                diagonal = (aiBlockPos.X - zone.X) - (aiBlockPos.Y - zone.Y);
                if (diagonal >= zone.Width - 1) return DragHandle.TopRight;
                if (aiBlockPos.X == zoneLeft) return DragHandle.Left;
                if (aiBlockPos.Y == zoneBottom - 1) return DragHandle.Bottom;
                break;

            default:
                throw new InvalidOperationException();
        }

        return DragHandle.None;
    }

    public static void Resize(this AiZone zone, Vector2 tilePos, DragHandle handle)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == ZoneShape.Rectangle) ResizeRectangle(zone, handle, aiBlockPos);
        else ResizeTriangle(zone, handle, aiBlockPos);
    }

    private static void ResizeRectangle(AiZone zone, DragHandle dragHandle, Vec2I aiBlockPos)
    {
        var zoneRect = GetZoneRect(zone);
        int zoneLeft = (int)(zoneRect.X);
        int zoneRight = (int)(zoneRect.X + zoneRect.Width);
        int zoneTop = (int)(zoneRect.Y);
        int zoneBottom = (int)(zoneRect.Y + zoneRect.Height);
        var x = aiBlockPos.X;
        var y = aiBlockPos.Y;

        switch (dragHandle)
        {
            case DragHandle.TopLeft:
                if (x >= zoneRight) x = zoneRight - 1;
                if (y >= zoneBottom) y = zoneBottom - 1;

                zone.X = (ushort)x;
                zone.Y = (ushort)y;
                zone.Width = (ushort)(zoneRight - x - 1);
                zone.Height = (ushort)(zoneBottom - y - 1);
                break;
            case DragHandle.Top:
                if (y >= zoneBottom) y = zoneBottom - 1;

                zone.Y = (ushort)y;
                zone.Height = (ushort)(zoneBottom - y - 1);
                break;
            case DragHandle.TopRight:
                if (x < zoneLeft) x = zoneLeft;
                if (y >= zoneBottom) y = zoneBottom - 1;

                zone.Y = (ushort)y;
                zone.Width = (ushort)(x - zoneLeft);
                zone.Height = (ushort)(zoneBottom - y - 1);
                break;
            case DragHandle.Right:
                if (x < zoneLeft) x = zoneLeft;
                zone.Width = (ushort)(x - zoneLeft);
                break;
            case DragHandle.BottomRight:
                if (x < zoneLeft) x = zoneLeft;
                if (y < zoneTop) y = zoneTop;

                zone.Width = (ushort)(x - zoneLeft);
                zone.Height = (ushort)(y - zoneTop);
                break;
            case DragHandle.Bottom:
                if (y < zoneTop) y = zoneTop;
                zone.Height = (ushort)(y - zoneTop);
                break;
            case DragHandle.BottomLeft:
                if (x >= zoneRight) x = zoneRight - 1;
                if (y < zoneTop) y = zoneTop;

                zone.X = (ushort)x;
                zone.Width = (ushort)(zoneRight - x - 1);
                zone.Height = (ushort)(y - zoneTop);
                break;
            case DragHandle.Left:
                if (x >= zoneRight) x = zoneRight - 1;

                zone.X = (ushort)x;
                zone.Width = (ushort)(zoneRight - x - 1);
                break;
        }
    }

    private static void ResizeTriangle(AiZone zone, DragHandle dragHandle, Vec2I aiBlockPos)
    {
        int size;

        var zoneRect = GetZoneRect(zone);
        int zoneLeft = (int)zoneRect.X;
        int zoneRight = (int)(zoneRect.X + zoneRect.Width);
        int zoneTop = (int)zoneRect.Y;
        int zoneBottom = (int)(zoneRect.Y + zoneRect.Height);

        var x = aiBlockPos.X;
        var y = aiBlockPos.Y;

        switch (dragHandle)
        {
            case DragHandle.Top:
                size = zoneBottom - y - 1;
                zone.Y = (ushort)(zoneBottom - size - 1);
                break;
            case DragHandle.Bottom:
                size = y - zoneTop;
                zone.Y = (ushort)(zoneTop + size);
                break;
            case DragHandle.Left:
                size = zoneRight - x - 1;
                zone.X = (ushort)(zoneRight - size - 1);
                break;
            case DragHandle.Right:
                size = x - zoneLeft;
                zone.X = (ushort)(zoneLeft + size);
                break;

            case DragHandle.TopLeft:
                size = (zoneRight - x - 1) + (zoneBottom - y - 1);
                break;
            case DragHandle.TopRight:
                size = (x - zone.X) + (zoneBottom - y - 1);
                break;
            case DragHandle.BottomRight:
                size = (x - zone.X) + (y - zone.Y);
                break;
            case DragHandle.BottomLeft:
                size = (zoneRight - x - 1) + (y - zone.Y);
                break;
            default:
                throw new InvalidOperationException();
        }

        zone.Width = (ushort)Math.Clamp(size, 0, ushort.MaxValue);
    }
}