using AdvancedLib.Serialization.Tracks;

namespace AdvancedLib.Serialization.AI;

public class TrackAi : ISerializable
{
    private const int DefaultSets = 3;
    public List<AiZone> Zones { get; set; } = new();
    public List<AiTarget[]> TargetSets { get; set; } = new();
    public void Serialize(Stream stream)
    {
        var header = new AiHeader
        {
            ZoneCount = (byte)Zones.Count,
            ZonesOffset = 5,
            TargetsOffset = (ushort)(5 + AiZone.Size * Zones.Count)
        };
        stream.Write(header);
        foreach (var zone in Zones)
            stream.Write(zone);
        foreach (var set in TargetSets)
        foreach (var target in set)
            stream.Write(target);
    }

    public void Deserialize(Stream stream)
    {
        var basePos = stream.Position;
        var header = stream.Read<AiHeader>();
        stream.Seek(basePos + header.ZonesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.ZoneCount; i++)
            Zones.Add(stream.Read<AiZone>());
        stream.Seek(basePos + header.TargetsOffset, SeekOrigin.Begin);
        for (int i = 0; i < DefaultSets; i++)
        {
            var set = new AiTarget[header.ZoneCount];
            for (var j = 0; j < set.Length; j++)
            {
                set[j] = stream.Read<AiTarget>();
            }
            TargetSets.Add(set);
        }
    }

    public byte[] GenerateZoneMap(int trackWidth)
    {
        var aiMapSize = trackWidth * 64;
        var aiMap = new byte[aiMapSize * aiMapSize];
        Array.Fill(aiMap, (byte)0x7F);
        for (var i = 0; i < Zones.Count; i++)
        {
            var id = i;
            if (id == 0 || id == Zones.Count - 1)
                id |= 0x80;
            Zones[i].WriteZoneMap((byte)id, ref aiMap, aiMapSize);
        }
        return aiMap;
    }
}