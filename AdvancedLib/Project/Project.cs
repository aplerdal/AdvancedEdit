using System.Diagnostics;
using System.Formats.Tar;
using AdvancedLib.Game;
using AdvancedLib.Serialization;
using AdvancedLib.Serialization.Objects;
using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Project;

public class Project(string name)
{
    public string Name { get; set; } = name;
    public readonly string Folder = Path.Combine(Path.GetTempPath(), "AdvLib", Guid.NewGuid().ToString("N"));
    public ProjectConfig Config = new();

    /// <summary>
    /// Load project from a .amkp file
    /// </summary>
    /// <param name="path">path to the file</param>
    public static Project Unpack(string path)
    {
        var project = new Project(Path.GetFileNameWithoutExtension(path));
        if (Directory.Exists(project.Folder)) Directory.Delete(project.Folder, true);

        Directory.CreateDirectory(project.Folder);
        TarFile.ExtractToDirectory(path, project.Folder, false);

        using var configStream = File.OpenRead(Path.Combine(project.Folder, "config.msp"));
        project.Config = MessagePackSerializer.Deserialize<ProjectConfig>(configStream);

        foreach (var cup in project.Config.Cups)
        foreach (var track in cup.Tracks)
            track.ResolveFolder(Path.Combine(project.Folder, cup.Name));

        return project;
    }

    public void Save(string path)
    {
        using var configStream = File.Create(Path.Combine(Folder, "config.msp"));
        Debug.Assert(Config.ObstacleOam != null);
        MessagePackSerializer.Serialize(configStream, Config);
        configStream.Close();
        if (File.Exists(path)) File.Delete(path);
        TarFile.CreateFromDirectory(Folder, path, false);
    }

    public void ToRom(Stream stream)
    {
        // Apply Patches
        Patcher.Apply("Resources/Patches/objRework.ips", stream);
        Patcher.Apply("Resources/Patches/headerLaps.ips", stream);
        Patcher.Apply("Resources/Patches/fixFinalTrackCheck.ips", stream);
        Patcher.Apply("Resources/Patches/fixMinimapScale.ips", stream);

        var headerIdx = 0;
        stream.Seek(new Pointer(0x08400000));
        for (var cupIdx = 0; cupIdx < Config.Cups.Count; cupIdx++)
        {
            var cup = Config.Cups[cupIdx];
            foreach (var projectTrack in cup.Tracks)
            {
                var track = projectTrack.LoadTrackData();
                var trackAddress = stream.Position;
                track.WriteTrack(stream, headerIdx);
                var trackEnd = stream.Position;
                var addr = RomData.Cups.Address + headerIdx * 4;
                if (cupIdx > 4) addr += 16;
                stream.Seek(addr, SeekOrigin.Begin);
                stream.Write(headerIdx);
                stream.Seek(RomData.TrackOffsets.Address + headerIdx * 4, SeekOrigin.Begin);
                stream.Write((uint)trackAddress - RomData.TrackOffsets.Address);
                stream.Seek(trackEnd, SeekOrigin.Begin);
                headerIdx++;
            }
        }

        // Podium
        {
            var projectTrack = new ProjectTrack("Podium");
            projectTrack.ResolveFolder(Folder);
            var track = projectTrack.LoadTrackData();
            var trackAddress = stream.Position;
            track.WriteTrack(stream, headerIdx);
            var trackEnd = stream.Position;
            var addr = RomData.Cups.Address + 5 * 16;
            stream.Seek(addr, SeekOrigin.Begin);
            stream.Write(headerIdx);
            stream.Write(headerIdx);
            stream.Write(headerIdx);
            stream.Write(headerIdx);
            stream.Seek(RomData.PodiumHeaderIdx);
            stream.Write((byte)headerIdx);
            stream.Seek(RomData.TrackOffsets.Address + headerIdx * 4, SeekOrigin.Begin);
            stream.Write((uint)trackAddress - RomData.TrackOffsets.Address);
            stream.Seek(trackEnd, SeekOrigin.Begin);
        }
    }

    public static async Task<Project> FromRomAsync(Stream romStream, string projectName)
    {
        var project = new Project(projectName);
        if (Directory.Exists(project.Folder)) Directory.Delete(project.Folder, true);

        Directory.CreateDirectory(project.Folder);

        for (int i = 0; i < TrackNames.Cups.Length; i++)
        {
            var cupName = TrackNames.Cups[i];
            if (cupName == "Podium")
            {
                romStream.Seek(RomData.Cups.Address + 16 * i, SeekOrigin.Begin);
                var headerIdx = romStream.Read<int>();
                var podiumTrack = new ProjectTrack("Podium");
                podiumTrack.ResolveFolder(Path.Combine(project.Folder));
                await podiumTrack.SaveTrackDataAsync(Track.FromRom(romStream, headerIdx));
                continue;
            }

            var cupTracks = new ProjectTrack[4];
            for (var j = 0; j < 4; j++)
            {
                romStream.Seek(RomData.Cups.Address + 16 * i + j * 4, SeekOrigin.Begin);
                var headerIdx = romStream.Read<int>();
                var name = TrackNames.GetTrackNameFromHeaderIndex(headerIdx);
                cupTracks[j] = new ProjectTrack(name);
                cupTracks[j].ResolveFolder(Path.Combine(project.Folder, cupName));
                await cupTracks[j].SaveTrackDataAsync(Track.FromRom(romStream, headerIdx));
            }

            project.Config.Cups.Add(new Cup(cupName, cupTracks));
        }

        var themes = Enum.GetNames(typeof(RetroTheme));
        for (int i = 0; i < themes.Length; i++)
        {
            var theme = (RetroTheme)i;
            var themeName = themes[i];
            var headerIdx = theme switch
            {
                RetroTheme.GhostValley => 34,
                RetroTheme.MarioCircuit => 32,
                RetroTheme.VanillaLake => 44,
                RetroTheme.DonutPlains => 33,
                RetroTheme.ChocoIsland => 37,
                RetroTheme.KoopaBeach => 42,
                RetroTheme.BowserCastle => 35,
                RetroTheme.RainbowRoad => 51,
                _ => 34,
            };
            var themeBaseTrack = new ProjectTrack(themeName);
            themeBaseTrack.ResolveFolder(Path.Combine(project.Folder, "themeBase"));
            await themeBaseTrack.SaveTrackDataAsync(Track.FromRom(romStream, headerIdx));
        }

        {
            var oamData = new ObstacleOam(romStream);
            project.Config.ObstacleOam = oamData;
            Debug.Assert(project.Config.ObstacleOam != null);
        }

        return project;
    }
}