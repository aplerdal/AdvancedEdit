namespace AdvancedLib;
public struct Pointer(uint value)
{
    public uint Raw { get; set; } = (value & 0xffffff) | 0x08000000;

    public uint Address
    {
        get=>Raw & 0xffffff; 
        set=>Raw = (value & 0xffffff) | 0x08000000;
    }

    public static Pointer Null = new Pointer(0);
    public bool IsNull => Raw == 0;
}