using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.Tracks;

[MessagePackObject(keyAsPropertyName: true)]
public class TrackDefinition : ISerializable, IEquatable<TrackDefinition>
{
    public int HeaderIndex { get; set; }
    public uint BackgroundIndex { get; set; }
    public uint BackgroundBehavior { get; set; }
    public uint PaletteBehavior { get; set; }
    public uint Theme { get; set; }
    public Pointer Turns { get; set; }
    public uint SongID { get; set; }
    public Pointer TargetOptions { get; set; }
    public Pointer CoverGfx { get; set; }
    public Pointer CoverPal { get; set; }
    public Pointer LockedTrackPal { get; set; }
    public Pointer TrackNameGfx { get; set; }
    public uint LapsCount { get; set; }

    public void Deserialize(Stream stream)
    {
        HeaderIndex = stream.ReadInt32();
        BackgroundIndex = stream.ReadUInt32();
        BackgroundBehavior = stream.ReadUInt32();
        PaletteBehavior = stream.ReadUInt32();
        Theme = stream.ReadUInt32();
        Turns = new Pointer(stream.ReadUInt32());
        SongID = stream.ReadUInt32();
        TargetOptions = new Pointer(stream.ReadUInt32());
        stream.Skip(4);
        CoverGfx = new Pointer(stream.ReadUInt32());
        CoverPal = new Pointer(stream.ReadUInt32());
        LockedTrackPal = new Pointer(stream.ReadUInt32());
        TrackNameGfx = new Pointer(stream.ReadUInt32());
        LapsCount = stream.ReadUInt32();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(HeaderIndex);
        stream.Write(BackgroundIndex);
        stream.Write(BackgroundBehavior);
        stream.Write(PaletteBehavior);
        stream.Write(Theme);
        stream.Write(Turns.Raw);
        stream.Write(SongID);
        stream.Write(TargetOptions.Raw);
        stream.Write((uint)0);
        stream.Write(CoverGfx.Raw);
        stream.Write(CoverPal.Raw);
        stream.Write(LockedTrackPal.Raw);
        stream.Write(TrackNameGfx.Raw);
        stream.Write(LapsCount);
    }

    public bool Equals(TrackDefinition? other)
    {
        return other != null && HeaderIndex == other.HeaderIndex && BackgroundIndex == other.BackgroundIndex && BackgroundBehavior == other.BackgroundBehavior && PaletteBehavior == other.PaletteBehavior && Theme == other.Theme && Turns.Raw == other.Turns.Raw && SongID == other.SongID && TargetOptions.Raw == other.TargetOptions.Raw && CoverGfx.Raw == other.CoverGfx.Raw && CoverPal.Raw == other.CoverPal.Raw && LockedTrackPal.Raw == other.LockedTrackPal.Raw && TrackNameGfx.Raw == other.TrackNameGfx.Raw && LapsCount == other.LapsCount;
    }
}