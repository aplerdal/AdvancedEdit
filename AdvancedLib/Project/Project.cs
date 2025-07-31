using System.Diagnostics;
using System.Formats.Tar;
using System.Text.Json;
using AdvancedLib.Game;
using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Project;

public class Project(string name)
{
    public string Name { get; set; } = name;
    public string Folder => Path.Combine(Path.GetTempPath(), "AdvLib", Name);
    public ProjectConfig Config = new ProjectConfig();
    /// <summary>
    /// Load project from a .amkp file
    /// </summary>
    /// <param name="path">path to the file</param>
    public static Project Unpack(string path)
    {
        var project = new Project(Path.GetFileNameWithoutExtension(path));
        var folder = project.Folder;
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
        Directory.CreateDirectory(folder);
        TarFile.ExtractToDirectory(path, folder, false);

        using var configStream = File.OpenRead(Path.Combine(folder, "config.json"));
        project.Config = JsonSerializer.Deserialize<ProjectConfig>(configStream, JsonSerializerOptions.Default) ?? throw new InvalidOperationException();
        
        return project;
    }

    public void Save(string path)
    {
        var configStream = File.Create(Path.Combine(Folder, "config.json"));
        JsonSerializer.Serialize(configStream, Config, JsonSerializerOptions.Default);
        configStream.Close();
        configStream.Dispose();
        TarFile.CreateFromDirectory(Folder, path, false);
    }
    public static Project FromRom(Stream romStream, string projectName)
    {
        var project = new Project(projectName);
        var folder = Path.Combine(Path.GetTempPath(), "AdvLib", projectName);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }

        Directory.CreateDirectory(folder);
        
        for (var i = 0; i < TrackNames.Cups.Length; i++)
        {
            var cupName = TrackNames.Cups[i];
            ProjectTrack[] cupTracks = new ProjectTrack[4];
            for (int j = 0; j < 4; j++)
            {
                romStream.Seek(RomData.Cups.Address + 16 * i + j * 4, SeekOrigin.Begin);
                var headerIdx = romStream.Read<int>();
                var name = TrackNames.GetTrackNameFromHeaderIndex(headerIdx);
                cupTracks[j] = new ProjectTrack(Path.Combine(folder, name), name);
                cupTracks[j].SaveTrackData(Track.FromRom(romStream, headerIdx));
            }
            project.Config.Cups.Add(new Cup(cupName, cupTracks));
        }
        return project;
    }
}