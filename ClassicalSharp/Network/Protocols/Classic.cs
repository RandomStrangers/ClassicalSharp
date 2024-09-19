﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Gui;
using ClassicalSharp.Gui.Screens;
using ClassicalSharp.Entities;
using OpenTK;
using Ionic.Zlib;
using BlockID = System.UInt16;

namespace ClassicalSharp.Network.Protocols {

	/// <summary> Implements the packets for the original classic. </summary>
	public sealed class ClassicProtocol : IProtocol {
		
		public ClassicProtocol(Game game) : base(game) { }
		bool receivedFirstPosition;
		DateTime mapReceiveStart;
		DeflateStream gzipStream;
		GZipHeaderReader gzipHeader;
		int mapSizeIndex, mapIndex, mapVolume;
		byte[] mapSize = new byte[4], map;
		FixedBufferStream mapPartStream;
		Screen prevScreen;
		
		public override void Reset() {
			if (mapPartStream == null) mapPartStream = new FixedBufferStream(net.reader.buffer);
			receivedFirstPosition = false;
			
			net.Set(Opcode.Handshake, HandleHandshake, 131);
			net.Set(Opcode.Ping, HandlePing, 1);
			net.Set(Opcode.LevelInit, HandleLevelInit, 1);
			net.Set(Opcode.LevelDataChunk, HandleLevelDataChunk, 1028);
			net.Set(Opcode.LevelFinalise, HandleLevelFinalise, 7);
			net.Set(Opcode.SetBlock, HandleSetBlock, 8);
			
			net.Set(Opcode.AddEntity, HandleAddEntity, 74);
			net.Set(Opcode.EntityTeleport, HandleEntityTeleport, 10);
			net.Set(Opcode.RelPosAndOrientationUpdate, HandleRelPosAndOrientationUpdate, 7);
			net.Set(Opcode.RelPosUpdate, HandleRelPositionUpdate, 5);
			net.Set(Opcode.OrientationUpdate, HandleOrientationUpdate, 4);
			net.Set(Opcode.RemoveEntity, HandleRemoveEntity, 2);
			
			net.Set(Opcode.Message, HandleMessage, 66);
			net.Set(Opcode.Kick, HandleKick, 65);
			net.Set(Opcode.SetPermission, HandleSetPermission, 2);
		}
		
		public override void Tick() {
			if (receivedFirstPosition) {
				LocalPlayer player = game.LocalPlayer;
				WritePosition(player.Position, player.HeadY, player.HeadX);
			}
		}
		
		#if !ONLY_8BIT
		DeflateStream gzipStream2;
		byte[] map2;
		int mapIndex2;
		#endif
		
		#region Read

		void HandleHandshake() {
			byte protocolVer = reader.ReadUInt8();
			net.ServerName = reader.ReadString();
			net.ServerMotd = reader.ReadString();
			game.Chat.SetLogName(net.ServerName);
			
			game.LocalPlayer.Hacks.SetUserType(reader.ReadUInt8(), !net.cpeData.blockPerms);
			game.LocalPlayer.Hacks.HacksFlags = net.ServerName + net.ServerMotd;
			game.LocalPlayer.Hacks.UpdateHacksState();
		}
		
		void HandlePing() { }
		
		void HandleLevelInit() {
			if (gzipStream == null) StartLoadingState();
			
			// Fast map puts volume in header, doesn't bother with gzip
			if (net.cpeData.fastMap) {
				mapVolume = reader.ReadInt32();
				gzipHeader.done = true;
				mapSizeIndex = 4;
				map = new byte[mapVolume];
			}
		}
		
		void StartLoadingState() {
			game.World.Reset();
			Events.RaiseOnNewMap();
			
			prevScreen = game.Gui.activeScreen;
			if (prevScreen is LoadingScreen) prevScreen = null;
			
			game.Gui.SetNewScreen(new LoadingScreen(game, net.ServerName, net.ServerMotd), false);
			net.wom.CheckMotd();
			receivedFirstPosition = false;
			gzipHeader = new GZipHeaderReader();
			
			// Workaround because built in mono stream assumes that the end of stream
			// has been reached the first time a read call returns 0. (MS.NET doesn't)
			gzipStream = new DeflateStream(mapPartStream);
			
			#if !ONLY_8BIT
			gzipStream2 = new DeflateStream(mapPartStream);
			#endif
			
			mapSizeIndex = 0;
			mapIndex = 0;
			#if !ONLY_8BIT
			mapIndex2 = 0;
			#endif
			mapReceiveStart = DateTime.UtcNow;
		}
		
		void HandleLevelDataChunk() {
			// Workaround for some servers that send LevelDataChunk before LevelInit
			// due to their async packet sending behaviour.
			if (gzipStream == null) StartLoadingState();
			
			int usedLength = reader.ReadUInt16();
			mapPartStream.pos = 0;
			mapPartStream.bufferPos = reader.index;
			mapPartStream.len = usedLength;
			
			reader.Skip(1024);
			byte value = reader.ReadUInt8(); // progress in original classic, but we ignore it
			
			if (gzipHeader.done || gzipHeader.ReadHeader(mapPartStream)) {
				if (mapSizeIndex < 4) {
					mapSizeIndex += gzipStream.Read(mapSize, mapSizeIndex, 4 - mapSizeIndex);
				}

				if (mapSizeIndex == 4) {
					if (map == null) {
						mapVolume = mapSize[0] << 24 | mapSize[1] << 16 | mapSize[2] << 8 | mapSize[3];
						map = new byte[mapVolume];
					}
					
					#if !ONLY_8BIT
					if (reader.ExtendedBlocks && value != 0) {
						// Only allocate map2 when needed
						if (map2 == null) map2 = new byte[mapVolume];
						mapIndex2 += gzipStream2.Read(map2, mapIndex2, map2.Length - mapIndex2);
					} else {
						mapIndex += gzipStream.Read(map, mapIndex, map.Length - mapIndex);
					}
					#else
					mapIndex += gzipStream.Read(map, mapIndex, map.Length - mapIndex);
					#endif
				}
			}
			
			float progress = map == null ? 0 : (float)mapIndex / map.Length;
			Events.RaiseLoading(progress);
		}
		
		void HandleLevelFinalise() {
			game.Gui.SetNewScreen(null);
			game.Gui.activeScreen = prevScreen;
			game.Gui.CalcCursorVisible();
			prevScreen = null;
			
			int mapWidth  = reader.ReadUInt16();
			int mapHeight = reader.ReadUInt16();
			int mapLength = reader.ReadUInt16();
			
			double loadingMs = (DateTime.UtcNow - mapReceiveStart).TotalMilliseconds;
			Utils.LogDebug("map loading took: " + loadingMs);
			
			game.World.SetNewMap(map, mapWidth, mapHeight, mapLength);
			#if !ONLY_8BIT
			if (reader.ExtendedBlocks) {
				// defer allocation of scond map array if possible
				game.World.blocks2 = map2 == null ? map : map2;
				BlockInfo.SetMaxUsed(map2 == null ? 255 : 767);
			}
			#endif
			Events.RaiseOnNewMapLoaded();
			net.wom.CheckSendWomID();
			
			map = null;
			gzipStream.Dispose();
			gzipStream = null;
			#if !ONLY_8BIT
			map2 = null;
			gzipStream2.Dispose();
			gzipStream2 = null;
			#endif
			GC.Collect();
		}
		
		void HandleSetBlock() {
			int x = reader.ReadUInt16();
			int y = reader.ReadUInt16();
			int z = reader.ReadUInt16();
			BlockID block = reader.ReadBlock();
			if (game.World.IsValidPos(x, y, z)) {
				game.UpdateBlock(x, y, z, block);
			}
		}
		
		void HandleAddEntity() {
			byte id = reader.ReadUInt8();
			string name = reader.ReadString();
			string skin = name;
			CheckName(id, ref name, ref skin);
			AddEntity(id, name, skin, true);
			
			// Workaround for some servers that declare they support ExtPlayerList,
			// but doesn't send ExtAddPlayerName packets.
			AddTablistEntry(id, name, name, "Players", 0);
			classicTabList[id >> 3] |= (byte)(1 << (id & 0x7));
		}
		
		void HandleEntityTeleport() {
			byte id = reader.ReadUInt8();
			ReadAbsoluteLocation(id, true);
		}
		
		void HandleRelPosAndOrientationUpdate() {
			byte id = reader.ReadUInt8();
			Vector3 v;
			v.X = reader.ReadInt8() / 32f;
			v.Y = reader.ReadInt8() / 32f;
			v.Z = reader.ReadInt8() / 32f;
			
			float rotY =  (float)Utils.PackedToDegrees(reader.ReadUInt8());
			float headX = (float)Utils.PackedToDegrees(reader.ReadUInt8());
			LocationUpdate update = LocationUpdate.MakePosAndOri(v, rotY, headX, true);
			UpdateLocation(id, update, true);
		}
		
		void HandleRelPositionUpdate() {
			byte id = reader.ReadUInt8();
			Vector3 v;
			v.X = reader.ReadInt8() / 32f;
			v.Y = reader.ReadInt8() / 32f;
			v.Z = reader.ReadInt8() / 32f;
			
			LocationUpdate update = LocationUpdate.MakePos(v, true);
			UpdateLocation(id, update, true);
		}
		
		void HandleOrientationUpdate() {
			byte id = reader.ReadUInt8();
			float rotY =  (float)Utils.PackedToDegrees(reader.ReadUInt8());
			float headX = (float)Utils.PackedToDegrees(reader.ReadUInt8());
			
			LocationUpdate update = LocationUpdate.MakeOri(rotY, headX);
			UpdateLocation(id, update, true);
		}
		
		void HandleRemoveEntity() {
			byte id = reader.ReadUInt8();
			RemoveEntity(id);
		}
		
		void HandleMessage() {
			byte type = reader.ReadUInt8();
			// Original vanilla server uses player ids in message types, 255 for server messages.
			bool prepend = !net.cpeData.useMessageTypes && type == 0xFF;
			
			if (!net.cpeData.useMessageTypes) type = (byte)MessageType.Normal;
			string text = reader.ReadChatString(ref type);
			if (prepend) text = "&e" + text;
			
			if (!Utils.CaselessStarts(text, "^detail.user")) {
				game.Chat.Add(text, (MessageType)type);
			}
		}
		
		void HandleKick() {
			string reason = reader.ReadString();
			game.Disconnect("&eLost connection to the server", reason);
		}
		
		void HandleSetPermission() {
			game.LocalPlayer.Hacks.SetUserType(reader.ReadUInt8(), !net.cpeData.blockPerms);
			game.LocalPlayer.Hacks.UpdateHacksState();
		}
		
		internal void ReadAbsoluteLocation(byte id, bool interpolate) {
			Vector3 P = reader.ReadPosition(id);
			float rotY =  (float)Utils.PackedToDegrees(reader.ReadUInt8());
			float headX = (float)Utils.PackedToDegrees(reader.ReadUInt8());
			
			if (id == EntityList.SelfID) receivedFirstPosition = true;
			LocationUpdate update = LocationUpdate.MakePosAndOri(P, rotY, headX, false);
			UpdateLocation(id, update, interpolate);
		}
		#endregion
		
		#region Write
		
		internal void WriteChat(string text, bool partial) {
			int payload = !net.SupportsPartialMessages ? EntityList.SelfID : (partial ? 1 : 0);
			writer.WriteUInt8((byte)Opcode.Message);
			writer.WriteUInt8((byte)payload);
			writer.WriteString(text);
		}
		
		internal void WritePosition(Vector3 pos, float rotY, float headX) {
			int payload = net.cpeData.sendHeldBlock ? game.Inventory.Selected : EntityList.SelfID;
			writer.WriteUInt8((byte)Opcode.EntityTeleport);
			
			writer.WriteBlock((BlockID)payload); // held block when using HeldBlock, otherwise just 255
			writer.WritePosition(pos);
			writer.WriteUInt8(Utils.DegreesToPacked(rotY));
			writer.WriteUInt8(Utils.DegreesToPacked(headX));
		}
		
		internal void WriteSetBlock(int x, int y, int z, bool place, BlockID block) {
			writer.WriteUInt8((byte)Opcode.SetBlockClient);
			writer.WriteInt16((short)x);
			writer.WriteInt16((short)y);
			writer.WriteInt16((short)z);
			writer.WriteUInt8(place ? (byte)1 : (byte)0);
			writer.WriteBlock(block);
		}
		
		internal void WriteLogin(string username, string verKey) {
			byte payload = game.UseCPE ? (byte)0x42 : (byte)0x00;
			writer.WriteUInt8((byte)Opcode.Handshake);
			
			writer.WriteUInt8(7); // protocol version
			writer.WriteString(username);
			writer.WriteString(verKey);
			writer.WriteUInt8(payload);
		}
		
		#endregion
	}
}
