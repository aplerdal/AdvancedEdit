using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.AI;

public enum CheckpointShape
{
    Rectangle,
    TriangleTopLeft,
    TriangleTopRight,
    TriangleBottomRight,
    TriangleBottomLeft
}

[MessagePackObject]
public class Checkpoint : ISerializable, IEquatable<Checkpoint>
{
    public static readonly int Precision = 2;
    public static readonly int Size = 12;

    [Key(0)] public CheckpointShape Shape { get; set; }

    [Key(1)] public ushort X { get; set; }

    [Key(2)] public ushort Y { get; set; }

    [Key(3)] public ushort Width { get; set; }

    [Key(4)] public ushort Height { get; set; }

    public Checkpoint()
    {
    }

    public static Checkpoint Default => new(0, 0, 16, 16, CheckpointShape.Rectangle);

    public Checkpoint(ushort x, ushort y, ushort width, ushort height, CheckpointShape shape)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Shape = shape;
    }

    public void Serialize(Stream stream)
    {
        stream.Write((byte)Shape);
        stream.Write(X);
        stream.Write(Y);
        stream.Write(Width);
        stream.Write(Height);
        stream.Write([0, 0, 0]);
    }

    public void Deserialize(Stream stream)
    {
        Shape = (CheckpointShape)stream.ReadUInt8();
        X = stream.ReadUInt16();
        Y = stream.ReadUInt16();
        Width = stream.ReadUInt16();
        Height = stream.ReadUInt16();
        stream.Skip(3);
    }

    public void WriteZoneMap(byte id, ref byte[] map, int mapWidth)
    {
        var posX = X;
        var posY = Y;
        switch (Shape)
        {
            case CheckpointShape.Rectangle:
            {
                for (var y = 0; y <= Height; y++)
                {
                    var destY = posY + y;
                    if (destY >= mapWidth) continue;

                    for (var x = 0; x <= Width; x++)
                    {
                        var destX = posX + x;
                        if (destX >= mapWidth) continue;

                        map[destX + destY * mapWidth] = id;
                    }
                }
            }
                break;
            case CheckpointShape.TriangleTopLeft:
            case CheckpointShape.TriangleTopRight:
            case CheckpointShape.TriangleBottomRight:
            case CheckpointShape.TriangleBottomLeft:
            {
                var height = Width + 1;
                for (var dy = 0; dy < height; dy++)
                {
                    int rowY;
                    if (Shape == CheckpointShape.TriangleTopLeft || Shape == CheckpointShape.TriangleTopRight)
                        rowY = posY + dy;
                    else
                        rowY = posY - dy;
                    if (rowY >= mapWidth) continue;

                    var length = height - dy;
                    for (var dx = 0; dx < length; dx++)
                    {
                        int colX;
                        if (Shape == CheckpointShape.TriangleTopLeft || Shape == CheckpointShape.TriangleBottomLeft)
                            colX = posX + dx;
                        else
                            colX = posX - dx;
                        if (colX >= mapWidth) continue;

                        map[colX + rowY * mapWidth] = id;
                    }
                }
            }
                break;
        }
    }

    public bool Equals(Checkpoint? other)
    {
        return other != null && Shape == other.Shape && X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }
}