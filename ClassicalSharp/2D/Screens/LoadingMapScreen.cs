﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using ClassicalSharp.Entities;
using ClassicalSharp.Generator;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Gui.Widgets;
using ClassicalSharp.Model;
using ClassicalSharp.Textures;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp.Gui.Screens {
	public class LoadingScreen : Screen {
		
		readonly Font font;
		public LoadingScreen(Game game, string title, string message) : base(game) {
			this.title = title;
			this.message = message;
			font = new Font(game.FontName, 16);
			BlocksWorld = true;
			RenderHudOver = true;
		}
		
		string title, message;
		float progress;
		TextWidget titleWidget, messageWidget;
		const int progWidth = 200, progHeight = 4;
		readonly PackedCol backCol = new PackedCol(128, 128, 128);
		readonly PackedCol progressCol = new PackedCol(128, 255, 128);

		
		public override void Init() {
			game.Graphics.Fog = false;
			ContextRecreated();
			
			Events.Loading          += Loading;
			Events.ContextLost      += ContextLost;
			Events.ContextRecreated += ContextRecreated;
		}
		
		public void SetTitle(string title) {
			this.title = title;
			if (titleWidget != null) titleWidget.Dispose();
			
			titleWidget = TextWidget.Create(game, title, font)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, -31);
		}
		
		public void SetMessage(string message) {
			this.message = message;
			if (messageWidget != null) messageWidget.Dispose();
			
			messageWidget = TextWidget.Create(game, message, font)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, 17);
		}
		
		public void SetProgress(float progress) {
			this.progress = progress;
		}
		
		public override void Dispose() {
			font.Dispose();
			ContextLost();
			
			Events.Loading          -= Loading;
			Events.ContextLost      -= ContextLost;
			Events.ContextRecreated -= ContextRecreated;
		}
		
		public override void OnResize() {
			messageWidget.Reposition();
			titleWidget.Reposition();
		}
		
		void Loading(float progress) { this.progress = progress; }
		
		protected override void ContextLost() {
			if (messageWidget == null) return;
			messageWidget.Dispose();
			titleWidget.Dispose();
		}
		
		protected override void ContextRecreated() {
			if (game.Graphics.LostContext) return;
			SetTitle(title);
			SetMessage(message);
		}
		
		
		public override bool HandlesKeyDown(Key key) {
			if (key == Key.Tab) return true;
			return game.Gui.hudScreen.HandlesKeyDown(key);
		}
		
		public override bool HandlesKeyPress(char key)  {
			return game.Gui.hudScreen.HandlesKeyPress(key);
		}
		
		public override bool HandlesKeyUp(Key key) {
			if (key == Key.Tab) return true;
			return game.Gui.hudScreen.HandlesKeyUp(key);
		}
		
		public override bool HandlesMouseDown(int mouseX, int mouseY, MouseButton button) {
			if (game.Gui.hudScreen.HandlesAllInput)
				game.Gui.hudScreen.HandlesMouseDown(mouseX, mouseY, button);
			return true;
		}
		
		public override bool HandlesMouseUp(int mouseX, int mouseY, MouseButton button) { return true; }		
		public override bool HandlesMouseMove(int mouseX, int mouseY) { return true; }		
		public override bool HandlesMouseScroll(float delta)  { return true; }

		public override void Render(double delta) {
			IGraphicsApi gfx = game.Graphics;
			gfx.Texturing = true;
			DrawBackground();
			titleWidget.Render(delta);
			messageWidget.Render(delta);
			gfx.Texturing = false;
			
			int x = CalcPos(Anchor.Centre,  0, progWidth,  game.Width);
			int y = CalcPos(Anchor.Centre, 34, progHeight, game.Height);

			gfx.Draw2DQuad(x, y, progWidth,                   progHeight, backCol);
			gfx.Draw2DQuad(x, y, (int)(progWidth * progress), progHeight, progressCol);
		}
		
		void DrawBackground() {
			VertexP3fT2fC4b[] vertices = game.ModelCache.vertices;
			int index = 0, atlasIndex = 0;
			int drawnY = 0, height = game.Height;
			PackedCol col = new PackedCol(64, 64, 64);
			
			int texLoc = BlockInfo.GetTextureLoc(Block.Dirt, Side.Top);
			Texture tex = new Texture(0, 0, 0, game.Width, 64,
			                          Atlas1D.GetTexRec(texLoc, 1, out atlasIndex));
			tex.U2 = (float)game.Width / 64;
			bool bound = false;
			
			while (drawnY < height) {
				tex.Y1 = drawnY;
				IGraphicsApi.Make2DQuad(ref tex, col, vertices, ref index);
				if (index >= vertices.Length)
					DrawBackgroundVertices(ref index, atlasIndex, ref bound);
				drawnY += 64;
			}
			DrawBackgroundVertices(ref index, atlasIndex, ref bound);
		}
		
		void DrawBackgroundVertices(ref int index, int atlasIndex, ref bool bound) {
			if (index == 0) return;
			if (!bound) {
				bound = true;
				game.Graphics.BindTexture(Atlas1D.TexIds[atlasIndex]);
			}
			
			ModelCache cache = game.ModelCache;
			game.Graphics.SetBatchFormat(VertexFormat.P3fT2fC4b);
			game.Graphics.UpdateDynamicVb_IndexedTris(cache.vb, cache.vertices, index);
			index = 0;
		}
	}
	
	public class GeneratingMapScreen : LoadingScreen {
		
		string lastState;
		IMapGenerator gen;
		public GeneratingMapScreen(Game game, IMapGenerator gen) : base(game, "Generating level", "Generating..") {
			this.gen = gen;
		}
		
		public override void Init() {
			game.World.Reset();
			Events.RaiseOnNewMap();
			GC.Collect();
			
			base.Init();
			gen.GenerateAsync(game);
		}
		
		public override void Render(double delta) {
			base.Render(delta);
			if (gen.Done) { EndGeneration(); return; }
			
			string state = gen.CurrentState;
			SetProgress(gen.CurrentProgress);
			if (state == lastState) return;
			
			lastState = state;
			SetMessage(state);
		}
		
		void EndGeneration() {
			game.Gui.SetNewScreen(null);
			if (gen.Blocks == null) {
				game.Chat.Add("&cFailed to generate the map.");
			} else {
				game.World.SetNewMap(gen.Blocks, gen.Width, gen.Height, gen.Length);
				gen.Blocks = null;
				ResetPlayerPosition();
				
				Events.RaiseOnNewMapLoaded();
				gen.ApplyEnv(game.World);
			}
			
			gen = null;
			GC.Collect();
		}
		
		void ResetPlayerPosition() {
			float x = (game.World.Width  / 2) + 0.5f;
			float z = (game.World.Length / 2) + 0.5f;
			Vector3 spawn = Respawn.FindSpawnPosition(game, x, z, game.LocalPlayer.Size);
			
			LocationUpdate update = LocationUpdate.MakePosAndOri(spawn, 0, 0, false);
			game.LocalPlayer.SetLocation(update, false);
			game.LocalPlayer.Spawn = spawn;
			game.CurrentCameraPos = game.Camera.GetPosition(0);
		}
	}
}
