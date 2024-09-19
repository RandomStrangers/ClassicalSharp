﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
//#define TEST_VANILLA
using System;
using System.IO;
using System.Net;
using ClassicalSharp.Entities;
using ClassicalSharp.Generator;
using ClassicalSharp.Gui.Screens;
using ClassicalSharp.Physics;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp.Singleplayer {

	public sealed class SinglePlayerServer : IServerConnection {
		
		internal PhysicsBase physics;
		
		public SinglePlayerServer(Game game) {
			this.game = game;
			physics = new PhysicsBase(game);
			SupportsFullCP437 = !game.ClassicMode;
			SupportsPartialMessages = true;
			IsSinglePlayer = true;
		}
		
		public override void BeginConnect() {
			game.Chat.SetLogName("Singleplayer");
			game.SupportsCPEBlocks = game.UseCPE;
			int max = game.SupportsCPEBlocks ? Block.MaxCpeBlock : Block.MaxOriginalBlock;
			for (int i = 1; i <= max; i++) {
				BlockInfo.CanPlace[i] = true;
				BlockInfo.CanDelete[i] = true;
			}
			Events.RaiseBlockPermissionsChanged();
			
			// For when user drops a map file onto ClassicalSharp.exe
			string path = game.Username;
			if (path.IndexOf(Path.DirectorySeparatorChar) >= 0 && Platform.FileExists(path)) {
				LoadLevelScreen.LoadMap(game, path);
				game.Gui.SetNewScreen(null);
				return;
			}
			
			NotchyGenerator gen = new NotchyGenerator();
			gen.Width = 128; gen.Height = 64; gen.Length = 128;
			gen.Seed = new Random().Next();
			
			game.Gui.SetNewScreen(new GeneratingMapScreen(game, gen));
		}
		
		char lastCol = '\0';
		public override void SendChat(string text) {
			if (String.IsNullOrEmpty(text)) return;
			lastCol = '\0';
			
			while (text.Length > Utils.StringLength) {
				AddChat(text.Substring(0, Utils.StringLength));
				text = text.Substring(Utils.StringLength);
			}
			AddChat(text);
		}
		
		void AddChat(string text) {
			text = text.TrimEnd().Replace('%', '&');
			if (!IDrawer2D.IsWhiteCol(lastCol))
				text = "&" + lastCol + text;
			
			char col = IDrawer2D.LastCol(text, text.Length);
			if (col != '\0') lastCol = col;
			game.Chat.Add(text, MessageType.Normal);
		}
		
		public override void SendPosition(Vector3 pos, float rotY, float headX) {
		}
		
		public override void SendPlayerClick(MouseButton button, bool buttonDown, byte targetId, PickedPos pos) {
		}
		
		public override void OnNewMap(Game game) { }
		public override void Reset(Game game) { }
		public override void Dispose() {
			physics.Dispose();
		}
		
		public override void Tick(ScheduledTask task) {
			if (Disconnected) return;
			// Network ticked 60 times a second, only do physics 20 times a second
			if ((netTicks % 3) == 0) {
				physics.Tick();
				CheckAsyncResources();
			}
			netTicks++;
		}
	}
}