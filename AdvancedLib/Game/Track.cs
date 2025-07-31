using System.Text.Json;
using AdvancedLib.Graphics;
using AdvancedLib.Serialization;
using AdvancedLib.Serialization.AI;
using AdvancedLib.Serialization.Objects;
using AdvancedLib.Serialization.Tracks;
using AuroraLib.Core.IO;

namespace AdvancedLib.Game;

/// <summary>
/// An object for handling track data
/// </summary>
public class Track
{
    public required TrackConfig Config { get; set; }
    public required Tileset Tileset { get; set; }
    public required Palette TilesetPalette { get; set; }
    public required AffineTilemap Tilemap { get; set; }
    public required Tileset Minimap { get; set; }
    public required Tileset? ObstacleGfx { get; set; }
    public required TrackObjects Objects { get; set; }
    public required TrackAi Ai { get; set; }
    public required byte[] Behaviors { get; set; }

    /// <summary>
    /// Initialize empty <see cref="Track"/> object
    /// </summary>
    public Track() { }
    
    /// <summary>
    /// Load <see cref="Track"/> from stream
    /// </summary>
    /// <param name="stream">A stream containing the game ROM</param>
    /// <param name="headerIndex">The index of the header for the track being loaded</param>
    public static Track FromRom(Stream stream, int headerIndex)
    {
        var definition = LoadDefinition(stream, headerIndex);
        var header = LoadHeader(stream, definition.HeaderIndex);
        return new Track()
        {
            Config = LoadConfig(header, definition),
            TilesetPalette = LoadTilesetPalette(stream, header), // TODO: Do this for the other constructor and writing palette to rom
            Tileset = LoadTileset(stream, header, headerIndex),
            Tilemap = LoadTilemap(stream, header),
            Minimap = LoadMinimap(stream, header),
            Behaviors = LoadBehaviors(stream, header),
            ObstacleGfx = LoadObstacleGraphics(stream, header, headerIndex),
            Objects = LoadObjects(stream, header, headerIndex),
            Ai = LoadAi(stream, header, definition),
        };
    }
    
    /// <summary>
    /// Default track object
    /// </summary>
    public static Track Default => new Track
    {
        Config = TrackConfig.Default,
        Tileset = new Tileset(256, PixelFormat.Bpp8),
        TilesetPalette = new Palette(64),
        Tilemap = new AffineTilemap(TrackConfig.Default.Size.X * 128, TrackConfig.Default.Size.Y * 128),
        Minimap = new Tileset(64, PixelFormat.Bpp4),
        ObstacleGfx = new Tileset(256, PixelFormat.Bpp4),
        Objects = new TrackObjects(), // TODO: Default starting positions
        Behaviors = new byte[256],
        Ai = new TrackAi(),
    };
    
    #region Load Track Data
    
    private static TrackDefinition LoadDefinition(Stream reader, int index)
    {
        const uint definitionPointerTableAddress = 0x0E7FEC;
        reader.Seek(definitionPointerTableAddress + index * 4, SeekOrigin.Begin);
        var definitionAddress = reader.Read<Pointer>();
        if (definitionAddress.IsNull) throw new ArgumentOutOfRangeException(nameof(index), "Invalid header index");
        reader.Seek(definitionAddress);
        return reader.Read<TrackDefinition>();
    }
    private static TrackHeader LoadHeader(Stream reader, int index)
    {
        const uint trackTableAddress = 0x258000;
        reader.Seek(trackTableAddress + index * 4, SeekOrigin.Begin);
        var offset = reader.ReadUInt32();
        reader.Seek(trackTableAddress + offset, SeekOrigin.Begin);
        return reader.Read<TrackHeader>();
    }
    private static TrackConfig LoadConfig(TrackHeader header, TrackDefinition definition)
    {
        return new TrackConfig
        {
            Size = new Vec2I(header.TrackWidth, header.TrackHeight),
            BackgroundIndex = definition.BackgroundIndex,
            BackgroundBehavior = definition.BackgroundBehavior,
            PaletteBehavior = definition.PaletteBehavior,
            Theme = definition.Theme,
            SongID = definition.SongID,
            Laps = definition.LapsCount,
        };
    }

    private static Palette LoadTilesetPalette(Stream reader, TrackHeader header)
    {
        var palAddress = header.Address + header.TilesetPaletteOffset;
        reader.Seek(palAddress, SeekOrigin.Begin);
        return new Palette(reader, 64);
    }
    
    private static Tileset LoadTileset(Stream reader, TrackHeader header, int headerIndex)
    {
        const int tilesetSize = 256;
        
        using var tilesetStream = new MemoryPoolStream(tilesetSize * Tile.Size * Tile.Size, true);
        if (header.SharedTileset != 0)
        {
            var sharedTrackDef = LoadDefinition(reader, headerIndex + header.SharedTileset);
            var sharedTrackHeader = LoadHeader(reader, sharedTrackDef.HeaderIndex);
            var tilesAddress = sharedTrackHeader.Address + sharedTrackHeader.TilesetOffset;
            reader.Seek(tilesAddress, SeekOrigin.Begin);
            if (sharedTrackHeader.Flags.HasFlag(TrackFlags.SplitTileset))
                Compressor.SplitDecompress(reader, tilesetStream);
            else
                Compressor.Decompress(reader, tilesetStream);
        }
        else
        {
            var tilesAddress = header.Address + header.TilesetOffset;
            reader.Seek(tilesAddress, SeekOrigin.Begin);
            if (header.Flags.HasFlag(TrackFlags.SplitTileset))
                Compressor.SplitDecompress(reader, tilesetStream);
            else
                Compressor.Decompress(reader, tilesetStream);
        }

        tilesetStream.Seek(0, SeekOrigin.Begin);
        return new Tileset(tilesetStream, tilesetSize, PixelFormat.Bpp8);
    }
    
    private static AffineTilemap LoadTilemap(Stream reader, TrackHeader header)
    {
        var trackWidth = (header.TrackWidth * 128);
        var trackHeight = (header.TrackHeight * 128);
        
        using var tilemapStream = new MemoryPoolStream(trackWidth * trackHeight, true);
        var tilemapAddress = header.Address + header.TilemapOffset;
        reader.Seek(tilemapAddress, SeekOrigin.Begin);
        if (header.Flags.HasFlag(TrackFlags.SplitTilemap))
            Compressor.SplitDecompress(reader, tilemapStream);
        else
            Compressor.Decompress(reader, tilemapStream);
        tilemapStream.Seek(0, SeekOrigin.Begin);
        return new AffineTilemap(tilemapStream, trackWidth, trackHeight);
    }
    
    private static Tileset LoadMinimap(Stream reader, TrackHeader header)
    {
        const int minimapTiles = 64;
        using var minimapStream = new MemoryPoolStream(minimapTiles * Tile4Bpp.DataSize, true);
        var minimapAddress = header.Address + header.MinimapOffset;
        reader.Seek(minimapAddress, SeekOrigin.Begin);
        Compressor.Decompress(reader, minimapStream);
        minimapStream.Seek(0, SeekOrigin.Begin);
        return new Tileset(minimapStream, minimapTiles, PixelFormat.Bpp4);
    }

    private static byte[] LoadBehaviors(Stream reader, TrackHeader header)
    {
        var behaviorsAddress = header.Address + header.BehaviorsOffset;
        reader.Seek(behaviorsAddress, SeekOrigin.Begin);
        byte[] behaviors = new byte[256];
        reader.ReadExactly(behaviors);
        return behaviors;
    }
    private static Tileset? LoadObstacleGraphics(Stream reader, TrackHeader header, int headerIndex)
    {
        using var obstacleGraphicsStream = new MemoryPoolStream(256 * Tile4Bpp.DataSize, true);
        if (header.ObstacleGfxOffset != 0)
        {
            if (header.SharedObstacleGfx != 0)
            {
                var sharedTrackDef = LoadDefinition(reader, headerIndex + header.SharedTileset);
                var sharedTrackHeader = LoadHeader(reader, sharedTrackDef.HeaderIndex);
                var obstacleGraphicsAddress = sharedTrackHeader.Address + sharedTrackHeader.ObstacleGfxOffset;
                reader.Seek(obstacleGraphicsAddress, SeekOrigin.Begin);
                if (sharedTrackHeader.Flags.HasFlag(TrackFlags.SplitObjects))
                    Compressor.SplitDecompress(reader, obstacleGraphicsStream);
                else
                    Compressor.Decompress(reader, obstacleGraphicsStream);
            }
            else
            {
                var obstacleGraphicsAddress = header.Address + header.ObstacleGfxOffset;
                reader.Seek(obstacleGraphicsAddress, SeekOrigin.Begin);
                if (header.Flags.HasFlag(TrackFlags.SplitObjects))
                    Compressor.SplitDecompress(reader, obstacleGraphicsStream);
                else
                    Compressor.Decompress(reader, obstacleGraphicsStream);
            }

            obstacleGraphicsStream.SetLength(256 * Tile4Bpp.DataSize);
            obstacleGraphicsStream.Seek(0, SeekOrigin.Begin);
            return new Tileset(obstacleGraphicsStream, 256, PixelFormat.Bpp4);
        }

        return null;
    }
    
    private static TrackObjects LoadObjects(Stream reader, TrackHeader header, int headerIndex)
    {
        var obstacles = ObstacleTable.ReadTable(reader, headerIndex);
        var trackObjects = new TrackObjects();

        if (header.ObstaclesOffset != 0)
        {
            var obstaclesAddress = header.Address + header.ObstaclesOffset;
            reader.Seek(obstaclesAddress, SeekOrigin.Begin);
            while (reader.PeekByte() != 0)
            {
                var placement = reader.Read<ObjectPlacement>();
                var trackObstacle = new ObstaclePlacement(obstacles[placement.ID], new Vec2I(placement.X, placement.Y));
                trackObjects.ObstaclePlacements.Add(trackObstacle);
            }
        }

        if (header.ItemBoxOffset != 0)
        {
            var itemBoxAddress = header.Address + header.ItemBoxOffset;
            reader.Seek(itemBoxAddress, SeekOrigin.Begin);
            while (reader.PeekByte() != 0)
            {
                var placement = reader.Read<ObjectPlacement>();
                trackObjects.ItemBoxes.Add(new Vec2I(placement.X, placement.Y));
            }
        }

        var startPositionAddress = header.Address + header.StartPositionOffset;
        reader.Seek(startPositionAddress, SeekOrigin.Begin);
        while (reader.PeekByte() != 0)
        {
            var placement = reader.Read<ObjectPlacement>();
            var startPos = new StartPosition(new Vec2I(placement.X, placement.Y), (StartingPlace)(placement.ID & ~0x80));
            trackObjects.StartPositions.Add(startPos);
        }

        return trackObjects;
    }
    
    private static TrackAi LoadAi(Stream reader, TrackHeader header, TrackDefinition definition)
    {
        var trackAi = new TrackAi();
        var aiAddress = header.Address + header.AiOffset;
        reader.Seek(aiAddress, SeekOrigin.Begin);
        var aiHeader = reader.Read<AiHeader>();
        var zonesAddress = aiAddress + aiHeader.ZonesOffset;
        var targetsAddress = aiAddress + aiHeader.TargetsOffset;
        
        reader.Seek(zonesAddress, SeekOrigin.Begin);
        for (int i = 0; i < aiHeader.ZoneCount; i++)
        {
            trackAi.Zones.Add(reader.Read<AiZone>());
        }

        reader.Seek(definition.TargetOptions);
        var targetOptions = reader.Read<TargetOptions>();
        
        reader.Seek(targetsAddress, SeekOrigin.Begin);
        for (int set = 0; set < targetOptions.SetCount; set++)
        {
            var currentSet = new AiTarget[aiHeader.ZoneCount];
            for (int i = 0; i < aiHeader.ZoneCount; i++)
            {
                currentSet[i] = reader.Read<AiTarget>();
            }
            trackAi.TargetSets.Add(currentSet);
        }

        //reader.Seek(definition.Turns);
        //while (reader.PeekByte() != 0xff)
            //trackAi.Add(reader.Read<TurnMarker>());
        
        return trackAi;
    }

    #endregion
    
    /// <summary>
    /// Write track to the ROM
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> object over the ROM</param>
    /// <param name="headerIndex">header index of the track</param>
    public void WriteTrack(Stream stream, int headerIndex)
    {
        var trackStream = new MemoryStream();

        var trackDefinition = new TrackDefinition();
        var trackHeader = new TrackHeader();
        trackStream.Skip(0x100);
        
        WriteConfig(Config, ref trackDefinition, ref trackHeader);
        WriteTileset(trackStream, ref trackHeader);
        WriteTilemap(trackStream, ref trackHeader);
        WriteMinimap(trackStream, ref trackHeader);
        WriteObstacleGfx(trackStream, ref trackHeader);
        var obstacleTable = WriteObstacleTable(stream, headerIndex, Objects);
        WriteObjects(trackStream, ref trackHeader, obstacleTable, Objects);
        WriteAi(trackStream, ref trackHeader);

        var end = trackStream.Position;
        trackStream.Seek(0, SeekOrigin.Begin);
        trackStream.Write(trackHeader);
        WriteDefinition(trackStream, trackDefinition, headerIndex);
    }

    #region Write Track Data
    private void WriteConfig(TrackConfig config, ref TrackDefinition definition, ref TrackHeader header)
    {
        definition.BackgroundIndex = config.BackgroundIndex;
        definition.BackgroundBehavior = config.BackgroundBehavior;
        definition.PaletteBehavior = config.PaletteBehavior;
        definition.Theme = config.Theme;
        definition.SongID = config.SongID;
        definition.LapsCount = config.Laps;

        header.TrackWidth = (byte)config.Size.X;
        header.TrackHeight = (byte)config.Size.Y;
    }

    private void WriteDefinition(Stream trackStream, TrackDefinition definition, int headerIndex)
    {
        const uint definitionPointerTableAddress = 0x0E7FEC;
        trackStream.Seek(definitionPointerTableAddress + headerIndex * 4, SeekOrigin.Begin);
        var definitionAddress = trackStream.Read<Pointer>();
        if (definitionAddress.IsNull) throw new ArgumentOutOfRangeException(nameof(headerIndex), "Invalid header index");
        trackStream.Seek(definitionAddress);
        trackStream.Write(definition);
    }

    private void WriteTileset(Stream trackStream, ref TrackHeader header)
    {
        header.Flags |= TrackFlags.SplitTileset;
        header.TilesetOffset = (uint)trackStream.Position;
        Compressor.SplitCompress(Tileset.GetData(), trackStream);
    }
    private void WriteTilemap(Stream trackStream, ref TrackHeader header)
    {
        header.Flags |= TrackFlags.SplitTilemap;
        header.TilemapOffset = (uint)trackStream.Position;
        Compressor.SplitCompress(Tilemap.GetData(), trackStream);
    }
    private void WriteMinimap(Stream trackStream, ref TrackHeader header)
    {
        header.MinimapOffset = (uint)trackStream.Position;
        Compressor.Compress(Minimap.GetData(), trackStream);
    }
    private void WriteObstacleGfx(Stream trackStream, ref TrackHeader header)
    {
        if (ObstacleGfx is null)
        {
            header.ObstacleGfxOffset = 0;
        }
        else
        {
            header.Flags |= TrackFlags.SplitObjects;
            header.ObstacleGfxOffset = (uint)trackStream.Position;
            Compressor.SplitCompress(ObstacleGfx.GetData(), trackStream);
        }
    }
    private ObstacleTable WriteObstacleTable(Stream romStream, int headerIndex, TrackObjects trackObjects)
    {
        var obstacles = new List<Obstacle>();
        foreach (var obstaclePlacement in trackObjects.ObstaclePlacements)
        {
            if (!obstacles.Contains(obstaclePlacement.Obstacle))
                obstacles.Add(obstaclePlacement.Obstacle);
        }
        var obstacleTable = new ObstacleTable { Obstacles = obstacles };
        obstacleTable.OverrideExistingTable(romStream, headerIndex);
        return obstacleTable;
    }

    private void WriteObjects(Stream trackStream, ref TrackHeader header, ObstacleTable obstacleTable, TrackObjects trackObjects)
    {
        byte[] aiMap = Ai.GenerateZoneMap(header.TrackWidth);
        header.ObstaclesOffset = (uint)trackStream.Position;
        for (int i = 0; i < trackObjects.ObstaclePlacements.Count; i++)
        {
            var obsPlacement = trackObjects.ObstaclePlacements[i];
            var objPlacement = new ObjectPlacement
            {
                ID = (byte)obstacleTable.IndexOfObstacle(obsPlacement.Obstacle),
                X = (byte)obsPlacement.Position.X,
                Y = (byte)obsPlacement.Position.X,
                Zone = aiMap[obsPlacement.Position.X + obsPlacement.Position.Y * header.TrackWidth * 64],
            };
        }
    }

    private void WriteAi(Stream trackStream, ref TrackHeader header)
    {
        header.AiOffset = (uint)trackStream.Position;
        var aiHeader = new AiHeader { ZoneCount = (byte)Ai.Zones.Count };
        var headerAddr = trackStream.Position;
        trackStream.Seek(5, SeekOrigin.Current);
        var zonesOffs = (ushort)(trackStream.Position - headerAddr);
        foreach (var zone in Ai.Zones)
            trackStream.Write(zone);

        aiHeader.TargetsOffset = (ushort)(trackStream.Position - headerAddr);
        foreach (var set in Ai.TargetSets)
        foreach (var target in set)
            trackStream.Write(target);

        var endPos = trackStream.Position;
        trackStream.Seek(headerAddr, SeekOrigin.Begin);
        trackStream.Write(aiHeader);
        trackStream.Seek(endPos, SeekOrigin.Begin);
    }

    #endregion
}