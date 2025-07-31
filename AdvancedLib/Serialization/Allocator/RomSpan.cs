namespace AdvancedLib.Serialization.Allocator;

public class RomSpan(uint address, uint length)
{
    public uint Address { get; set; } = address;
    public uint Length { get; set; } = length;
    public uint End => Address + Length;
}