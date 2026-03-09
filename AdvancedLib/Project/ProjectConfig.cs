using AdvancedLib.Serialization.OAM;
using AdvancedLib.Serialization.Objects;
using MessagePack;

namespace AdvancedLib.Project;

[MessagePackObject]
public record Cup([property: Key(0)] string Name, [property: Key(1)]  ProjectTrack[] Tracks);

[MessagePackObject]
public class ProjectConfig
{
    [Key(0)] public List<Cup> Cups { get; set; } = new();
    [Key(1)] public ObstacleOam ObstacleOam { get; set; }
}