﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.IO;
using System.IO.Compression;
using ClassicalSharp.Entities;
using OpenTK;

namespace ClassicalSharp.Map {

	public sealed class MapCwExporter : IMapFormatExporter {

		BinaryWriter writer;
		NbtFile nbt;
		Game game;
		World map;
		
		public void Save(Stream stream, Game game) {
			using (GZipStream s = new GZipStream(stream, CompressionMode.Compress)) {
				writer = new BinaryWriter(s);
				nbt = new NbtFile(writer);
				this.game = game;
				map = game.World;
				
				nbt.Write(NbtTagType.Compound, "ClassicWorld");
				
				nbt.Write(NbtTagType.Int8, "FormatVersion"); 
				nbt.WriteUInt8(1);
				
				nbt.Write(NbtTagType.Int8Array, "UUID"); 
				nbt.WriteInt32(16);
				nbt.WriteBytes(map.Uuid.ToByteArray());
				
				nbt.Write(NbtTagType.Int16, "X"); 
				nbt.WriteInt16((short)map.Width);
				
				nbt.Write(NbtTagType.Int16, "Y"); 
				nbt.WriteInt16((short)map.Height);
				
				nbt.Write(NbtTagType.Int16, "Z"); 
				nbt.WriteInt16((short)map.Length);
				
				WriteSpawnCompoundTag();
				
				nbt.Write(NbtTagType.Int8Array, "BlockArray"); 
				nbt.WriteInt32(map.blocks.Length);
				nbt.WriteBytes(map.blocks);
				
				WriteMetadata();
				
				nbt.Write(NbtTagType.End);
			}
		}
		
		void WriteSpawnCompoundTag() {
			nbt.Write(NbtTagType.Compound, "Spawn");
			LocalPlayer p = game.LocalPlayer;
			Vector3 spawn = p.Position; // TODO: Maybe also keep real spawn too?
			
			nbt.Write(NbtTagType.Int16, "X"); 
			nbt.WriteInt16((short)spawn.X);
			
			nbt.Write(NbtTagType.Int16, "Y"); 
			nbt.WriteInt16((short)spawn.Y);
			
			nbt.Write(NbtTagType.Int16, "Z"); 
			nbt.WriteInt16((short)spawn.Z);
			
			nbt.Write(NbtTagType.Int8, "H");
			nbt.WriteUInt8(Utils.DegreesToPacked(p.SpawnRotY));
			
			nbt.Write(NbtTagType.Int8, "P");
			nbt.WriteUInt8(Utils.DegreesToPacked(p.SpawnHeadX));
			
			nbt.Write(NbtTagType.End);
		}
		
		void WriteMetadata() {
			nbt.Write(NbtTagType.Compound, "Metadata");
			nbt.Write(NbtTagType.Compound, "CPE");
			LocalPlayer p = game.LocalPlayer;

			nbt.WriteCpeExtCompound("ClickDistance", 1);
			nbt.Write(NbtTagType.Int16, "Distance"); 
			nbt.WriteInt16((short)(p.ReachDistance * 32));
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvWeatherType", 1);
			nbt.Write(NbtTagType.Int8, "WeatherType"); 
			nbt.WriteUInt8((byte)map.Env.Weather);
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvMapAppearance", 1);
			nbt.Write(NbtTagType.Int8, "SideBlock"); 
			nbt.WriteUInt8(map.Env.SidesBlock);
			nbt.Write(NbtTagType.Int8, "EdgeBlock"); 
			nbt.WriteUInt8(map.Env.EdgeBlock);
			nbt.Write(NbtTagType.Int16, "SideLevel"); 
			nbt.WriteInt16((short)map.Env.EdgeHeight);
			nbt.Write(NbtTagType.String, "TextureURL");
			string url = game.World.TextureUrl == null ? "" : game.World.TextureUrl;
			nbt.Write(url);
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvColors", 1);
			WriteColCompound("Sky", map.Env.SkyCol);
			WriteColCompound("Cloud", map.Env.CloudsCol);
			WriteColCompound("Fog", map.Env.FogCol);
			WriteColCompound("Ambient", map.Env.Shadow);
			WriteColCompound("Sunlight", map.Env.Sun);
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("BlockDefinitions", 1);
			for (int block = 1; block < 256; block++) {
				if (BlockInfo.IsCustomDefined((byte)block)) {
					WriteBlockDefinitionCompound((byte)block);
				}
			}
			nbt.Write(NbtTagType.End);
			
			nbt.Write(NbtTagType.End);
			nbt.Write(NbtTagType.End);
		}
		
		void WriteColCompound(string name, PackedCol col) {
			nbt.Write(NbtTagType.Compound, name);
			
			nbt.Write(NbtTagType.Int16, "R"); 
			nbt.WriteInt16(col.R);
			nbt.Write(NbtTagType.Int16, "G"); 
			nbt.WriteInt16(col.G);
			nbt.Write(NbtTagType.Int16, "B"); 
			nbt.WriteInt16(col.B);
			
			nbt.Write(NbtTagType.End);
		}
		
		unsafe void WriteBlockDefinitionCompound(byte id) {
			nbt.Write(NbtTagType.Compound, "Block" + id);
			bool sprite = BlockInfo.Draw[id] == DrawType.Sprite;
			
			nbt.Write(NbtTagType.Int8, "ID"); 
			nbt.WriteUInt8(id);
			nbt.Write(NbtTagType.String, "Name"); 
			nbt.Write(BlockInfo.Name[id]);
			nbt.Write(NbtTagType.Int8, "CollideType"); 
			nbt.WriteUInt8((byte)BlockInfo.Collide[id]);
			float speed = BlockInfo.SpeedMultiplier[id];
			nbt.Write(NbtTagType.Real32, "Speed"); 
			nbt.WriteInt32(*((int*)&speed));
			
			nbt.Write(NbtTagType.Int8Array, "Textures"); 
			nbt.WriteInt32(6);
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Top));
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Bottom));
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Left));
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Right));
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Front));
			nbt.WriteUInt8(BlockInfo.GetTextureLoc(id, Side.Back));
			
			nbt.Write(NbtTagType.Int8, "TransmitsLight"); 
			nbt.WriteUInt8(BlockInfo.BlocksLight[id] ? 0 : 1);
			nbt.Write(NbtTagType.Int8, "WalkSound"); 
			nbt.WriteUInt8(BlockInfo.DigSounds[id]);
			nbt.Write(NbtTagType.Int8, "FullBright"); 
			nbt.WriteUInt8(BlockInfo.FullBright[id] ? 1 : 0);
									
			nbt.Write(NbtTagType.Int8, "Shape");
			int shape = sprite ? 0 : (int)(BlockInfo.MaxBB[id].Y * 16);
			nbt.WriteUInt8(shape);			
			nbt.Write(NbtTagType.Int8, "BlockDraw");
			byte draw = sprite ? BlockInfo.SpriteOffset[id] : BlockInfo.Draw[id];
			nbt.WriteUInt8(draw);
			
			PackedCol col = BlockInfo.FogCol[id];
			nbt.Write(NbtTagType.Int8Array, "Fog"); 
			nbt.WriteInt32(4);
			byte fog = (byte)(128 * BlockInfo.FogDensity[id] - 1);
			nbt.WriteUInt8(BlockInfo.FogDensity[id] == 0 ? (byte)0 : fog);
			nbt.WriteUInt8(col.R); nbt.WriteUInt8(col.G); nbt.WriteUInt8(col.B);
			
			Vector3 min = BlockInfo.MinBB[id], max = BlockInfo.MaxBB[id];
			nbt.Write(NbtTagType.Int8Array, "Coords"); 
			nbt.WriteInt32(6);
			nbt.WriteUInt8((byte)(min.X * 16)); nbt.WriteUInt8((byte)(min.Y * 16)); 
			nbt.WriteUInt8((byte)(min.Z * 16)); nbt.WriteUInt8((byte)(max.X * 16));
			nbt.WriteUInt8((byte)(max.Y * 16)); nbt.WriteUInt8((byte)(max.Z * 16));
			
			nbt.Write(NbtTagType.End);
		}
	}
}