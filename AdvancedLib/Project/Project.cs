using System.Formats.Tar;
using AdvancedLib.Game;
using AdvancedLib.Serialization;
using AuroraLib.Core.IO;
using MessagePack;

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

        using var configStream = File.OpenRead(Path.Combine(folder, "config.msp"));
        project.Config = MessagePackSerializer.Deserialize<ProjectConfig>(configStream);

        return project;
    }

    public void Save(string path)
    {
        var configStream = File.Create(Path.Combine(Folder, "config.msp"));
        MessagePackSerializer.Serialize(configStream, Config);
        configStream.Dispose();
        if (File.Exists(path)) File.Delete(path);
        TarFile.CreateFromDirectory(Folder, path, false);
    }

    public void ToRom(Stream stream)
    {
        // Apply Patches
        Patcher.Apply("Resources/Patches/objRework.ips", stream);

        int headerIdx = 0;
        stream.Seek(new Pointer(0x08400000));
        foreach (var cup in Config.Cups)
        foreach (var projectTrack in cup.Tracks)
        {
            var track = projectTrack.LoadTrackData();
            var trackAddress = stream.Position;
            track.WriteTrack(stream, headerIdx);
            var trackEnd = stream.Position;
            stream.Seek(RomData.Cups.Address + headerIdx * 4, SeekOrigin.Begin);
            stream.Write(headerIdx);
            stream.Seek(RomData.TrackOffsets.Address + headerIdx * 4, SeekOrigin.Begin);
            stream.Write((uint)trackAddress - RomData.TrackOffsets.Address);
            stream.Seek(trackEnd, SeekOrigin.Begin);
            headerIdx++;
        }
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
            if (cupName == "Victory") continue; // TODO: Editable Podium Track
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