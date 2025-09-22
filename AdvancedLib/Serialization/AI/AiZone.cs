using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.AI;

public enum ZoneShape
{
    Rectangle,
    TriangleTopLeft,
    TriangleTopRight,
    TriangleBottomRight,
    TriangleBottomLeft,
}

[MessagePackObject(keyAsPropertyName: true)]
public class AiZone : ISerializable, IEquatable<AiZone>
{
    public static int Precision = 2;
    public static int Size = 12;
    public ZoneShape Shape { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public AiZone() { }
    public static AiZone Default => new(0, 0, 16, 16, ZoneShape.Rectangle);
    public AiZone(ushort x, ushort y, ushort width, ushort height, ZoneShape shape)
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
        stream.Write([0,0,0]);
    }

    public void Deserialize(Stream stream)
    {
        Shape = (ZoneShape)stream.ReadUInt8();
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
            case ZoneShape.Rectangle:
            {
                for (int y = 0; y <= Height; y++)
                {
                    int destY = posY + y;
                    if (destY >= mapWidth) continue;

                    for (int x = 0; x <= Width; x++)
                    {
                        int destX = posX + x;
                        if (destX >= mapWidth) continue;

                        map[destX + destY * mapWidth] = id;
                    }
                }
            } break;
            case ZoneShape.TriangleTopLeft:
            case ZoneShape.TriangleTopRight:
            case ZoneShape.TriangleBottomRight:
            case ZoneShape.TriangleBottomLeft:
            {
                var height = Width + 1;
                for (int dy = 0; dy < height; dy++)
                {
                    int rowY;
                    if (Shape == ZoneShape.TriangleTopLeft || Shape == ZoneShape.TriangleTopRight)
                        rowY = posY + dy;
                    else
                        rowY = posY - dy;
                    if (rowY >= mapWidth) continue;

                    int length = height - dy;
                    for (int dx = 0; dx < length; dx++)
                    {
                        int colX;
                        if (Shape == ZoneShape.TriangleTopLeft || Shape == ZoneShape.TriangleBottomLeft)
                            colX = posX + dx;
                        else
                            colX = posX - dx;
                        if (colX >= mapWidth) continue;

                        map[colX + rowY * mapWidth] = id;
                    }
                }
            } break;
        }
    }

    public bool Equals(AiZone other)
    {
        return Shape == other.Shape && X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is AiZone other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Shape, X, Y, Width, Height);
    }
}