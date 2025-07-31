namespace AdvancedLib.Serialization.Allocator;

public static class RomAllocator
{
    private static AllocationTable _allocationTable = new AllocationTable([
        new RomSpan(0x1E9E00,0x4A300),
        new RomSpan(0x400100, 0x1C00000),
    ]);
    /// <summary>
    /// Allocate a block of ROM space
    /// </summary>
    /// <param name="length">Length of allocation in bytes</param>
    /// <returns>Address of allocated block</returns>
    public static Pointer Allocate(uint length)
    {
        uint alignedLength = (uint)((length & ~3) + 4);
        uint? address = null;
        for (var i = 0; i < _allocationTable.Blocks.Count; i++)
        {
            var span = _allocationTable.Blocks[i];
            if (span.Length <= alignedLength)
            {
                address = span.Address;
                span.Address += alignedLength;
                span.Length -= alignedLength;
                if (span.Length == 0) _allocationTable.Blocks.RemoveAt(i);
                break;
            }
        }
        if (address is null) throw new OutOfMemoryException("There is not enough rom space remaining to allocate that much space.");
        return new Pointer(address.Value);
    }
    public static void AddFreeBlock(uint address, uint length)
    {
        _allocationTable.Blocks.Add(new RomSpan(address, length));
    }
    
}