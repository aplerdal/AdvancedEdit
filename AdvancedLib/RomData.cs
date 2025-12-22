namespace AdvancedLib;

public enum Region
{
    USA,
    JPN,
    PAL,
    CHN,
}

public static class RomData
{
    public static Region Region { get; set; } = Region.USA;

    public static Pointer Cups => Region switch
    {
        Region.USA => new Pointer(0x080E7464),
        _ => Pointer.Null,
    };

    public static Pointer TrackOffsets => Region switch
    {
        Region.USA => new Pointer(0x08258000),
        Region.PAL => new Pointer(0x08258000),
        _ => Pointer.Null,
    };
}