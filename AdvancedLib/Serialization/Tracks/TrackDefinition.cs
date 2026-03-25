using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

public class TrackDefinition : ISerializable, IEquatable<TrackDefinition>
{
     public int HeaderIndex { get; set; }
     public uint BackgroundIndex { get; set; }
     public uint BackgroundBehavior { get; set; }
     public uint PaletteBehavior { get; set; }
     public uint Theme { get; set; }
     public Pointer TurnSigns { get; set; }
     public uint SongID { get; set; }
     public Pointer TargetTimes { get; set; }
     public Pointer RivalTargets { get; set; }
     public Pointer CoverGfx { get; set; }
     public Pointer CoverPalette { get; set; }
     public Pointer LockedCoverPal { get; set; }
     public Pointer TrackNameGfx { get; set; }
     public uint LapsCount { get; set; }

    public void Deserialize(Stream stream)
    {
        HeaderIndex = stream.ReadInt32();
        BackgroundIndex = stream.ReadUInt32();
        BackgroundBehavior = stream.ReadUInt32();
        PaletteBehavior = stream.ReadUInt32();
        Theme = stream.ReadUInt32();
        TurnSigns = new Pointer(stream.ReadUInt32());
        SongID = stream.ReadUInt32();
        RivalTargets = new Pointer(stream.ReadUInt32());
        TargetTimes = new Pointer(stream.ReadUInt32());
        CoverGfx = new Pointer(stream.ReadUInt32());
        CoverPalette = new Pointer(stream.ReadUInt32());
        LockedCoverPal = new Pointer(stream.ReadUInt32());
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
        stream.Write(TurnSigns.Raw);
        stream.Write(SongID);
        stream.Write(RivalTargets.Raw);
        stream.Write(TargetTimes.Raw);
        stream.Write(CoverGfx.Raw);
        stream.Write(CoverPalette.Raw);
        stream.Write(LockedCoverPal.Raw);
        stream.Write(TrackNameGfx.Raw);
        stream.Write(LapsCount);
    }

    public bool Equals(TrackDefinition? other)
    {
        return other != null && HeaderIndex == other.HeaderIndex && BackgroundIndex == other.BackgroundIndex && BackgroundBehavior == other.BackgroundBehavior && PaletteBehavior == other.PaletteBehavior && Theme == other.Theme && TurnSigns.Raw == other.TurnSigns.Raw && SongID == other.SongID && RivalTargets.Raw == other.RivalTargets.Raw && CoverGfx.Raw == other.CoverGfx.Raw && CoverPalette.Raw == other.CoverPalette.Raw && LockedCoverPal.Raw == other.LockedCoverPal.Raw && TrackNameGfx.Raw == other.TrackNameGfx.Raw && LapsCount == other.LapsCount;
    }
}