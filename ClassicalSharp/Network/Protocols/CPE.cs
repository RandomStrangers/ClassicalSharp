﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Entities;
using ClassicalSharp.Hotkeys;
using ClassicalSharp.Map;
using ClassicalSharp.Textures;
using OpenTK.Input;
using BlockID = System.UInt16;

namespace ClassicalSharp.Network.Protocols {

	/// <summary> Implements the packets for classic protocol extension. </summary>
	public sealed class CPEProtocol : IProtocol {
		
		public CPEProtocol(Game game) : base(game) { }
		int pingTicks;

		public override void Reset() {
			pingTicks = 0;
			if (!game.UseCPE) return;
			
			net.Set(Opcode.CpeExtInfo, HandleExtInfo, 67);
			net.Set(Opcode.CpeExtEntry, HandleExtEntry, 69);
			net.Set(Opcode.CpeSetClickDistance, HandleSetClickDistance, 3);
			net.Set(Opcode.CpeCustomBlockSupportLevel, HandleCustomBlockSupportLevel, 2);
			net.Set(Opcode.CpeHoldThis, HandleHoldThis, 3);
			net.Set(Opcode.CpeSetTextHotkey, HandleSetTextHotkey, 134);
			
			net.Set(Opcode.CpeExtAddPlayerName, HandleExtAddPlayerName, 196);
			net.Set(Opcode.CpeExtAddEntity, HandleExtAddEntity, 130);
			net.Set(Opcode.CpeExtRemovePlayerName, HandleExtRemovePlayerName, 3);
			
			net.Set(Opcode.CpeEnvColours, HandleEnvColours, 8);
			net.Set(Opcode.CpeMakeSelection, HandleMakeSelection, 86);
			net.Set(Opcode.CpeRemoveSelection, HandleRemoveSelection, 2);
			net.Set(Opcode.CpeSetBlockPermission, HandleSetBlockPermission, 4);
			net.Set(Opcode.CpeChangeModel, HandleChangeModel, 66);
			net.Set(Opcode.CpeEnvSetMapApperance, HandleEnvSetMapAppearance, 69);
			net.Set(Opcode.CpeEnvWeatherType, HandleEnvWeatherType, 2);
			net.Set(Opcode.CpeHackControl, HandleHackControl, 8);
			net.Set(Opcode.CpeExtAddEntity2, HandleExtAddEntity2, 138);
			
			net.Set(Opcode.CpeBulkBlockUpdate, HandleBulkBlockUpdate, 1282);
			net.Set(Opcode.CpeSetTextColor, HandleSetTextColor, 6);
			net.Set(Opcode.CpeSetMapEnvUrl, HandleSetMapEnvUrl, 65);
			net.Set(Opcode.CpeSetMapEnvProperty, HandleSetMapEnvProperty, 6);
			net.Set(Opcode.CpeSetEntityProperty, HandleSetEntityProperty, 7);
			net.Set(Opcode.CpeTwoWayPing, HandleTwoWayPing, 4);
			net.Set(Opcode.CpeSetInventoryOrder, HandleSetInventoryOrder, 3);
		}
		
		public override void Tick() {
			pingTicks++;
			if (pingTicks >= 20 && net.cpeData.twoWayPing) {
				WriteTwoWayPing(false, PingList.NextTwoWayPingData());
				pingTicks = 0;
			}
		}
		
		#region Read
		void HandleExtInfo() {
			string appName = reader.ReadString();
			game.Chat.Add("Server software: " + appName);
			if (Utils.CaselessStarts(appName, "D3 server"))
				net.cpeData.needD3Fix = true;
			
			// Workaround for old MCGalaxy that send ExtEntry sync but ExtInfo async. This means
			// ExtEntry may sometimes arrive before ExtInfo, thus have to use += instead of =
			net.cpeData.ServerExtensionsCount += reader.ReadUInt16();
			SendCpeExtInfoReply();
		}
		
		void HandleExtEntry() {
			string extName = reader.ReadString();
			int extVersion = reader.ReadInt32();
			Utils.LogDebug("cpe ext: {0}, {1}", extName, extVersion);
			
			net.cpeData.HandleEntry(extName, extVersion, net);
			SendCpeExtInfoReply();
		}
		
		void HandleSetClickDistance() {
			game.LocalPlayer.ReachDistance = reader.ReadUInt16() / 32f;
		}
		
		void HandleCustomBlockSupportLevel() {
			byte supportLevel = reader.ReadUInt8();
			WriteCustomBlockSupportLevel(1);
			net.SendPacket();
			game.SupportsCPEBlocks = true;
			Events.RaiseBlockPermissionsChanged();
		}
		
		void HandleHoldThis() {
			BlockID block = reader.ReadBlock();
			bool canChange = reader.ReadUInt8() == 0;
			
			game.Inventory.CanChangeHeldBlock = true;
			game.Inventory.Selected = block;
			game.Inventory.CanChangeHeldBlock = canChange;
			game.Inventory.CanPick = block != Block.Air;
		}
		
		void HandleSetTextHotkey() {
			string label = reader.ReadString();
			string action = reader.ReadString();
			int keyCode = reader.ReadInt32();
			byte keyMods = reader.ReadUInt8();
			
			#if !ANDROID
			if (keyCode < 0 || keyCode > 255) return;
			Key key = LwjglToKey.Map[keyCode];
			if (key == Key.None) return;
			
			Utils.LogDebug("CPE Hotkey added: " + key + "," + keyMods + " : " + action);
			if (action.Length == 0) {
				HotkeyList.Remove(key, keyMods);
			} else if (action[action.Length - 1] == '◙') { // graphical form of \n
				action = action.Substring(0, action.Length - 1);
				HotkeyList.Add(key, keyMods, action, false);
			} else { // more input needed by user
				HotkeyList.Add(key, keyMods, action, true);
			}
			#endif
		}
		
		void HandleExtAddPlayerName() {
			byte id = (byte)reader.ReadUInt16();
			string playerName = Utils.StripColours(reader.ReadString());
			playerName = Utils.RemoveEndPlus(playerName);
			string listName = reader.ReadString();
			listName = Utils.RemoveEndPlus(listName);
			string groupName = reader.ReadString();
			byte groupRank = reader.ReadUInt8();
			
			// Workaround for server software that declares support for ExtPlayerList, but sends AddEntity then AddPlayerName
			int mask = id >> 3, bit = 1 << (id & 0x7);
			classicTabList[mask] &= (byte)~bit;
			AddTablistEntry((byte)id, playerName, listName, groupName, groupRank);
		}
		
		void HandleExtAddEntity() {
			byte id = reader.ReadUInt8();
			string displayName = reader.ReadString();
			string skinName = reader.ReadString();
			
			CheckName(id, ref displayName, ref skinName);
			AddEntity(id, displayName, skinName, false);
		}
		
		void HandleExtRemovePlayerName() {
			byte id = (byte)reader.ReadUInt16();
			RemoveTablistEntry(id);
		}
		
		void HandleMakeSelection() {
			byte selectionId = reader.ReadUInt8();
			string label = reader.ReadString();
			
			Vector3I p1;
			p1.X = reader.ReadInt16();
			p1.Y = reader.ReadInt16();
			p1.Z = reader.ReadInt16();
			
			Vector3I p2;
			p2.X = reader.ReadInt16();
			p2.Y = reader.ReadInt16();
			p2.Z = reader.ReadInt16();
			
			byte r = (byte)reader.ReadUInt16();
			byte g = (byte)reader.ReadUInt16();
			byte b = (byte)reader.ReadUInt16();
			byte a = (byte)reader.ReadUInt16();
			
			PackedCol col = new PackedCol(r, g, b, a);
			game.SelectionManager.AddSelection(selectionId, p1, p2, col);
		}
		
		void HandleRemoveSelection() {
			byte selectionId = reader.ReadUInt8();
			game.SelectionManager.RemoveSelection(selectionId);
		}
		
		void HandleEnvColours() {
			byte variable = reader.ReadUInt8();
			ushort r = reader.ReadUInt16();
			ushort g = reader.ReadUInt16();
			ushort b = reader.ReadUInt16();
			bool invalid = r > 255 || g > 255 || b > 255;
			PackedCol col = new PackedCol(r, g, b);

			if (variable == 0) {
				game.World.Env.SetSkyCol(invalid ? WorldEnv.DefaultSkyCol : col);
			} else if (variable == 1) {
				game.World.Env.SetCloudsCol(invalid ? WorldEnv.DefaultCloudsCol : col);
			} else if (variable == 2) {
				game.World.Env.SetFogCol(invalid ? WorldEnv.DefaultFogCol : col);
			} else if (variable == 3) {
				game.World.Env.SetShadowCol(invalid ? WorldEnv.DefaultShadowlight : col);
			} else if (variable == 4) {
				game.World.Env.SetSunCol(invalid ? WorldEnv.DefaultSunlight : col);
			}
		}
		
		void HandleSetBlockPermission() {
			BlockID block = reader.ReadBlock();
			BlockInfo.CanPlace[block]  = reader.ReadUInt8() != 0;
			BlockInfo.CanDelete[block] = reader.ReadUInt8() != 0;
			Events.RaiseBlockPermissionsChanged();
		}
		
		void HandleChangeModel() {
			byte id = reader.ReadUInt8();
			string modelName = reader.ReadString();
			Entity entity = game.Entities.List[id];
			if (entity != null) entity.SetModel(modelName);
		}
		
		void HandleEnvSetMapAppearance() {
			HandleSetMapEnvUrl();
			game.World.Env.SetSidesBlock(reader.ReadUInt8());
			game.World.Env.SetEdgeBlock(reader.ReadUInt8());
			game.World.Env.SetEdgeLevel(reader.ReadInt16());
			if (net.cpeData.envMapVer == 1) return;
			
			// Version 2
			game.World.Env.SetCloudsLevel(reader.ReadInt16());
			short maxViewDist = reader.ReadInt16();
			game.MaxViewDistance = maxViewDist <= 0 ? 32768 : maxViewDist;
			game.SetViewDistance(game.UserViewDistance);
		}
		
		void HandleEnvWeatherType() {
			game.World.Env.SetWeather((Weather)reader.ReadUInt8());
		}
		
		void HandleHackControl() {
			LocalPlayer p = game.LocalPlayer;
			p.Hacks.CanFly = reader.ReadUInt8() != 0;
			p.Hacks.CanNoclip = reader.ReadUInt8() != 0;
			p.Hacks.CanSpeed = reader.ReadUInt8() != 0;
			p.Hacks.CanRespawn = reader.ReadUInt8() != 0;
			p.Hacks.CanUseThirdPersonCamera = reader.ReadUInt8() != 0;
			p.CheckHacksConsistency();
			
			ushort jumpHeight = reader.ReadUInt16();
			if (jumpHeight == ushort.MaxValue) { // special value of -1 to reset default
				p.physics.jumpVel = p.Hacks.CanJumpHigher ? p.physics.userJumpVel : 0.42f;
			} else {
				p.physics.CalculateJumpVelocity(jumpHeight / 32f);
			}
			
			p.physics.serverJumpVel = p.physics.jumpVel;
			Events.RaiseHackPermissionsChanged();
		}
		
		void HandleExtAddEntity2() {
			byte id = reader.ReadUInt8();
			string displayName = reader.ReadString();
			string skinName = reader.ReadString();
			
			CheckName(id, ref displayName, ref skinName);
			AddEntity(id, displayName, skinName, true);
		}
		
		const int bulkCount = 256;
		unsafe void HandleBulkBlockUpdate() {
			int count = reader.ReadUInt8() + 1;
			World map = game.World;
			int mapSize = map.HasBlocks ? map.blocks.Length : 0;
			
			int* indices = stackalloc int[bulkCount];
			for (int i = 0; i < count; i++) {
				indices[i] = reader.ReadInt32();
			}
			reader.Skip((bulkCount - count) * sizeof(int));
			
			BlockID* blocks = stackalloc BlockID[bulkCount];
			for (int i = 0; i < count; i++) {
				blocks[i] = reader.buffer[reader.index + i];
			}
			reader.Skip(bulkCount);
			
			if (reader.ExtendedBlocks) {
				for (int i = 0; i < count; i += 4) {
					byte flags = reader.buffer[reader.index + (i >> 2)];
					blocks[i + 0] |= (BlockID)((flags & 0x03) << 8);
					blocks[i + 1] |= (BlockID)((flags & 0x0C) << 6);
					blocks[i + 2] |= (BlockID)((flags & 0x30) << 4);
					blocks[i + 3] |= (BlockID)((flags & 0xC0) << 2);
				}
				reader.Skip(bulkCount / 4);
			}
			
			for (int i = 0; i < count; i++) {
				int index = indices[i];
				if (index < 0 || index >= mapSize) continue;
				
				int x = index % map.Width;
				int y = index / (map.Width * map.Length);
				int z = (index / map.Width) % map.Length;
				if (map.IsValidPos(x, y, z)) {
					game.UpdateBlock(x, y, z, blocks[i]);
				}
			}
		}
		
		void HandleSetTextColor() {
			PackedCol col = new PackedCol(reader.ReadUInt8(), reader.ReadUInt8(),
			                                reader.ReadUInt8(), reader.ReadUInt8());
			byte code = reader.ReadUInt8();
			
			// disallow space, null, and colour code specifiers
			if (code == '\0' || code == ' ' || code == 0xFF) return;
			if (code == '%' || code == '&') return;
			
			IDrawer2D.Cols[code] = col;
			Events.RaiseColCodeChanged((char)code);
		}
		
		void HandleSetMapEnvUrl() {
			string url = reader.ReadString();
			if (!game.AllowServerTextures) return;
			
			if (url.Length == 0) {
				if (game.World.TextureUrl != null) TexturePack.ExtractDefault(game);
			} else if (Utils.IsUrlPrefix(url, 0)) {
				net.RetrieveTexturePack(url);
			}
			Utils.LogDebug("Image url: " + url);
		}
		
		void HandleSetMapEnvProperty() {
			byte type = reader.ReadUInt8();
			int value = reader.ReadInt32();
			WorldEnv env = game.World.Env;
			Utils.Clamp(ref value, -0xFFFFFF, 0xFFFFFF);
			int maxBlock = BlockInfo.Count - 1;
			
			switch (type) {
				case 0:
					Utils.Clamp(ref value, 0, maxBlock);
					env.SetSidesBlock((BlockID)value); break;
				case 1:
					Utils.Clamp(ref value, 0, maxBlock);
					env.SetEdgeBlock((BlockID)value); break;
				case 2:
					env.SetEdgeLevel(value); break;
				case 3:
					env.SetCloudsLevel(value); break;
				case 4:
					Utils.Clamp(ref value, -0x7FFF, 0x7FFF);
					game.MaxViewDistance = value <= 0 ? 32768 : value;
					game.SetViewDistance(game.UserViewDistance); break;
				case 5:
					env.SetCloudsSpeed(value / 256f); break;
				case 6:
					env.SetWeatherSpeed(value / 256f); break;
				case 7:
					Utils.Clamp(ref value, byte.MinValue, byte.MaxValue);
					env.SetWeatherFade(value / 128f); break;
				case 8:
					env.SetExpFog(value != 0); break;
				case 9:
					env.SetSidesOffset(value); break;
				case 10:
					env.SetSkyboxHorSpeed(value / 1024f); break;
				case 11:
					env.SetSkyboxVerSpeed(value / 1024f); break;
			}
		}
		
		void HandleSetEntityProperty() {
			byte id = reader.ReadUInt8();
			byte type = reader.ReadUInt8();
			int value = reader.ReadInt32();
			
			Entity entity = game.Entities.List[id];
			if (entity == null) return;
			LocationUpdate update = default(LocationUpdate);
			
			switch (type) {
				case 0:
					update.Flags |= LocationUpdateFlag.RotX;
					update.RotX = LocationUpdate.Clamp(value); break;
				case 1:
					update.Flags |= LocationUpdateFlag.HeadY;
					update.HeadY = LocationUpdate.Clamp(value); break;
				case 2:
					update.Flags |= LocationUpdateFlag.RotZ;
					update.RotZ = LocationUpdate.Clamp(value); break;
					
				case 3:
				case 4:
				case 5:
					float scale = value / 1000.0f;
					Utils.Clamp(ref scale, 0.01f, entity.Model.MaxScale);
					if (type == 3) entity.ModelScale.X = scale;
					if (type == 4) entity.ModelScale.Y = scale;
					if (type == 5) entity.ModelScale.Z = scale;
					
					entity.UpdateModelBounds();
					return;
				default:
					return;
			}
			entity.SetLocation(update, true);
		}
		
		void HandleTwoWayPing() {
			bool serverToClient = reader.ReadUInt8() != 0;
			ushort data = reader.ReadUInt16();
			if (!serverToClient) { PingList.Update(data); return; }
			
			WriteTwoWayPing(true, data); // server to client reply
			net.SendPacket();
		}
		
		void HandleSetInventoryOrder() {
			BlockID block = reader.ReadBlock();
			BlockID order = reader.ReadBlock();
			
			game.Inventory.Remove(block);
			if (order != 0) { game.Inventory.Map[order - 1] = block; }
		}
		
		#endregion
		
		#region Write
		
		internal void WritePlayerClick(MouseButton button, bool buttonDown, 
		                               byte targetId, PickedPos pos) {
			Player p = game.LocalPlayer;
			writer.WriteUInt8((byte)Opcode.CpePlayerClick);
			writer.WriteUInt8((byte)button);
			writer.WriteUInt8(buttonDown ? (byte)0 : (byte)1);
			writer.WriteInt16((short)Utils.DegreesToPacked(p.HeadY, 65536));
			writer.WriteInt16((short)Utils.DegreesToPacked(p.HeadX, 65536));
			
			writer.WriteUInt8(targetId);
			writer.WriteInt16((short)pos.BlockPos.X);
			writer.WriteInt16((short)pos.BlockPos.Y);
			writer.WriteInt16((short)pos.BlockPos.Z);
			writer.WriteUInt8((byte)pos.Face);
		}
		
		void WriteExtInfo(string appName, int extensionsCount) {
			writer.WriteUInt8((byte)Opcode.CpeExtInfo);
			writer.WriteString(appName);
			writer.WriteInt16((short)extensionsCount);
		}
		
		void WriteExtEntry(string extensionName, int extensionVersion) {
			writer.WriteUInt8((byte)Opcode.CpeExtEntry);
			writer.WriteString(extensionName);
			writer.WriteInt32(extensionVersion);
		}
		
		void WriteCustomBlockSupportLevel(byte version) {
			writer.WriteUInt8((byte)Opcode.CpeCustomBlockSupportLevel);
			writer.WriteUInt8(version);
		}
		
		void WriteTwoWayPing(bool serverToClient, ushort data) {
			writer.WriteUInt8((byte)Opcode.CpeTwoWayPing);
			writer.WriteUInt8((byte)(serverToClient ? 1 : 0));
			writer.WriteInt16((short)data);
		}
		
		void SendCpeExtInfoReply() {
			if (net.cpeData.ServerExtensionsCount != 0) return;
			string[] clientExts = CPESupport.ClientExtensions;
			int count = clientExts.Length;
			if (!game.AllowCustomBlocks) count -= 2;
			#if !ONLY_8BIT
			if (!game.AllowCustomBlocks) count -= 1;
			#endif
			
			WriteExtInfo(net.AppName, count);
			net.SendPacket();
			for (int i = 0; i < clientExts.Length; i++) {
				string name = clientExts[i];
				int ver = 1;
				if (name == "ExtPlayerList") ver = 2;
				if (name == "EnvMapAppearance") ver = net.cpeData.envMapVer;
				if (name == "BlockDefinitionsExt") ver = net.cpeData.blockDefsExtVer;
				
				if (!game.AllowCustomBlocks && name == "BlockDefinitionsExt") continue;
				if (!game.AllowCustomBlocks && name == "BlockDefinitions")    continue;
				#if !ONLY_8BIT
				if (!game.AllowCustomBlocks && name == "ExtendedBlocks")      continue;
				#endif
				
				WriteExtEntry(name, ver);
				net.SendPacket();
			}
		}
		#endregion
	}
}
