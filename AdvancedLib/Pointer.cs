using MessagePack;

namespace AdvancedLib;

[MessagePackObject]
public struct Pointer(uint value)
{
    [Key(0)] public uint Raw { get; set; } = (value & 0xffffff) | 0x08000000;

    [IgnoreMember]
    public uint Address
    {
        get => Raw & 0xffffff;
        set => Raw = (value & 0xffffff) | 0x08000000;
    }

    public static Pointer Null = new(0);
    [IgnoreMember] public bool IsNull => (Raw & 0xffffff) == 0;
}