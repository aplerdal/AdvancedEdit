using MessagePack;

namespace AdvancedLib.Project;

[MessagePackObject(keyAsPropertyName: true)]
public record Cup(string Name, ProjectTrack[] Tracks);

[MessagePackObject(keyAsPropertyName: true)]
public class ProjectConfig
{
    public List<Cup> Cups { get; set; } = new List<Cup>();
}