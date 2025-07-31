using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public enum ObjectMode
{
    Regular,
    Affine,
    Hide,
    AffineDouble,
}
public enum GraphicsMode
{
    Normal,
    AlphaBlend,
    Window,
}
public class OamAttribute : ISerializable, IEquatable<OamAttribute>
{
    public byte YPosition { get; set; }
    public ObjectMode ObjectMode { get; set; }
    public GraphicsMode GraphicsMode { get; set; }
    public bool Mosaic { get; set; }
    public bool Is8Bit { get; set; }
    public byte SpriteShape { get; set; }
    public ushort XPosition { get; set; }
    public byte AffineIndex { get; set; }
    public bool HorizontalFlip { get; set; }
    public bool VerticalFlip { get; set; }
    public byte SpriteSize { get; set; }
    public ushort TileIndex { get; set; }
    public byte Priority { get; set; }
    public byte Palette { get; set; }
    public void Serialize(Stream stream) {
        ushort attr0 = (ushort)(
            (YPosition&0xff) | 
            (((int)ObjectMode&0b11)<<8) | 
            (((int)GraphicsMode&0b11)<<10) |
            ((Mosaic?1:0)<<12) |
            ((Is8Bit?1:0)<<13) |
            ((SpriteShape&0b11)<<14)
        );
        ushort attr1 = (ushort)(
            (XPosition&0x1ff) |
            ((SpriteSize&0b11)<<14)
        );
        if (ObjectMode == ObjectMode.Affine || ObjectMode == ObjectMode.AffineDouble) {
            attr1 |= (ushort)((AffineIndex&0b11111)<<9);
        } else {
            attr1 |= (ushort)(((HorizontalFlip?1:0)<<12) | ((VerticalFlip?1:0)<<13));
        }
        ushort attr2 = (ushort)(
            (TileIndex & 0x3ff) |
            ((Priority&0b11)<<10) |
            ((Palette&0b1111)<<12)
        );
        stream.Write(attr0);
        stream.Write(attr1);
        stream.Write(attr2);
    }
    public void Deserialize(Stream stream) {
        var attr0 = stream.ReadUInt16();
        YPosition = (byte)(attr0 & 0xff);
        ObjectMode = (ObjectMode)((attr0>>8) & 0b11);
        GraphicsMode = (GraphicsMode)((attr0>>10) & 0b11);
        Mosaic = ((attr0>>12) & 1) != 0;
        Is8Bit = ((attr0>>13) & 1) != 0;
        SpriteShape = (byte)((attr0>>14) & 0b11);

        var attr1 = stream.ReadUInt16();
        XPosition = (ushort)(attr1 & 0x1ff);
        if (ObjectMode == ObjectMode.Affine || ObjectMode == ObjectMode.AffineDouble) {
            AffineIndex = (byte)((attr1>>9) & 0b11111);
        } else {
            HorizontalFlip = ((attr1>>12) & 1) != 0;
            VerticalFlip = ((attr1>>13) & 1) != 0;
        }
        SpriteSize = (byte)((attr1>>14) & 0b11);
        
        var attr2 = stream.ReadUInt16();
        TileIndex = (ushort)(attr2 & 0x3ff);
        Priority = (byte)((attr2>>10)&0b11);
        Palette = (byte)((attr2>>12)&0b1111);
    }

    public bool Equals(OamAttribute? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return YPosition == other.YPosition && ObjectMode == other.ObjectMode && GraphicsMode == other.GraphicsMode && Mosaic == other.Mosaic && Is8Bit == other.Is8Bit && SpriteShape == other.SpriteShape && XPosition == other.XPosition && AffineIndex == other.AffineIndex && HorizontalFlip == other.HorizontalFlip && VerticalFlip == other.VerticalFlip && SpriteSize == other.SpriteSize && TileIndex == other.TileIndex && Priority == other.Priority && Palette == other.Palette;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OamAttribute)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(YPosition);
        hashCode.Add((int)ObjectMode);
        hashCode.Add((int)GraphicsMode);
        hashCode.Add(Mosaic);
        hashCode.Add(Is8Bit);
        hashCode.Add(SpriteShape);
        hashCode.Add(XPosition);
        hashCode.Add(AffineIndex);
        hashCode.Add(HorizontalFlip);
        hashCode.Add(VerticalFlip);
        hashCode.Add(SpriteSize);
        hashCode.Add(TileIndex);
        hashCode.Add(Priority);
        hashCode.Add(Palette);
        return hashCode.ToHashCode();
    }
}