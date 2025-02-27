using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AdvancedEdit.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Vector2 = System.Numerics.Vector2;

namespace AdvancedEdit.Serialization;

[Flags]
public enum TrackFlags
{
    SplitTileset = 1,
    SplitLayout = 2,
    SplitActorGfx = 4,
}
public class Track
{
    public int Id;
    
    // From track header
    public Tilemap Tilemap;
    private byte[] _minimap;
    
    private Gfx8? _tileset;
    public Gfx8 Tileset
    {
        get
        {
            if (_tileset is not null) return _tileset;
            if (ReusedTileset == 0) throw new Exception("Tileset null, not reused.");
            return TrackManager.Tracks![Math.Clamp(Id - (256 - ReusedTileset), 0, Id-1)].Tileset; //TODO: Proper reused tilset offsets
        }
        set {
            _tileset = value;
            ReusedTileset = 0;
        }
    }
    public List<AiSector> AiSectors = new List<AiSector>();

    private Gfx4? _actorGfx;
    public Gfx4? ActorGfx
    {
        get
        {
            if (_actorGfx is not null) return _actorGfx;
            if (ReusedActorGfx == 0) return null; // No actor gfx in scene
            return TrackManager.Tracks![Math.Clamp(Id - (256 - ReusedTileset), 0, Id - 1)].ActorGfx;
        }
        set => _actorGfx = value;
    }
    
    /// <summary>
    /// Track size in tiles
    /// </summary>
    public Point Size;
    public TrackFlags Flags;
    
    public bool IsTilesetCompressed;
    public uint ReusedTileset;
    public uint ReusedActorGfx;
    public byte[] Behaviors;
    public List<GameObject> Actors = new List<GameObject>();
    public List<GameObject> Positions = new List<GameObject>();
    public List<GameObject> ItemBoxes = new List<GameObject>();
    public List<Point> Overlay = new List<Point>();

    // From track definition
    // public Gfx4 Name;
    // public Gfx4 Cover;
    
    public uint Laps; // Artificially clamp from 1-5 (beyond that it likely breaks). Need to Auto apply patch to all ROMs.

    // Store data we do not keep track of that cannot be determined.
    private byte _magic; // Might be okay to change to const?
    private uint _trackRoutine;

    private uint _trackId;
    private uint _backgroundId; // Might want to allow changing this
    private uint _backgroundBehavior;
    private uint _animation;
    private uint _material;
    private uint _turnInstructions;
    private uint _musicId; // and this as well
    private uint _targetSetTable;
    private uint _tdUnknown; // The "unk" value from the track definition
    private uint _trackArtGfx;
    private uint _trackArtPalette;
    private uint _trackLockedPalette;
    private uint _trackNameGfx;

    /// <summary>
    /// Reads a track object from a file
    /// </summary>
    /// <param name="reader">a BinaryReader</param>
    /// <param name="id">The number of the track</param>
    /// <param name="definition">The track definition address</param>
    /// <param name="headerAddress">The track header address</param>
    public Track(BinaryReader reader, int id, uint definition, uint headerAddress)
    {
        Id = id;
        #region Definition
        reader.BaseStream.Seek(definition, SeekOrigin.Begin);
        _trackId = reader.ReadUInt32();
        _backgroundId = reader.ReadUInt32();
        _backgroundBehavior = reader.ReadUInt32();
        _animation = reader.ReadUInt32();
        _material = reader.ReadUInt32();
        _turnInstructions = reader.ReadUInt32();
        _musicId = reader.ReadUInt32();
        _targetSetTable = reader.ReadUInt32();
        _tdUnknown = reader.ReadUInt32();
        _trackArtGfx = reader.ReadUInt32();
        _trackArtPalette = reader.ReadUInt32();
        _trackLockedPalette = reader.ReadUInt32();
        _trackNameGfx = reader.ReadUInt32();
        
        Laps = reader.ReadUInt32();

        #endregion
        
        #region Header
        reader.BaseStream.Seek(headerAddress, SeekOrigin.Begin);
        _magic = reader.ReadByte();
        IsTilesetCompressed = reader.ReadBoolean();
        reader.BaseStream.Seek(1, SeekOrigin.Current);
        Flags = (TrackFlags)reader.ReadByte();
        Size = new Point(reader.ReadByte()*128, reader.ReadByte()*128);
        reader.BaseStream.Seek(42, SeekOrigin.Current);
        ReusedTileset = reader.ReadUInt32();
        reader.BaseStream.Seek(12, SeekOrigin.Current);
        var layoutAddress = headerAddress+reader.ReadUInt32();
        reader.BaseStream.Seek(60, SeekOrigin.Current);
        var tilesetAddress = headerAddress + reader.ReadUInt32();
        var paletteAddress = headerAddress + reader.ReadUInt32();
        var behaviorAddress = headerAddress + reader.ReadUInt32();
        var actorsAddress = headerAddress + reader.ReadUInt32();
        var overlayAddress = headerAddress + reader.ReadUInt32();
        var itemBoxAddress = headerAddress + reader.ReadUInt32();
        var positionsAddress = headerAddress + reader.ReadUInt32();
        var unk1Address = headerAddress + reader.ReadUInt32();
        reader.BaseStream.Seek(32, SeekOrigin.Current);
        _trackRoutine = reader.ReadUInt32();
        var minimapAddress = headerAddress + reader.ReadUInt32();
        reader.ReadUInt32(); // unk battle stuff
        var aiAddress = headerAddress + reader.ReadUInt32();
        reader.BaseStream.Seek(20, SeekOrigin.Current);
        var actorGfxAddress = headerAddress + reader.ReadUInt32();
        var actorPaletteAddress = headerAddress + reader.ReadUInt32();
        ReusedActorGfx = reader.ReadUInt32();
        #endregion
        
        #region Load Tileset
        
        Color[] tilePalette = new Color[64];
        reader.BaseStream.Seek(paletteAddress, SeekOrigin.Begin);
        for (int i = 0; i < 64; i++)
            tilePalette[i] = new BgrColor(reader.ReadUInt16()).ToColor();
        
        if (ReusedTileset != 0)
        {
            _tileset = null;
        } else if (Flags.HasFlag(TrackFlags.SplitTileset))
        {
            long pos = tilesetAddress;
            byte[] indices = new byte[4096*4];
            for (int i = 0; i < 4; i++)
            {
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                var offset = reader.ReadUInt16();
                pos += 2;
                if (offset != 0)
                {
                    var partAddress = tilesetAddress + offset;
                    reader.BaseStream.Seek(partAddress, SeekOrigin.Begin);
                    byte[] data = Lz10.Decompress(reader).ToArray();
                    Array.Copy(data, 0, indices, i*4096, 4096);
                }
            }
            Tileset = new Gfx8(indices, tilePalette);
        }
        else
        {
            reader.BaseStream.Seek(tilesetAddress, SeekOrigin.Begin);
            byte[] data = Lz10.Decompress(reader).ToArray();
            Tileset = new Gfx8(data, tilePalette);
        }
        #endregion
        
        #region Load Layout
        if (Flags.HasFlag(TrackFlags.SplitLayout))
        {
            uint pos = layoutAddress;
            byte[] indices = new byte[4096*16];
            for (int i = 0; i < 16; i++)
            {
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                var offset = reader.ReadUInt16();
                pos += 2;
                if (offset != 0)
                {
                    var partAddress = layoutAddress + offset;
                    
                    reader.BaseStream.Seek(partAddress, SeekOrigin.Begin);
                    var data = Lz10.Decompress(reader).ToArray();
                    Array.Copy(data, 0, indices, i*4096, 4096);
                }
            }
            
            Tilemap = new Tilemap(Size, Tileset.Texture, indices);
        }
        else
        {
            reader.BaseStream.Seek(layoutAddress, SeekOrigin.Begin);
            var data = Lz10.Decompress(reader).ToArray();
            Tilemap = new Tilemap(Size, Tileset.Texture, data);
        }
        #endregion

        #region Load AI

        reader.BaseStream.Seek(aiAddress, SeekOrigin.Begin);
        byte sectorCount = reader.ReadByte();
        uint zonesAddress = aiAddress + reader.ReadUInt16();
        uint targetsAddress = aiAddress + reader.ReadUInt16();
        const int targetSize = 2 + 2 + 1 + 3;
        const int zoneSize = 1 + 2 + 2 + 2 + 2 + 3;
        uint targetSetSize = (uint)(sectorCount*targetSize);

        for (int i = 0; i < sectorCount; i++)
        {
            Point[] targets = new Point[3];
            int[] speeds = new int[3];
            bool[] intersections = new bool[3];
            for (int j = 0; j<3; j++) {
                reader.BaseStream.Seek(targetsAddress + i*targetSize + targetSetSize*j, SeekOrigin.Begin);
                targets[j] = new Point(reader.ReadUInt16(), reader.ReadUInt16());
                byte speedFlagUnion = reader.ReadByte();
                speeds[j] = (speedFlagUnion & 0x3);
                intersections[j] = (speedFlagUnion & 0x80) == 0x80;
            }
            reader.BaseStream.Seek(zonesAddress + i * zoneSize, SeekOrigin.Begin);
            var shape = (ZoneShape)reader.ReadByte();
            var rect = new Rectangle(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

            AiSectors.Add(new AiSector(targets, shape, rect, speeds, intersections));
        }
        #endregion

        #region Load Actor GFX
        
        if (ReusedActorGfx != 0)
        {
            ActorGfx = null;
        }
        else
        {
            if (actorGfxAddress != headerAddress)
            {
                Color[,] actorPalettes = new Color[16, 4];

                reader.BaseStream.Seek(actorPaletteAddress, SeekOrigin.Begin);
                for (int pal = 0; pal < 4; pal++)
                for (int i = 0; i < 16; i++)
                    actorPalettes[i, pal] = new BgrColor(reader.ReadUInt16()).ToColor();
                
                if (!Flags.HasFlag(TrackFlags.SplitActorGfx))
                {
                    reader.BaseStream.Seek(actorGfxAddress, SeekOrigin.Begin);
                    var data = Lz10.Decompress(reader).ToArray();
                    ActorGfx = new Gfx4(data, actorPalettes);
                }
                else
                {
                    long pos = actorGfxAddress;
                    byte[] data = new byte[4096 * 2];
                    for (int i = 0; i < 2; i++)
                    {
                        reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                        var offset = reader.ReadUInt16();
                        pos += 2;
                        if (offset != 0)
                        {
                            var partAddress = actorGfxAddress + offset;
                            reader.BaseStream.Seek(partAddress, SeekOrigin.Begin);
                            var temp = Lz10.Decompress(reader).ToArray();
                            Array.Copy(temp, 0, data, 4096*i, temp.Length);
                        }
                    }

                    ActorGfx = new Gfx4(data, actorPalettes);
                }
            }
        }

        #endregion

        #region Load Actors

        if (actorsAddress != headerAddress)
        {
            reader.BaseStream.Seek(actorsAddress, SeekOrigin.Begin);
            while (true)
            {
                var objId = reader.ReadByte();
                if (objId == 0) break;
                Actors.Add(new(objId, new(reader.ReadByte(), reader.ReadByte()), reader.ReadByte()));
            }
        }

        #endregion
    
        #region Load Item Boxes
        if (itemBoxAddress != headerAddress)
        {
            reader.BaseStream.Seek(itemBoxAddress, SeekOrigin.Begin);
            while (true)
            {
                var boxId = reader.ReadByte();
                if (boxId == 0) break;
                ItemBoxes.Add(new(boxId, new(reader.ReadByte(), reader.ReadByte()), reader.ReadByte()));
            }
        }
        #endregion

        #region Load Positions

        if (positionsAddress != headerAddress)
        {
            reader.BaseStream.Seek(positionsAddress, SeekOrigin.Begin);
            while (true)
            {
                var posId = reader.ReadByte();
                if (posId == 0) break;
                Positions.Add(new(posId, new(reader.ReadByte(), reader.ReadByte()), reader.ReadByte()));
            }
        }

        #endregion
        
        #region Load Behaviors

        reader.BaseStream.Seek(behaviorAddress, SeekOrigin.Begin);
        Behaviors = reader.ReadBytes(256);

        #endregion

        #region Load Overlay

        if (overlayAddress != headerAddress)
        {
            reader.BaseStream.Seek(overlayAddress, SeekOrigin.Begin);
            while (true)
            {
                if (reader.ReadByte() == 0) break;
                Overlay.Add(new Point(reader.ReadByte(), reader.ReadByte()));
                reader.ReadByte();
            }
        }

        #endregion

        #region Load Minimap
        {
            reader.BaseStream.Seek(minimapAddress, SeekOrigin.Begin);
            byte[] data = Lz10.Decompress(reader).ToArray();
            _minimap = data;
        }
        #endregion
    }

    /// <summary>
    /// Writes track to given addresses
    /// </summary>
    /// <param name="writer">Binary writer</param>
    /// <param name="definition">The address of the new definition</param>
    /// <param name="header">The address of the new header</param>
    /// <returns>Size of track</returns>
    public uint Write(BinaryWriter writer, uint definition, uint header) {

        writer.BaseStream.Seek(definition, SeekOrigin.Begin);
        writer.Write(_trackId);
        writer.Write(_backgroundId);
        writer.Write(_backgroundBehavior);
        writer.Write(_animation);
        writer.Write(_material);
        writer.Write(_turnInstructions);
        writer.Write(_musicId);
        writer.Write(_targetSetTable);
        writer.Write(_tdUnknown);
        writer.Write(_trackArtGfx);
        writer.Write(_trackArtPalette);
        writer.Write(_trackLockedPalette);
        writer.Write(_trackNameGfx);
        writer.Write(Laps);

        const int headerSize = 0x100;
        uint pos = headerSize;

        #region Layout
        uint layoutOffset = pos;
        {
            Point trackSize = new Point(Tilemap.Layout.GetLength(0), Tilemap.Layout.GetLength(1));
            byte[] tmp = new byte[trackSize.X * trackSize.Y];
            for (int y = 0; y < trackSize.Y; y++)
            for (int x = 0; x < trackSize.X; x++)
                 tmp[x + y * trackSize.Y] = Tilemap.Layout[x, y];
            if (tmp.Length <= 4096)
            {
                Flags &= ~TrackFlags.SplitLayout;
                byte[] compressedLayout = Lz10.Compress(tmp);
                writer.BaseStream.Seek(header + layoutOffset, SeekOrigin.Begin);
                writer.Write(compressedLayout);
                pos += (uint)compressedLayout.Length;
            }
            else
            {
                Flags |= TrackFlags.SplitLayout;
                byte[][] parts = new byte[tmp.Length / 4096][];
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = Lz10.Compress(tmp[(i * 4096)..((i + 1) * 4096)]);
                }

                writer.BaseStream.Seek(header + pos, SeekOrigin.Begin);
                ushort localPos = 0x20;
                for (int i = 0; i < 16; i++)
                {
                    if (i < parts.Length)
                    {
                        writer.Write(localPos);
                        localPos += (ushort)parts[i].Length;
                    }
                    else
                        writer.Write((ushort)0);
                }

                pos += 0x20;
                foreach (var part in parts)
                {
                    writer.Write(part);
                    pos += (uint)part.Length;
                    Debug.Assert(writer.BaseStream.Position == pos + header);
                }
            }
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Minimap
        uint minimapOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + minimapOffset);
        {
            var data = Lz10.Compress(_minimap);
            writer.Write(data);
            pos += (uint)data.Length;
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Tileset
        uint tilesetOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + tilesetOffset);
        {
            byte[] tmp = Tileset.Indices;
            Debug.Assert(tmp.Length == 16384);
            
            Flags |= TrackFlags.SplitTileset;
            byte[][] parts = new byte[16384 / 4096][];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Lz10.Compress(tmp[(i * 4096)..((i + 1) * 4096)]);
            }

            ushort localPos = 0x20;
            for (int i = 0; i < 16; i++)
            {
                if (i < parts.Length)
                {
                    writer.Write(localPos);
                    localPos += (ushort)parts[i].Length;
                }
                else
                    writer.Write((ushort)0);
            }

            pos += 0x20;
            foreach (var part in parts)
            {
                writer.Write(part);
                pos += (uint)part.Length;
            }
        }
        uint tilesetPalOffset = pos;
        {
            Debug.Assert(writer.BaseStream.Position == header+tilesetPalOffset);
            var palette = Tileset.Palette;
            byte[] data = new byte[palette.Length*2];
            for (int i = 0; i < palette.Length*2; i+=2)
            {
                var col = BitConverter.GetBytes(new BgrColor(palette[i/2]).Raw);
                data[i] = col[0];
                data[i + 1] = col[1];
            }
            writer.Write(data);
            pos += (uint)data.Length;
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Behaviors
        uint behaviorsOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + behaviorsOffset);
        {
            writer.Write(Behaviors);
            pos += (uint)Behaviors.Length;
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region AI
        uint aiOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + aiOffset);
        {
            
            writer.Write((byte)AiSectors.Count);
            writer.Write((ushort)5);
            writer.Write((ushort)(5+12*AiSectors.Count));
            pos += 5;
            
            foreach (var sector in AiSectors)
            {
                sector.GetRawInputs(out _, out var shape, out var zone, out _, out _);
                writer.Write((byte)shape);
                writer.Write((ushort)zone.X);
                writer.Write((ushort)zone.Y);
                writer.Write((ushort)zone.Width);
                writer.Write((ushort)zone.Height);
                WritePadding(writer, 3);
                pos += 12;
            }

            for (int i = 0; i < 3; i++)
            {
                foreach (var sector in AiSectors)
                {
                    sector.GetRawInputs(out var target, out _, out _, out var speed, out var intersection);
                    writer.Write((ushort)target[i].X);
                    writer.Write((ushort)target[i].Y);
                    writer.Write((byte)(speed[i] | (intersection[i] ? 0x80 : 0)));
                    WritePadding(writer, 3);
                    pos += 8;
                }
            }
        }
        #endregion
        pos += (uint)Align(writer);
        #region ActorGfx

        uint actorGfxOffset = 0;
        uint actorPalOffset = 0;
        if (ReusedActorGfx == 0 && ActorGfx != null)
        {
            actorGfxOffset = pos;
            Debug.Assert(writer.BaseStream.Position == header + actorGfxOffset);
            byte[] bytes = ActorGfx.GetBytes();
            if (bytes.Length <= 4096)
            {
                Flags &= ~TrackFlags.SplitActorGfx;
                byte[] compressed = Lz10.Compress(bytes);
                writer.Write(compressed);
                pos += (uint)compressed.Length;
            }
            else
            {
                Flags |= TrackFlags.SplitActorGfx;
                byte[][] parts = new byte[bytes.Length / 4096][];
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = Lz10.Compress(bytes[(i * 4096)..((i + 1) * 4096)]);
                }

                ushort localPos = 0x20;
                for (int i = 0; i < 16; i++)
                {
                    if (i < parts.Length)
                    {
                        writer.Write(localPos);
                        localPos += (ushort)parts[i].Length;
                    }
                    else
                        writer.Write((ushort)0);
                }

                pos += 0x20;
                foreach (var part in parts)
                {
                    writer.Write(part);
                    pos += (uint)part.Length;
                }
            }
            actorPalOffset = pos;
            Debug.Assert(writer.BaseStream.Position == header + actorPalOffset);
            {
                var palettes = ActorGfx.Palettes;
                for (int i = 0; i < 4; i++)
                for (int j = 0; j < 16; j++)
                {
                    writer.Write(new BgrColor(palettes[j, i]).Raw);
                    pos += 2;
                }
            }
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Actors
        uint actorsOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + actorsOffset);
        {
            foreach (var obj in Actors)
            {
                writer.Write((byte)obj.Id);
                writer.Write((byte)obj.Position.X);
                writer.Write((byte)obj.Position.Y);
                writer.Write((byte)obj.Zone);
                pos += 4;
            }
            writer.Write((uint)0);
            pos += 4;
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Overlay
        uint overlayOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + overlayOffset);
        {
            foreach (var point in Overlay)
            {
                writer.Write((byte)1);
                writer.Write((byte)point.X);
                writer.Write((byte)point.Y);
                writer.Write((byte)0);
                pos += 4;
            }

            writer.Write((uint)0);
            pos += 4;
        }
        
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Boxes
        uint boxesOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + boxesOffset);
        {
            foreach (var obj in ItemBoxes)
            {
                writer.Write((byte)obj.Id);
                writer.Write((byte)obj.Position.X);
                writer.Write((byte)obj.Position.Y);
                writer.Write((byte)obj.Zone);
                pos += 4;
            }

            writer.Write((uint)0);
            pos += 4;
        }
        #endregion

        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        #region Positions
        uint positionsOffset = pos;
        Debug.Assert(writer.BaseStream.Position == header + positionsOffset);
        {
            foreach (var obj in Positions)
            {
                writer.Write((byte)obj.Id);
                writer.Write((byte)obj.Position.X);
                writer.Write((byte)obj.Position.Y);
                writer.Write((byte)obj.Zone);
                pos += 4;
            }

            writer.Write((uint)0);
            pos += 4;
        }
        #endregion
        Debug.Assert((writer.BaseStream.Position & ~3) == writer.BaseStream.Position);
        
        writer.BaseStream.Seek(header, SeekOrigin.Begin);
        var headStart = writer.BaseStream.Position;
        writer.Write((byte)Flags);
        writer.Write((byte)01);
        WritePadding(writer, 1);
        writer.Write((byte)Flags);
        writer.Write((byte)(Size.X/128));
        writer.Write((byte)(Size.Y/128));
        writer.Write((ushort)0);
        WritePadding(writer, 10*4);
        writer.Write(ReusedTileset);
        WritePadding(writer, 3*4);
        writer.Write(0x100u);
        WritePadding(writer, 15*4);
        writer.Write(tilesetOffset);
        writer.Write(tilesetPalOffset);
        writer.Write(behaviorsOffset);
        writer.Write(actorsOffset);
        writer.Write(overlayOffset);
        writer.Write(boxesOffset);
        writer.Write(positionsOffset);
        writer.Write(0x100100u);
        WritePadding(writer, 9*4);
        writer.Write(minimapOffset);
        writer.Write(0u);
        writer.Write(aiOffset);
        writer.Write(0u);
        writer.Write(layoutOffset);
        WritePadding(writer, 3 * 4);
        writer.Write(actorGfxOffset);
        writer.Write(actorPalOffset);
        writer.Write(ReusedActorGfx);
        WritePadding(writer, 4 * 4);
        
        Debug.Assert(writer.BaseStream.Position-headStart == 0x100);
        
        return pos;
    }

    private void WritePadding(BinaryWriter writer, int size)
    {
        if (size % 4 == 0)
            for (int i = 0; i<size/4; i++)
                writer.Write(0);
        else
            for (int i = 0; i<size; i++)
                writer.Write((byte)0);
    }

    private int Align(BinaryWriter writer)
    {
        long pos = writer.BaseStream.Position;
        int count = 0;
        while (pos%4!=0)
        {
            writer.Write((byte)0);
            pos++;
            count++;
        }

        return count;
    }
}