#region LICENSE
/*
Copyright(C) 2024 Andrew Lerdal

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

using AdvancedEdit.Types;
using AdvancedEdit.Compression;
using System;
using System.Collections.Generic;

namespace AdvancedEdit.TrackData
{
    /// <summary>
    /// An object that stores all relevant data for each track.
    /// </summary>
    public class Track
    {
        public byte[] TrackData;
        public int Address;
        public int RepeatTiles;
        public int TilesetPointerTable;
        public int LayoutPointerTable;
        public int PalettePointer;
        public int TileBehaviours;
        public int ObjectsPointer;
        public int OverlayPointer;
        public int MinimapPointer;
        public int AiZonesPointer;
        public int ItemBoxesPointer;
        public int EndlinePointer;
        public int TreeGfx;
        public int ObjectPalette;
        int TrackSize;
        public int[] TileBlocks;
        int tileLength;
        int layoutLength;
        public int[] LayoutBlocks;
        public byte[,] Indicies;

        public Tile[] Tiles;
        public Palette palette;
        
        public Track(int address,int nextTrackAddr)
        {
            //Load Track into seperate byte array
            TrackData = new byte[nextTrackAddr - address];
            Array.Copy(AdvancedEditor.file, address, TrackData, 0, nextTrackAddr - address);
            Address = address;

            if (address == 0x27f510 || address == 0x280580 || address == 0x281624 || address == 0x282c24)
            {
                return;
            }
            LoadOffsets();
            LoadLayout();
        }
        public void LoadOffsets()
        {
            TrackSize = TrackData[4] * 128;
            RepeatTiles = LittleEndianInt(TrackData[0x30..0x34]);
            TilesetPointerTable = LittleEndianInt(TrackData[0x80..0x84]);
            LayoutPointerTable = 0x100;
            PalettePointer = LittleEndianInt(TrackData[0x84..0x88]);
            TileBehaviours = LittleEndianInt(TrackData[0x88..0x8c]);
            ObjectsPointer = LittleEndianInt(TrackData[0x8c..0x90]);
            OverlayPointer = LittleEndianInt(TrackData[0x90..0x94]);
            ItemBoxesPointer = LittleEndianInt(TrackData[0x94..0x98]);
            EndlinePointer = LittleEndianInt(TrackData[0x98..0x9c]);
            MinimapPointer = LittleEndianInt(TrackData[0xc4..0xc8]);
            AiZonesPointer = LittleEndianInt(TrackData[0xcc..0xd0]);
            TreeGfx = LittleEndianInt(TrackData[0xe4..0xe8]);
            ObjectPalette = LittleEndianInt(TrackData[0xe8..0xec]);

            TileBlocks = new int[] {
                TilesetPointerTable + LittleEndianShort(TrackData[(TilesetPointerTable    )..(TilesetPointerTable + 2)]),
                TilesetPointerTable + LittleEndianShort(TrackData[(TilesetPointerTable + 2)..(TilesetPointerTable + 4)]),
                TilesetPointerTable + LittleEndianShort(TrackData[(TilesetPointerTable + 4)..(TilesetPointerTable + 6)]),
                TilesetPointerTable + LittleEndianShort(TrackData[(TilesetPointerTable + 6)..(TilesetPointerTable + 8)]),
            };
            LayoutBlocks = new int[] {
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 0)..(LayoutPointerTable + 2)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 2)..(LayoutPointerTable + 4)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 4)..(LayoutPointerTable + 6)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 6)..(LayoutPointerTable + 8)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 8)..(LayoutPointerTable + 10)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 10)..(LayoutPointerTable + 12)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 12)..(LayoutPointerTable + 14)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 14)..(LayoutPointerTable + 16)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 16)..(LayoutPointerTable + 18)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 18)..(LayoutPointerTable + 20)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 20)..(LayoutPointerTable + 22)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 22)..(LayoutPointerTable + 24)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 24)..(LayoutPointerTable + 26)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 26)..(LayoutPointerTable + 28)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 28)..(LayoutPointerTable + 30)]),
                LayoutPointerTable + LittleEndianShort(TrackData[(LayoutPointerTable + 30)..(LayoutPointerTable + 32)]),
            };
        }
        public void LoadLayout()
        {
            if (RepeatTiles == 0x00)
            {
                LoadLocalTiles();
            }
            else
            {
                LoadExternalTiles();
            }

            byte[] layout = new byte[TrackSize * TrackSize];
            int currentOffset = 0;
            layoutLength = 32;

            foreach (var o in LayoutBlocks)
            {
                if (o != 256)
                {
                    int len = LZ77.DecompressedLength(TrackData, o);
                    layoutLength += len;
                    var b = LZ77.DecompressRange(TrackData, o);
                    Array.Copy(b, 0, layout, currentOffset, b.Length);
                    currentOffset += b.Length;
                }
            }

            byte[,] output = new byte[TrackSize, TrackSize];
            for (int i = 0; i < TrackSize; i++)
            {
                for (int j = 0; j < TrackSize; j++)
                {
                    output[i, j] = layout[i * TrackSize + j];
                }
            }
            Indicies = output;
        }
        public void LoadLocalTiles()
        {
            byte[] tilegfx = new byte[4096 * 4];

            tileLength = 8 +
            LZ77.DecompressedLength(TrackData, TileBlocks[0]) +
            LZ77.DecompressedLength(TrackData, TileBlocks[1]) + 
            LZ77.DecompressedLength(TrackData, TileBlocks[2]) + 
            LZ77.DecompressedLength(TrackData, TileBlocks[3]);
            Array.Copy(LZ77.DecompressRange(TrackData, TileBlocks[0]), 0, tilegfx, 4096 * 0, 4096);
            Array.Copy(LZ77.DecompressRange(TrackData, TileBlocks[1]), 0, tilegfx, 4096 * 1, 4096);
            Array.Copy(LZ77.DecompressRange(TrackData, TileBlocks[2]), 0, tilegfx, 4096 * 2, 4096);
            Array.Copy(LZ77.DecompressRange(TrackData, TileBlocks[3]), 0, tilegfx, 4096 * 3, 4096);

            //Palette
            byte[] rawpal = new byte[128];
            Array.Copy(TrackData, PalettePointer, rawpal, 0, 128);
            Palette palette = new Palette(rawpal);
            Tiles = Tile.GenerateTiles(tilegfx, palette);
        }
        public void LoadExternalTiles()
        {
            //Palette
            byte[] rawpal = new byte[128];
            Array.Copy(TrackData, PalettePointer, rawpal, 0, 128);
            Palette palette = new Palette(rawpal);

            Track track = AdvancedEditor.tracks[AdvancedEditor.tracks.Count - (256 - RepeatTiles)];
            Tiles = new Tile[256];
            for(int i = 0; i  < 256; i++) 
            {
                var t = track.Tiles[i];
                t.palette = palette;
                Tiles[i] = t;
            }
        }
        public void PackData()
        {
            List<byte> rawTrack = new List<byte>(); //binary data of track

            // convert 2d indici array to single dimension
            byte[] indicies = new byte[Indicies.GetLength(0) * Indicies.GetLength(1)];
            int write = 0;
            for (int i = 0; i < Indicies.GetLength(0); i++)
            {
                for (int j = 0; j < Indicies.GetLength(1); j++)
                {
                    indicies[write++] = Indicies[i, j];
                }
            }
            #region Track Layout Compression
            List<byte> rawLayout = new List<byte>(); // binary data of the track layout

            rawLayout.AddRange(TrackData[LayoutPointerTable..(LayoutPointerTable + 32)]);
            for (int i = 0; i < TrackSize*TrackSize/4096; i++)
            {
                byte[] compressed = LZ77.CompressBytes(indicies[(i * 4096)..((i + 1) * 4096)]);
                rawLayout[i * 2] = ToLittleEndianShort((short)rawLayout.Count)[0];
                rawLayout[i * 2 + 1] = ToLittleEndianShort((short)rawLayout.Count)[1];
                rawLayout.AddRange(compressed);
            }
            int offset = rawLayout.Count - layoutLength;

            OffsetDataAfter(LayoutPointerTable, rawLayout.Count - layoutLength);
            WriteOffsets();
            
            #endregion
            #region Track Tile Compression
            List<byte> rawTiles = new List<byte>();
            rawTiles.AddRange(TrackData[TilesetPointerTable..(TilesetPointerTable+8)]);
            // Split tiles into 4 groups.
            for (int i = 0; i < 4; i++){
                byte[] tileGroupIndicies = new byte[4096];
                for (int j = 0; j < 64; j++){
                    for (int y = 0; y < 8; y++){
                        for (int x = 0; x < 8; x++){ // 4 nested loops! Yay!
                            tileGroupIndicies[j*64+y*8+x] = Tiles[i*64 + j].indicies[x,y];
                        }
                    }
                }
                rawTiles[i*2] = ToLittleEndianShort((short)rawTiles.Count)[0];
                rawTiles[i*2+1] = ToLittleEndianShort((short)rawTiles.Count)[1];
                rawTiles.AddRange(LZ77.CompressBytes(tileGroupIndicies));
            }

            OffsetDataAfter(TilesetPointerTable, rawTiles.Count - tileLength);
            WriteOffsets();

            #endregion



            rawTrack.AddRange(TrackData[0..LayoutPointerTable]);
            rawTrack.AddRange(rawLayout);
            rawTrack.AddRange(TrackData[(LayoutPointerTable + layoutLength)..TilesetPointerTable]);
            rawTrack.AddRange(rawTiles);
            rawTrack.AddRange(TrackData[(TilesetPointerTable + tileLength)..TrackData.Length]);
        

            layoutLength = rawLayout.Count;
            TrackData = rawTrack.ToArray();
            
        }
        public static byte[] CompileRom(List<Track> tracks)
        {
            int trackPointerTable = 0x258000;
            int position = tracks[0].Address;
            List<byte> rom = new List<byte>();
            rom.AddRange(AdvancedEditor.file[0..position]);
            for (int i = 0; i < tracks.Count; i++){
                Track track = tracks[i];
                byte[] relPos = ToLittleEndianInt(position-0x258000);
                rom[trackPointerTable + 4 * i + 0] = relPos[0]; rom[trackPointerTable + 4 * i + 1] = relPos[1];
                rom[trackPointerTable + 4 * i + 2] = relPos[2]; rom[trackPointerTable + 4 * i + 3] = relPos[3];
                track.Address = position;
                rom.AddRange(track.TrackData);
                position += track.TrackData.Length;
            }
            rom.AddRange(AdvancedEditor.file[position..AdvancedEditor.file.Length]);
            return rom.ToArray();
        }
        public void WriteOffsets()
        {
            Buffer.BlockCopy(ToLittleEndianInt(TilesetPointerTable),0,TrackData,0x80,4);
            Buffer.BlockCopy(ToLittleEndianInt(PalettePointer),0,TrackData,0x84,4);
            Buffer.BlockCopy(ToLittleEndianInt(TileBehaviours),0,TrackData,0x88,4);
            Buffer.BlockCopy(ToLittleEndianInt(ObjectsPointer),0,TrackData,0x8c,4);
            Buffer.BlockCopy(ToLittleEndianInt(OverlayPointer),0,TrackData,0x90,4);
            Buffer.BlockCopy(ToLittleEndianInt(ItemBoxesPointer),0,TrackData,0x94,4);
            Buffer.BlockCopy(ToLittleEndianInt(EndlinePointer),0,TrackData,0x98,4);
            Buffer.BlockCopy(ToLittleEndianInt(MinimapPointer),0,TrackData,0xc4,4);

            Buffer.BlockCopy(ToLittleEndianInt(AiZonesPointer),0,TrackData,0xcc,4);
            Buffer.BlockCopy(ToLittleEndianInt(TreeGfx),0,TrackData,0xe4,4);
            Buffer.BlockCopy(ToLittleEndianInt(ObjectPalette),0,TrackData,0xe8,4);
        }
        public void OffsetDataAfter(int finalPos, int offset)
        {
            if (finalPos < TilesetPointerTable) TilesetPointerTable += offset;
            if (finalPos < LayoutPointerTable) LayoutPointerTable += offset;
            if (finalPos < PalettePointer) PalettePointer += offset;
            if (finalPos < TileBehaviours) TileBehaviours += offset;
            if (finalPos < ObjectsPointer) ObjectsPointer += offset;
            if (finalPos < OverlayPointer) OverlayPointer += offset;
            if (finalPos < ItemBoxesPointer) ItemBoxesPointer += offset;
            if (finalPos < EndlinePointer) EndlinePointer += offset;
            if (finalPos < MinimapPointer) MinimapPointer += offset;
            if (finalPos < AiZonesPointer) AiZonesPointer += offset;
            /*if (finalPos < TreeGfx)*/ TreeGfx += offset;
            /*if (finalPos < ObjectPalette)*/ ObjectPalette += offset;

            // The 2 offsets above don't load. temp fix: assume they will be changed.
            // Todo: Fix loading

            /*for (int i = 0; i < TileBlocks.Length; i++)
            {
                if (finalPos < TileBlocks[i]) TileBlocks[i] += offset;
            }
            for (int i = 0; i < LayoutBlocks.Length; i++)
            {
                if (finalPos < LayoutBlocks[i]) LayoutBlocks[i] += offset;
            }*/

        }
        public static int LittleEndianInt(byte[] a)
        {
            return (a[3] << 24 | a[2] << 16 | a[1] << 8 | a[0]);
        }
        public static int LittleEndianShort(byte[] a)
        {
            return ( a[1] << 8 | a[0]);
        }
        public static byte[] ToLittleEndianInt(int a)
        {
            byte[] bytes = BitConverter.GetBytes(a);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
        public static byte[] ToLittleEndianShort(short a)
        {
            byte[] bytes = BitConverter.GetBytes(a);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public static void GenerateTracks()
        {
            int tracks = 0x258000;
            for (int i = 0; i < Enum.GetValues(typeof(TrackId)).Length; i++)
            {
                Track track = new Track(tracks + LittleEndianInt(AdvancedEditor.file[(tracks + i * 4)..(tracks + (i + 1) * 4)]), tracks + LittleEndianInt(AdvancedEditor.file[(tracks + (i + 1) * 4)..(tracks + (i + 2) * 4)]));
                AdvancedEditor.tracks.Add(track);
            }
        }
    }
    public enum TrackId
    {
        SNESMarioCircuit1,
        SNESDonutPlains1,
        SNESGhostValley1,
        SNESBowserCastle1,
        SNESMarioCircuit2,
        SNESChocoIsland1,
        SNESGhostValley2,
        SNESDonutPlains2,
        SNESBowserCastle2,
        SNESMarioCircuit3,
        SNESKoopaBeach1,
        SNESChocoIsland2,
        SNESVanillaLake1,
        SNESBowserCastle3,
        SNESMarioCircuit4,
        SNESDonutPlains3,
        SNESKoopaBeach2,
        SNESGhostValley3,
        SNESVanillaLake2,
        SNESRainbowRoad,
        SNESBattleCourse1,
        SNESBattleCourse2,
        SNESBattleCourse3,
        SNESBattleCourse4,
        
        PeachCircuit,
        ShyGuyBeach,
        SunsetWilds,
        BowserCastle1,
        LuigiCircuit,
        RiversidePark,
        YoshiDesert,
        BowserCastle2,
        MarioCircuit,
        CheepCheepIsland,
        RibbonRoad,
        BowserCastle3,
        SnowLand,
        BooLake,
        CheeseLand,
        RainbowRoad,
        SkyGarden,
        BrokenPier,
        BowserCastle4,
        LakesidePark,
        BattleCourse1,
        BattleCourse2,
        BattleCourse3,
        BattleCourse4,
        //TestTrack,
    }
}