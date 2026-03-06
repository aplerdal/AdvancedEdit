using MessagePack;

namespace AdvancedLib.Serialization.AI;

[MessagePackObject]
public class TrackAi : ISerializable
{
    private const int DefaultSets = 3;
    [Key(0)]
    public List<Checkpoint> Checkpoints { get; set; } = new();
    [Key(1)]
    public List<List<AiTarget>> TargetSets { get; set; } = new();

    public void Serialize(Stream stream)
    {
        var header = new AiHeader
        {
            CheckpointCount = (byte)Checkpoints.Count,
            CheckpointsOffset = 5,
            TargetsOffset = (ushort)(5 + Checkpoint.Size * Checkpoints.Count)
        };
        stream.Write(header);
        foreach (var checkpoint in Checkpoints)
            stream.Write(checkpoint);
        foreach (var set in TargetSets)
        foreach (var target in set)
            stream.Write(target);
    }

    public void Deserialize(Stream stream)
    {
        var basePos = stream.Position;
        var header = stream.Read<AiHeader>();
        stream.Seek(basePos + header.CheckpointsOffset, SeekOrigin.Begin);
        for (var i = 0; i < header.CheckpointCount; i++)
            Checkpoints.Add(stream.Read<Checkpoint>());
        stream.Seek(basePos + header.TargetsOffset, SeekOrigin.Begin);
        for (var i = 0; i < DefaultSets; i++)
        {
            var set = new List<AiTarget>(header.CheckpointCount);
            for (var j = 0; j < header.CheckpointCount; j++) set.Add(stream.Read<AiTarget>());

            TargetSets.Add(set);
        }
    }

    public byte[] GenerateCheckpointMap(int trackWidth)
    {
        var aiMapSize = trackWidth * 64;
        var aiMap = new byte[aiMapSize * aiMapSize];
        Array.Fill(aiMap, (byte)0x7F);
        for (var i = 0; i < Checkpoints.Count; i++)
        {
            var id = i;
            if (id == 0 || id == Checkpoints.Count - 1)
                id |= 0x80;
            Checkpoints[i].WriteZoneMap((byte)id, ref aiMap, aiMapSize);
        }

        return aiMap;
    }
}