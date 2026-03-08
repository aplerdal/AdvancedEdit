using AdvancedLib.Graphics;
using AdvancedLib.Serialization;
using AdvancedLib.Serialization.AI;
using AdvancedLib.Serialization.Allocator;
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
    public required Palette? ObstaclePalette { get; set; }
    public required TrackObjects Objects { get; set; }
    public required List<Vec2I> Coins { get; set; }
    public required TrackAi Ai { get; set; }
    public required Tileset? CoverArt { get; set; }
    public required Palette? CoverPalette { get; set; }
    public required Palette? LockedCoverPalette { get; set; }
    public required byte[] Behaviors { get; set; }
    public required TargetTime[] TargetTimes { get; set; }
    public required TurnSign[]? TurnSigns { get; set; }
    public required RivalTargets RivalTargets { get; set; }

    /// <summary>
    /// Initialize empty <see cref="Track"/> object
    /// </summary>
    public Track()
    {
    }

    /// <summary>
    /// Load <see cref="Track"/> from stream
    /// </summary>
    /// <param name="stream">A stream containing the game ROM</param>
    /// <param name="headerIndex">The index of the header for the track being loaded</param>
    public static Track FromRom(Stream stream, int headerIndex)
    {
        var definition = LoadDefinition(stream, headerIndex);
        var header = LoadHeader(stream, definition.HeaderIndex);
        return new Track
        {
            Config = LoadConfig(header, definition),
            TilesetPalette = LoadTilesetPalette(stream, header),
            Tileset = LoadTileset(stream, header, headerIndex),
            Tilemap = LoadTilemap(stream, header),
            Minimap = LoadMinimap(stream, header),
            Behaviors = LoadBehaviors(stream, header),
            ObstacleGfx = LoadObstacleGraphics(stream, header, headerIndex),
            ObstaclePalette = LoadObstaclePalette(stream, header),
            Objects = LoadObjects(stream, header, headerIndex),
            Coins = LoadCoins(stream, header),
            Ai = LoadAi(stream, header, definition),
            CoverArt = LoadCoverArt(stream, definition),
            CoverPalette = LoadCoverPalette(stream, definition),
            LockedCoverPalette = LoadLockedCoverPalette(stream, definition),
            TargetTimes = LoadTargetTimes(stream, definition),
            TurnSigns = LoadTurnSigns(stream, definition),
            RivalTargets = LoadRivalTargets(stream, definition),
        };
    }

    /// <summary>
    /// Default track object
    /// </summary>
    public static Track Default => new()
    {
        Config = TrackConfig.Default,
        Tileset = new Tileset(256, PixelFormat.Bpp8),
        TilesetPalette = new Palette(64),
        Tilemap = new AffineTilemap(TrackConfig.Default.Size.X * 128, TrackConfig.Default.Size.Y * 128),
        Minimap = new Tileset(64, PixelFormat.Bpp4),
        ObstacleGfx = new Tileset(256, PixelFormat.Bpp4),
        ObstaclePalette = new Palette(48),
        Objects = new TrackObjects(),
        Behaviors = new byte[256],
        Coins = new List<Vec2I>(),
        Ai = new TrackAi(),
        CoverArt = null,
        CoverPalette = null,
        LockedCoverPalette = null,
        RivalTargets = new(),
        TargetTimes = TargetTime.Defaults,
        TurnSigns = null,
    };

    #region Load Track Data

    private static TrackDefinition LoadDefinition(Stream stream, int index)
    {
        const uint definitionPointerTableAddress = 0x0E7FEC;
        stream.Seek(definitionPointerTableAddress + index * 4, SeekOrigin.Begin);
        var definitionAddress = stream.Read<Pointer>();
        if (definitionAddress.IsNull) throw new ArgumentOutOfRangeException(nameof(index), "Invalid header index");
        stream.Seek(definitionAddress);
        return stream.Read<TrackDefinition>();
    }

    private static TrackHeader LoadHeader(Stream stream, int index)
    {
        const uint trackTableAddress = 0x258000;
        stream.Seek(trackTableAddress + index * 4, SeekOrigin.Begin);
        var offset = stream.ReadUInt32();
        stream.Seek(trackTableAddress + offset, SeekOrigin.Begin);
        return stream.Read<TrackHeader>();
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
            TurnsPointer = definition.TurnSigns.Raw,
            TargetOptionsPointer = definition.RivalTargets.Raw,
            CoverGfxPointer = definition.CoverGfx.Raw,
            CoverPalPointer = definition.CoverPalette.Raw,
            LockedTrackPalPointer = definition.LockedCoverPal.Raw,
            TrackNameGfxPointer = definition.TrackNameGfx.Raw,
            TargetTimesPtr = definition.TargetTimes.Raw
        };
    }

    private static Palette LoadTilesetPalette(Stream stream, TrackHeader header)
    {
        var palAddress = header.Address + header.TilesetPaletteOffset;
        stream.Seek(palAddress, SeekOrigin.Begin);
        return new Palette(stream, 64);
    }

    private static Tileset LoadTileset(Stream stream, TrackHeader header, int headerIndex)
    {
        const int tilesetSize = 256;

        using var tilesetStream = new MemoryPoolStream(tilesetSize * Tile.Size * Tile.Size, true);
        if (header.SharedTileset != 0)
        {
            var sharedTrackDef = LoadDefinition(stream, headerIndex + header.SharedTileset);
            var sharedTrackHeader = LoadHeader(stream, sharedTrackDef.HeaderIndex);
            var tilesAddress = sharedTrackHeader.Address + sharedTrackHeader.TilesetOffset;
            stream.Seek(tilesAddress, SeekOrigin.Begin);
            if (sharedTrackHeader.Flags.HasFlag(TrackFlags.SplitTileset))
                Compressor.SplitDecompress(stream, tilesetStream);
            else
                Compressor.Decompress(stream, tilesetStream);
        }
        else
        {
            var tilesAddress = header.Address + header.TilesetOffset;
            stream.Seek(tilesAddress, SeekOrigin.Begin);
            if (header.Flags.HasFlag(TrackFlags.SplitTileset))
                Compressor.SplitDecompress(stream, tilesetStream);
            else
                Compressor.Decompress(stream, tilesetStream);
        }

        tilesetStream.Seek(0, SeekOrigin.Begin);
        return new Tileset(tilesetStream, tilesetSize, PixelFormat.Bpp8);
    }

    private static AffineTilemap LoadTilemap(Stream stream, TrackHeader header)
    {
        var trackWidth = header.TrackWidth * 128;
        var trackHeight = header.TrackHeight * 128;

        using var tilemapStream = new MemoryPoolStream(trackWidth * trackHeight, true);
        var tilemapAddress = header.Address + header.TilemapOffset;
        stream.Seek(tilemapAddress, SeekOrigin.Begin);
        if (header.Flags.HasFlag(TrackFlags.SplitTilemap))
            Compressor.SplitDecompress(stream, tilemapStream);
        else
            Compressor.Decompress(stream, tilemapStream);
        tilemapStream.Seek(0, SeekOrigin.Begin);
        return new AffineTilemap(tilemapStream, trackWidth, trackHeight);
    }

    private static Tileset LoadMinimap(Stream stream, TrackHeader header)
    {
        const int minimapTiles = 64;
        using var minimapStream = new MemoryPoolStream(minimapTiles * Tile4Bpp.DataSize, true);
        var minimapAddress = header.Address + header.MinimapOffset;
        stream.Seek(minimapAddress, SeekOrigin.Begin);
        Compressor.Decompress(stream, minimapStream);
        minimapStream.Seek(0, SeekOrigin.Begin);
        return new Tileset(minimapStream, minimapTiles, PixelFormat.Bpp4);
    }

    private static byte[] LoadBehaviors(Stream stream, TrackHeader header)
    {
        var behaviorsAddress = header.Address + header.BehaviorsOffset;
        stream.Seek(behaviorsAddress, SeekOrigin.Begin);
        var behaviors = new byte[256];
        stream.ReadExactly(behaviors);
        return behaviors;
    }

    private static Palette LoadObstaclePalette(Stream reader, TrackHeader header)
    {
        var palAddress = header.Address + header.ObstaclePaletteOffset;
        reader.Seek(palAddress, SeekOrigin.Begin);
        return new Palette(reader, 48);
    }

    private static Tileset? LoadObstacleGraphics(Stream stream, TrackHeader header, int headerIndex)
    {
        using var obstacleGraphicsStream = new MemoryPoolStream(256 * Tile4Bpp.DataSize, true);
        if (header.ObstacleGfxOffset != 0)
        {
            if (header.SharedObstacleGfx != 0)
            {
                var sharedTrackDef = LoadDefinition(stream, headerIndex + header.SharedTileset);
                var sharedTrackHeader = LoadHeader(stream, sharedTrackDef.HeaderIndex);
                var obstacleGraphicsAddress = sharedTrackHeader.Address + sharedTrackHeader.ObstacleGfxOffset;
                stream.Seek(obstacleGraphicsAddress, SeekOrigin.Begin);
                if (sharedTrackHeader.Flags.HasFlag(TrackFlags.SplitObjects))
                    Compressor.SplitDecompress(stream, obstacleGraphicsStream);
                else
                    Compressor.Decompress(stream, obstacleGraphicsStream);
            }
            else
            {
                var obstacleGraphicsAddress = header.Address + header.ObstacleGfxOffset;
                stream.Seek(obstacleGraphicsAddress, SeekOrigin.Begin);
                if (header.Flags.HasFlag(TrackFlags.SplitObjects))
                    Compressor.SplitDecompress(stream, obstacleGraphicsStream);
                else
                    Compressor.Decompress(stream, obstacleGraphicsStream);
            }

            obstacleGraphicsStream.SetLength(256 * Tile4Bpp.DataSize);
            obstacleGraphicsStream.Seek(0, SeekOrigin.Begin);
            return new Tileset(obstacleGraphicsStream, 256, PixelFormat.Bpp4);
        }

        return null;
    }

    private static TrackObjects LoadObjects(Stream stream, TrackHeader header, int headerIndex)
    {
        var obstacles = ObstacleTable.ReadTable(stream, headerIndex);
        var trackObjects = new TrackObjects();

        if (header.ObstaclesOffset != 0)
        {
            var obstaclesAddress = header.Address + header.ObstaclesOffset;
            stream.Seek(obstaclesAddress, SeekOrigin.Begin);
            while (stream.PeekByte() != 0)
            {
                var placement = stream.Read<ObjectPlacement>();
                var trackObstacle = new ObstaclePlacement(obstacles[placement.ID], new Vec2I(placement.X, placement.Y));
                trackObjects.ObstaclePlacements.Add(trackObstacle);
            }
        }

        if (header.ItemBoxOffset != 0)
        {
            var itemBoxAddress = header.Address + header.ItemBoxOffset;
            stream.Seek(itemBoxAddress, SeekOrigin.Begin);
            while (stream.PeekByte() != 0)
            {
                var placement = stream.Read<ObjectPlacement>();
                trackObjects.ItemBoxes.Add(new Vec2I(placement.X, placement.Y));
            }
        }

        var startPositionAddress = header.Address + header.StartPositionOffset;
        stream.Seek(startPositionAddress, SeekOrigin.Begin);
        while (stream.PeekByte() != 0)
        {
            var placement = stream.Read<ObjectPlacement>();
            var startPos = new StartPosition(new Vec2I(placement.X, placement.Y), (StartingPlace)(placement.ID & ~0x80));
            trackObjects.StartPositions.Add(startPos);
        }

        return trackObjects;
    }

    private static List<Vec2I> LoadCoins(Stream stream, TrackHeader header)
    {
        var coins = new List<Vec2I>();
        if (header.CoinsOffset != 0)
        {
            var coinsAddress = header.Address + header.CoinsOffset;
            stream.Seek(coinsAddress, SeekOrigin.Begin);
            while (stream.PeekByte() != 0)
            {
                var coin = stream.Read<ObjectPlacement>();
                coins.Add(new Vec2I(coin.X, coin.Y));
            }
        }

        return coins;
    }

    private static TrackAi LoadAi(Stream stream, TrackHeader header, TrackDefinition definition)
    {
        var trackAi = new TrackAi();
        var aiAddress = header.Address + header.AiOffset;
        stream.Seek(aiAddress, SeekOrigin.Begin);
        var aiHeader = stream.Read<AiHeader>();
        var zonesAddress = aiAddress + aiHeader.CheckpointsOffset;
        var targetsAddress = aiAddress + aiHeader.TargetsOffset;

        stream.Seek(zonesAddress, SeekOrigin.Begin);
        for (var i = 0; i < aiHeader.CheckpointCount; i++) trackAi.Checkpoints.Add(stream.Read<Checkpoint>());

        stream.Seek(definition.RivalTargets);
        var targetOptions = stream.Read<TargetOptions>();

        stream.Seek(targetsAddress, SeekOrigin.Begin);
        for (var set = 0; set < targetOptions.SetCount; set++)
        {
            var currentSet = new List<AiTarget>(aiHeader.CheckpointCount);
            for (var i = 0; i < aiHeader.CheckpointCount; i++) currentSet.Add(stream.Read<AiTarget>());

            trackAi.TargetSets.Add(currentSet);
        }

        return trackAi;
    }

    private static Tileset? LoadCoverArt(Stream stream, TrackDefinition definition)
    {
        if (definition.CoverGfx.IsNull) return null;

        const int tilesetSize = 81;
        using var tilesetStream = new MemoryPoolStream(tilesetSize * Tile.Size * Tile.Size, true);

        stream.Seek(definition.CoverGfx);
        Compressor.Decompress(stream, tilesetStream);

        tilesetStream.Seek(0, SeekOrigin.Begin);
        return new Tileset(tilesetStream, tilesetSize, PixelFormat.Bpp8);
    }

    private static Palette? LoadCoverPalette(Stream stream, TrackDefinition definition)
    {
        if (definition.CoverPalette.IsNull) return null;

        using var palStream = new MemoryPoolStream(80 * 2, true);

        stream.Seek(definition.CoverPalette);
        Compressor.Decompress(stream, palStream);

        palStream.Seek(0, SeekOrigin.Begin);
        return new Palette(palStream, 80);
    }

    private static Palette? LoadLockedCoverPalette(Stream stream, TrackDefinition definition)
    {
        if (definition.LockedCoverPal.IsNull) return null;

        using var palStream = new MemoryPoolStream(80 * 2, true);

        stream.Seek(definition.LockedCoverPal);
        Compressor.Decompress(stream, palStream);

        palStream.Seek(0, SeekOrigin.Begin);
        return new Palette(palStream, 80);
    }

    private static TargetTime[] LoadTargetTimes(Stream stream, TrackDefinition definition)
    {
        if (definition.TargetTimes.IsNull)
            return TargetTime.Defaults;
        stream.Seek(definition.TargetTimes);
        var times = new TargetTime[6];
        for (var i = 0; i < times.Length; i++)
            times[i] = stream.Read<TargetTime>();
        return times;
    }

    private static TurnSign[]? LoadTurnSigns(Stream stream, TrackDefinition definition)
    {
        var signs = new List<TurnSign>();
        if (definition.TurnSigns.IsNull)
            return null;
        stream.Seek(definition.TurnSigns);
        while (stream.PeekByte() != 0xff)
            signs.Add(stream.Read<TurnSign>());
        return signs.ToArray();
    }

    private static RivalTargets LoadRivalTargets(Stream stream, TrackDefinition definition)
    {
        stream.Seek(definition.RivalTargets);
        return stream.Read<RivalTargets>();
    }

    #endregion

    /// <summary>
    /// Write track to the ROM
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> object over the ROM</param>
    /// <param name="headerIndex">header index of the track</param>
    public void WriteTrack(Stream stream, int headerIndex)
    {
        var trackAddress = stream.Position;
        var trackStream = new MemoryStream();

        var trackDefinition = new TrackDefinition { HeaderIndex = headerIndex };
        var trackHeader = new TrackHeader();
        trackStream.Skip(0x100);

        WriteConfig(Config, ref trackDefinition, ref trackHeader);
        WriteTileset(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WritePalette(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteTilemap(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteMinimap(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteBehaviors(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteObstacleGfx(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteObstaclePal(trackStream, ref trackHeader);
        AlignStream(trackStream);
        var obstacleTable = WriteObstacleTable(stream, headerIndex, Objects);
        AlignStream(trackStream);
        WriteObjects(trackStream, ref trackHeader, obstacleTable, Objects);
        AlignStream(trackStream);
        WriteCoins(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteAi(trackStream, ref trackHeader);
        AlignStream(trackStream);
        WriteCoverArt(trackAddress, trackStream, ref trackDefinition);
        AlignStream(trackStream);
        WriteCoverPalette(trackAddress, trackStream, ref trackDefinition);
        AlignStream(trackStream);
        WriteLockedCoverPalette(trackAddress, trackStream, ref trackDefinition);
        AlignStream(trackStream);
        WriteTargetTimes(trackAddress, trackStream, ref trackDefinition);
        AlignStream(trackStream);
        WriteTurnSigns(trackAddress, trackStream, ref trackDefinition);
        AlignStream(stream);
        WriteRivalTargets(trackAddress, trackStream, ref trackDefinition);
        AlignStream(stream);

        trackStream.Seek(0, SeekOrigin.Begin);
        trackStream.Write(trackHeader);

        WriteDefinition(stream, trackDefinition, headerIndex);
        stream.Seek(trackAddress, SeekOrigin.Begin);
        stream.Write(trackStream.GetBuffer());
    }

    private static void AlignStream(Stream stream)
    {
        stream.Position = (stream.Position + 3) & ~3;
    }

    #region Write Track Data

    private static void WriteConfig(TrackConfig config, ref TrackDefinition definition, ref TrackHeader header)
    {
        definition.BackgroundIndex = config.BackgroundIndex;
        definition.BackgroundBehavior = config.BackgroundBehavior;
        definition.PaletteBehavior = config.PaletteBehavior;
        definition.Theme = config.Theme;
        definition.SongID = config.SongID;
        definition.LapsCount = config.Laps;

        definition.TurnSigns = new Pointer(config.TurnsPointer);
        definition.RivalTargets = new Pointer(config.TargetOptionsPointer);
        definition.CoverGfx = new Pointer(config.CoverGfxPointer);
        definition.CoverPalette = new Pointer(config.CoverPalPointer);
        definition.LockedCoverPal = new Pointer(config.LockedTrackPalPointer);
        definition.TrackNameGfx = new Pointer(config.TrackNameGfxPointer);
        definition.TargetTimes = new Pointer(config.TargetTimesPtr);

        header.TrackWidth = (byte)config.Size.X;
        header.TrackHeight = (byte)config.Size.Y;
    }

    private static void WriteDefinition(Stream romStream, TrackDefinition definition, int headerIndex)
    {
        const uint definitionPointerTableAddress = 0x0E7FEC;
        romStream.Seek(definitionPointerTableAddress + headerIndex * 4, SeekOrigin.Begin);
        var definitionAddress = romStream.Read<Pointer>();
        if (definitionAddress.IsNull) // Create new table if one doesn't exist
        {
            romStream.Seek(definitionPointerTableAddress + headerIndex * 4, SeekOrigin.Begin);
            var newDefAddress = RomAllocator.Allocate(14 * 4); // Definition size
            romStream.Write(newDefAddress.Raw);
            definitionAddress = newDefAddress;
        }

        romStream.Seek(definitionAddress);
        romStream.Write(definition);
    }

    private void WriteTileset(Stream trackStream, ref TrackHeader header)
    {
        header.Flags |= TrackFlags.SplitTileset;
        header.CompressedTileset = true;
        header.TilesetOffset = (uint)trackStream.Position;
        Compressor.SplitCompress(Tileset.GetData(), trackStream);
    }

    private void WritePalette(Stream trackStream, ref TrackHeader header)
    {
        header.TilesetPaletteOffset = (uint)trackStream.Position;
        TilesetPalette.Write(trackStream);
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

    private void WriteBehaviors(Stream trackStream, ref TrackHeader header)
    {
        header.BehaviorsOffset = (uint)trackStream.Position;
        trackStream.Write(Behaviors);
    }

    private void WriteObstaclePal(Stream trackStream, ref TrackHeader header)
    {
        if (ObstaclePalette is null)
        {
            header.ObstaclePaletteOffset = 0;
        }
        else
        {
            header.ObstaclePaletteOffset = (uint)trackStream.Position;
            ObstaclePalette.Write(trackStream);
        }
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

    private static ObstacleTable WriteObstacleTable(Stream romStream, int headerIndex, TrackObjects trackObjects)
    {
        var obstacles = new List<Obstacle>();
        foreach (var obstaclePlacement in trackObjects.ObstaclePlacements)
            if (!obstacles.Contains(obstaclePlacement.Obstacle))
                obstacles.Add(obstaclePlacement.Obstacle);

        var obstacleTable = new ObstacleTable { Obstacles = obstacles };
        obstacleTable.OverrideExistingTable(romStream, headerIndex);
        return obstacleTable;
    }

    private void WriteObjects(Stream trackStream, ref TrackHeader header, ObstacleTable obstacleTable, TrackObjects trackObjects)
    {
        var aiMap = Ai.GenerateCheckpointMap(header.TrackWidth);
        header.ObstaclesOffset = (uint)trackStream.Position;
        foreach (var obsPlacement in trackObjects.ObstaclePlacements)
        {
            var objPlacement = new ObjectPlacement
            {
                ID = (byte)obstacleTable.IndexOfObstacle(obsPlacement.Obstacle),
                X = (byte)obsPlacement.Position.X,
                Y = (byte)obsPlacement.Position.Y,
                Checkpoint = aiMap[obsPlacement.Position.X / 2 + obsPlacement.Position.Y / 2 * header.TrackWidth * 64]
            };
            objPlacement.Serialize(trackStream);
        }

        trackStream.Write((uint)0);

        header.ItemBoxOffset = (uint)trackStream.Position;
        foreach (var boxPosition in trackObjects.ItemBoxes)
        {
            var objPlacement = new ObjectPlacement
            {
                ID = 1,
                X = (byte)boxPosition.X,
                Y = (byte)boxPosition.Y,
                Checkpoint = aiMap[boxPosition.X / 2 + boxPosition.Y / 2 * header.TrackWidth * 64]
            };
            objPlacement.Serialize(trackStream);
        }

        trackStream.Write((uint)0);

        header.StartPositionOffset = (uint)trackStream.Position;
        foreach (var startPosition in trackObjects.StartPositions)
        {
            var objPlacement = new ObjectPlacement
            {
                ID = (byte)((int)startPosition.Place | 0x80),
                X = (byte)startPosition.Position.X,
                Y = (byte)startPosition.Position.Y,
                Checkpoint = aiMap[startPosition.Position.X / 2 + startPosition.Position.X / 2 * header.TrackWidth * 64]
            };
            objPlacement.Serialize(trackStream);
        }

        trackStream.Write((uint)0);
    }

    private void WriteCoins(Stream trackStream, ref TrackHeader header)
    {
        header.CoinsOffset = (uint)trackStream.Position;
        foreach (var coinPos in Coins)
        {
            var coin = new ObjectPlacement
            {
                ID = 0xff,
                X = (byte)coinPos.X,
                Y = (byte)coinPos.Y,
                Checkpoint = 0
            };
            coin.Serialize(trackStream);
        }
    }

    private void WriteAi(Stream trackStream, ref TrackHeader header)
    {
        header.AiOffset = (uint)trackStream.Position;
        var aiHeader = new AiHeader { CheckpointCount = (byte)Ai.Checkpoints.Count };
        var headerAddr = trackStream.Position;
        trackStream.Seek(5, SeekOrigin.Current);
        aiHeader.CheckpointsOffset = (ushort)(trackStream.Position - headerAddr);
        foreach (var zone in Ai.Checkpoints)
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

    private void WriteCoverArt(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        if (CoverArt is null)
        {
            definition.CoverGfx = Pointer.Null;
            return;
        }

        var ptr = new Pointer((uint)(trackAddress + trackStream.Position));
        Compressor.Compress(CoverArt.GetData(), trackStream);
        definition.CoverGfx = ptr;
    }

    private void WriteCoverPalette(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        if (CoverPalette is null)
        {
            definition.CoverPalette = Pointer.Null;
            return;
        }

        definition.CoverPalette = new Pointer((uint)(trackAddress + trackStream.Position));
        Compressor.Compress(CoverPalette.GetData(), trackStream);
    }

    private void WriteLockedCoverPalette(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        if (LockedCoverPalette is null)
        {
            definition.LockedCoverPal = Pointer.Null;
            return;
        }

        definition.CoverPalette = new Pointer((uint)(trackAddress + trackStream.Position));
        Compressor.Compress(LockedCoverPalette.GetData(), trackStream);
    }

    private void WriteTargetTimes(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        definition.TargetTimes = new Pointer((uint)(trackAddress + trackStream.Position));

        foreach (var time in TargetTimes)
            trackStream.Write(time);
    }

    private void WriteTurnSigns(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        definition.TurnSigns = new Pointer((uint)(trackAddress + trackStream.Position));
        if (TurnSigns is null)
        {
            trackStream.Write(0xff);
            return;
        }

        foreach (var sign in TurnSigns)
            trackStream.Write(sign);

        trackStream.Write(0xff);
    }

    private void WriteRivalTargets(long trackAddress, Stream trackStream, ref TrackDefinition definition)
    {
        definition.RivalTargets = new Pointer((uint)(trackAddress + trackStream.Position));
        trackStream.Write(RivalTargets);
    }

    #endregion
}