namespace AdvancedLib.Project;

public record Cup(string CupName, ProjectTrack[] tracks);
public class ProjectConfig
{
    public List<Cup> Cups { get; set; } = new List<Cup>();
}