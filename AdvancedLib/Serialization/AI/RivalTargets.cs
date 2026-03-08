using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.AI;

[MessagePackObject]
public class RivalTargets : ISerializable
{
    [Key(0)] public byte[] Table { get; set; } = new byte[40];

    public void Serialize(Stream stream)
    {
        stream.Write(Table);
    }

    public void Deserialize(Stream stream)
    {
        Table = new byte[40];
        stream.ReadExactly(Table);
    }
}