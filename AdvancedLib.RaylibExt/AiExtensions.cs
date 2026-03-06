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

public static class AiExtensions
{
    public static void Draw(this Checkpoint checkpoint, Color color)
    {
        if (checkpoint.Shape == CheckpointShape.Rectangle)
        {
            var cpRect = new Rectangle(checkpoint.X, checkpoint.Y, checkpoint.Width + 1, checkpoint.Height + 1);
            cpRect.Position *= 8 * Checkpoint.Precision;
            cpRect.Size *= 8 * Checkpoint.Precision;
            Raylib.DrawRectangleRec(cpRect, color with { A = 128 });
            Raylib.DrawRectangleLinesEx(cpRect, 1, Color.White);
        }
        else
        {
            var zoneRect = GetRect(checkpoint);

            zoneRect.Position *= 8 * Checkpoint.Precision;
            zoneRect.Size *= 8 * Checkpoint.Precision;
            DrawTriangle(checkpoint, color);
        }
    }

    public static Rectangle GetRect(this Checkpoint checkpoint)
    {
        if (checkpoint.Shape == CheckpointShape.Rectangle) return new Rectangle(checkpoint.X, checkpoint.Y, checkpoint.Width + 1, checkpoint.Height + 1);
        var cpRect = new Rectangle(checkpoint.X, checkpoint.Y, checkpoint.Width + 1, checkpoint.Width + 1);
        switch (checkpoint.Shape)
        {
            case CheckpointShape.TriangleTopRight:
                cpRect.X -= cpRect.Width - 1;
                break;
            case CheckpointShape.TriangleBottomRight:
                cpRect.X -= cpRect.Width - 1;
                cpRect.Y -= cpRect.Width - 1;
                break;
            case CheckpointShape.TriangleBottomLeft:
                cpRect.Y -= cpRect.Width - 1;
                break;
        }

        return cpRect;
    }

    private static void DrawTriangle(this Checkpoint zone, Color color)
    {
        var points = new Vector2[zone.Width * 2 + 5];
        var scale = Checkpoint.Precision * 8;
        var triRect = GetRect(zone);
        Vector2 vertex, arm;
        Vector2 step;
        switch (zone.Shape)
        {
            case CheckpointShape.TriangleTopLeft:
                vertex = triRect.Position;
                arm = vertex + new Vector2(0, triRect.Height);
                step = new Vector2(1, -1);
                break;
            case CheckpointShape.TriangleTopRight:
                vertex = triRect.Position + new Vector2(triRect.Width, 0);
                arm = vertex + new Vector2(0, triRect.Height);
                step = new Vector2(-1, -1);
                break;
            case CheckpointShape.TriangleBottomLeft:
                vertex = triRect.Position + new Vector2(0, triRect.Height);
                arm = vertex + new Vector2(0, -triRect.Height);
                step = new Vector2(1, 1);
                break;
            case CheckpointShape.TriangleBottomRight:
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
        var pos = arm;
        var even = true;
        for (var i = 1; i < points.Length - 1; i++)
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
        if (zone.Shape == CheckpointShape.TriangleTopRight || zone.Shape == CheckpointShape.TriangleBottomLeft) Array.Reverse(points);

        Raylib.DrawTriangleFan(points, points.Length, color with { A = 96 });

        for (var i = 0; i < points.Length - 1; i++)
        {
            float xAdj = 0, yAdj = 0;
            var point1 = points[i];
            var point2 = points[i + 1];
            if (point1.X == point2.X)
            {
                // Vertical
                if (point1.X == vertex.X)
                {
                    xAdj += zone.Shape switch
                    {
                        CheckpointShape.TriangleBottomLeft => 0.5f,
                        CheckpointShape.TriangleTopLeft => 0.5f,
                        _ => -0.5f
                    };
                }
                else
                {
                    var sign = Math.Sign(vertex.X - point1.X);
                    xAdj += sign * 0.5f;
                }
            }
            else if (point1.Y == point2.Y)
            {
                // Horizontal
                if (point1.Y == vertex.Y)
                {
                    yAdj += zone.Shape switch
                    {
                        CheckpointShape.TriangleBottomLeft => -0.5f,
                        CheckpointShape.TriangleBottomRight => -0.5f,
                        _ => 0.5f
                    };
                }
                else
                {
                    var ySign = Math.Sign(vertex.Y - point1.Y);
                    yAdj += ySign * 0.5f;
                    var xSign = Math.Sign(vertex.X - point1.X);
                    if (!(vertex.X == point1.X || vertex.X == point2.X))
                    {
                        if (Math.Abs(vertex.X - point1.X) < Math.Abs(vertex.X - point2.X))
                            point1.X += xSign;
                        else
                            point2.X += xSign;
                    }
                }
            }

            Raylib.DrawLineEx(point1 + new Vector2(xAdj, yAdj), point2 + new Vector2(xAdj, yAdj), 1, Color.White);
        }
    }

    public static bool IsHovered(this Checkpoint zone, Vector2 tilePos)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == CheckpointShape.Rectangle) return Raylib.CheckCollisionPointRec(aiBlockPos.AsVector2(), GetRect(zone));

        return zone.IntersectsWithTriangle(aiBlockPos);
    }

    private static bool IntersectsWithTriangle(this Checkpoint zone, Vec2I aiBlockPos)
    {
        if (!Raylib.CheckCollisionPointRec(aiBlockPos.AsVector2(), GetRect(zone))) return false;

        var zoneRect = GetRect(zone);
        var x = (int)(aiBlockPos.X - zoneRect.X);
        var y = (int)(aiBlockPos.Y - zoneRect.Y);

        switch (zone.Shape)
        {
            case CheckpointShape.TriangleTopLeft:
                return x + (y - 1) <= zone.Width - 1;
            case CheckpointShape.TriangleTopRight:
                return x >= y;
            case CheckpointShape.TriangleBottomRight:
                return x + 0 + y >= zone.Width;
            case CheckpointShape.TriangleBottomLeft:
                return x <= y;
            default:
                throw new InvalidOperationException();
        }
    }

    public static DragHandle GetResizeHandle(this Checkpoint zone, Vector2 tilePos)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == CheckpointShape.Rectangle) return GetResizeHandleRectangle(zone, aiBlockPos);

        return GetResizeHandleTriangle(zone, aiBlockPos);
    }

    private static DragHandle GetResizeHandleRectangle(Checkpoint zone, Vec2I aiBlockPos)
    {
        DragHandle dragHandle;

        var zoneRect = GetRect(zone);
        var zoneLeft = (int)zoneRect.X;
        var zoneRight = (int)(zoneRect.X + zoneRect.Width);
        var zoneTop = (int)zoneRect.Y;
        var zoneBottom = (int)(zoneRect.Y + zoneRect.Height);
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
                    dragHandle = DragHandle.TopLeft;
                else if (aiBlockPos.Y == zoneBottom - 1)
                    dragHandle = DragHandle.BottomLeft;
                else
                    dragHandle = DragHandle.Left;
            }
            else if (aiBlockPos.X == zoneRight - 1)
            {
                if (aiBlockPos.Y == zoneTop)
                    dragHandle = DragHandle.TopRight;
                else if (aiBlockPos.Y == zoneBottom - 1)
                    dragHandle = DragHandle.BottomRight;
                else
                    dragHandle = DragHandle.Right;
            }
            else
            {
                if (aiBlockPos.Y == zoneTop)
                    dragHandle = DragHandle.Top;
                else
                    dragHandle = DragHandle.Bottom;
            }
        }

        return dragHandle;
    }

    private static DragHandle GetResizeHandleTriangle(Checkpoint zone, Vec2I aiBlockPos)
    {
        int diagonal;

        var zoneRect = GetRect(zone);
        var zoneLeft = (int)zoneRect.X;
        var zoneRight = (int)(zoneRect.X + zoneRect.Width);
        var zoneTop = (int)zoneRect.Y;
        var zoneBottom = (int)(zoneRect.Y + zoneRect.Height);

        switch (zone.Shape)
        {
            case CheckpointShape.TriangleTopLeft:
                diagonal = aiBlockPos.X - zone.X + (aiBlockPos.Y - zone.Y);
                if (diagonal >= zone.Width - 1) return DragHandle.BottomRight;
                if (aiBlockPos.X == zoneLeft) return DragHandle.Left;
                if (aiBlockPos.Y == zoneTop) return DragHandle.Top;
                break;
            case CheckpointShape.TriangleTopRight:
                diagonal = aiBlockPos.X - zone.X - (aiBlockPos.Y - zone.Y);
                if (diagonal <= 1 - zone.Width) return DragHandle.BottomLeft;
                if (aiBlockPos.X == zoneRight - 1) return DragHandle.Right;
                if (aiBlockPos.Y == zoneTop) return DragHandle.Top;
                break;
            case CheckpointShape.TriangleBottomRight:
                diagonal = aiBlockPos.X - zone.X + (aiBlockPos.Y - zone.Y);
                if (diagonal <= 1 - zone.Width) return DragHandle.TopLeft;
                if (aiBlockPos.X == zoneRight - 1) return DragHandle.Right;
                if (aiBlockPos.Y == zoneBottom - 1) return DragHandle.Bottom;
                break;
            case CheckpointShape.TriangleBottomLeft:
                diagonal = aiBlockPos.X - zone.X - (aiBlockPos.Y - zone.Y);
                if (diagonal >= zone.Width - 1) return DragHandle.TopRight;
                if (aiBlockPos.X == zoneLeft) return DragHandle.Left;
                if (aiBlockPos.Y == zoneBottom - 1) return DragHandle.Bottom;
                break;

            default:
                throw new InvalidOperationException();
        }

        return DragHandle.None;
    }

    public static void Resize(this Checkpoint zone, Vector2 tilePos, DragHandle handle)
    {
        var aiBlockPos = (tilePos / 2).ToVec2I();
        if (zone.Shape == CheckpointShape.Rectangle) ResizeRectangle(zone, handle, aiBlockPos);
        else ResizeTriangle(zone, handle, aiBlockPos);
    }

    private static void ResizeRectangle(Checkpoint zone, DragHandle dragHandle, Vec2I aiBlockPos)
    {
        var zoneRect = GetRect(zone);
        var zoneLeft = (int)zoneRect.X;
        var zoneRight = (int)(zoneRect.X + zoneRect.Width);
        var zoneTop = (int)zoneRect.Y;
        var zoneBottom = (int)(zoneRect.Y + zoneRect.Height);
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

    private static void ResizeTriangle(Checkpoint zone, DragHandle dragHandle, Vec2I aiBlockPos)
    {
        int size;

        var zoneRect = GetRect(zone);
        var zoneLeft = (int)zoneRect.X;
        var zoneRight = (int)(zoneRect.X + zoneRect.Width);
        var zoneTop = (int)zoneRect.Y;
        var zoneBottom = (int)(zoneRect.Y + zoneRect.Height);

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
                size = zoneRight - x - 1 + (zoneBottom - y - 1);
                break;
            case DragHandle.TopRight:
                size = x - zone.X + (zoneBottom - y - 1);
                break;
            case DragHandle.BottomRight:
                size = x - zone.X + (y - zone.Y);
                break;
            case DragHandle.BottomLeft:
                size = zoneRight - x - 1 + (y - zone.Y);
                break;
            default:
                throw new InvalidOperationException();
        }

        zone.Width = (ushort)Math.Clamp(size, 0, ushort.MaxValue);
    }
}