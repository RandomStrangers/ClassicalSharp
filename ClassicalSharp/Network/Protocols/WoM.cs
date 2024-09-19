﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
// This class was partially based on information from http://files.worldofminecraft.com/texturing/
// NOTE: http://files.worldofminecraft.com/ has been down for quite a while, so support was removed on Oct 10, 2015
using System;
using ClassicalSharp.Map;

namespace ClassicalSharp.Network.Protocols {

	/// <summary> Implements the WoM http environment protocol. </summary>
	public sealed class WoMProtocol : IProtocol {
		
		public WoMProtocol(Game game) : base(game) { }
		
		string womEnvIdentifier;
		int womCounter;
		bool sendWomId, sentWomId;
		
		public override void Reset() {
			womEnvIdentifier = "womenv_0";
			womCounter = 0;
			sendWomId = false; sentWomId = false;
		}

		public override void Tick() {
			Request item;
			game.Downloader.TryGetItem(womEnvIdentifier, out item);
			if (item != null && item.Data != null) {
				ParseWomConfig((string)item.Data);
			}
		}
		
		internal void CheckMotd() {
			if (net.ServerMotd == null) return;
			int index = net.ServerMotd.IndexOf("cfg=");
			if (game.PureClassic || index == -1) return;
			
			string host = net.ServerMotd.Substring(index + 4); // "cfg=".Length
			string url = "http://" + host;
			url = url.Replace("$U", game.Username);
			
			// Ensure that if the user quickly changes to a different world, env settings from old world aren't
			// applied in the new world if the async 'get env request' didn't complete before the old world was unloaded
			womCounter++;
			womEnvIdentifier = "womenv_" + womCounter;
			game.Downloader.AsyncGetString(url, true, womEnvIdentifier);
			sendWomId = true;
		}
		
		internal void CheckSendWomID() {
			if (sendWomId && !sentWomId) {
				game.Chat.Send("/womid WoMClient-2.0.7", false);
				sentWomId = true;
			}
		}
		
		void ParseWomConfig(string page) {
			string line;
			int start = 0;
			while ((line = ReadLine(ref start, page)) != null) {
				Utils.LogDebug(line);
				int sepIndex = line.IndexOf('=');
				if (sepIndex == -1) continue;
				string key = line.Substring(0, sepIndex).TrimEnd();
				string value = line.Substring(sepIndex + 1).TrimStart();
				
				if (Utils.CaselessEq(key, "environment.cloud")) {
					PackedCol col = ParseWomCol(value, WorldEnv.DefaultCloudsCol);
					game.World.Env.SetCloudsCol(col);
				} else if (Utils.CaselessEq(key, "environment.sky")) {
					PackedCol col = ParseWomCol(value, WorldEnv.DefaultSkyCol);
					game.World.Env.SetSkyCol(col);
				} else if (Utils.CaselessEq(key, "environment.fog")) {
					PackedCol col = ParseWomCol(value, WorldEnv.DefaultFogCol);
					game.World.Env.SetFogCol(col);
				} else if (Utils.CaselessEq(key, "environment.level")) {
					int waterLevel = 0;
					if (Int32.TryParse(value, out waterLevel))
						game.World.Env.SetEdgeLevel(waterLevel);
				} else if (Utils.CaselessEq(key, "user.detail") && !net.cpeData.useMessageTypes) {
					game.Chat.Add(value, MessageType.Status2);
				}
			}
		}
		
		const int fullAlpha = 0xFF << 24;
		static PackedCol ParseWomCol(string value, PackedCol defaultCol) {
			int argb;
			return Int32.TryParse(value, out argb) ? PackedCol.Argb(argb | fullAlpha) : defaultCol;
		}
		
		static string ReadLine(ref int start, string value) {
			if (start == -1) return null;
			for (int i = start; i < value.Length; i++) {
				char c = value[i];
				if (c != '\r' && c != '\n') continue;
				
				string line = value.Substring(start, i - start);
				start = i + 1;
				if (c == '\r' && start < value.Length && value[start] == '\n')
					start++; // we stop at the \r, so make sure to skip following \n
				return line;
			}
			
			string last = value.Substring(start);
			start = -1;
			return last;
		}
	}
}
