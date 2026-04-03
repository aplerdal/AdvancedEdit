using System.Drawing;

namespace AdvancedLib;

public enum Region
{
    USA,
    JPN,
    CHN
}

public static class RomData
{
    public static Region Region { get; set; } = Region.USA;

    public static Pointer Cups => Region switch
    {
        Region.USA => new Pointer(0x080E7464),
        _ => throw new IndexOutOfRangeException("Pointer not available for this region")
    };

    public static Pointer PodiumHeaderIdx => Region switch
    {
        Region.USA => new Pointer(0x08028e0e),
        _ => throw new IndexOutOfRangeException("Pointer not available for this region")
    };

    public static Pointer TrackOffsets => Region switch
    {
        Region.USA => new Pointer(0x08258000),
        _ => Pointer.Null
    };
}