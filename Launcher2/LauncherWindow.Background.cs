﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Textures;
using Launcher.Drawing;

namespace Launcher {

	public sealed partial class LauncherWindow {

        public bool ClassicBackground = false;
        public bool fontPng, terrainPng;

        public void TryLoadTexturePack() {
			fontPng = false; terrainPng = false;
			Options.Load();
			LauncherSkin.LoadFromOptions();
			
			if (Options.Get("nostalgia-classicbg", null) != null) {
				ClassicBackground = Options.GetBool("nostalgia-classicbg", false);
			} else {
				ClassicBackground = Options.GetBool(OptionsKey.ClassicMode, false);
			}
			
			string texPack = Options.Get(OptionsKey.DefaultTexturePack, "default.zip");
			string texPath = Path.Combine("texpacks", texPack);
			
			if (!Platform.FileExists(texPath)) {
				texPath = Path.Combine("texpacks", "default.zip");
			}
			if (!Platform.FileExists(texPath)) return;
			
			ExtractTexturePack(texPath);
			// user selected texture pack is missing some required .png files
			if (!fontPng || !terrainPng) {
				texPath = Path.Combine("texpacks", "default.zip");
				ExtractTexturePack(texPath);
			}
		}

        public void ExtractTexturePack(string relPath) {
			using (Stream fs = Platform.FileOpen(relPath)) {
				ZipReader reader = new ZipReader();
				reader.SelectZipEntry = SelectZipEntry;
				reader.ProcessZipEntry = ProcessZipEntry;
				reader.Extract(fs);
			}
		}

        public bool SelectZipEntry(string path) {
			return 
				Utils.CaselessEq(path, "default.png") ||
				Utils.CaselessEq(path, "terrain.png");
		}

        public void ProcessZipEntry(string path, byte[] data, ZipEntry entry) {
			if (Utils.CaselessEq(path, "default.png")) {
				if (fontPng) return;
				
				Bitmap bmp = Platform.ReadBmp(Drawer, data);
				Drawer.SetFontBitmap(bmp);
				useBitmappedFont = !Options.GetBool(OptionsKey.UseChatFont, false);
				fontPng = true;
			} else if (Utils.CaselessEq(path, "terrain.png")) {
				if (terrainPng) return;
				
				Bitmap bmp = Platform.ReadBmp(Drawer, data);
				MakeClassicTextures(bmp);
				bmp.Dispose();
				terrainPng = true;
			}
		}

        public bool useBitmappedFont;
        public Bitmap terrainBmp;
        public FastBitmap terrainPixels;
        public const int tileSize = 48;

        public void MakeClassicTextures(Bitmap bmp) {
			int elemSize = bmp.Width / 16;
			Size size = new Size(tileSize, tileSize);
			terrainBmp = Platform.CreateBmp(tileSize * 2, tileSize);
			terrainPixels = new FastBitmap(terrainBmp, true, false);
			
			// Precompute the scaled background
			using (FastBitmap src = new FastBitmap(bmp, true, true)) {
				BitmapDrawer.DrawScaled(src, terrainPixels, size,
				                        new Rectangle(2 * elemSize, 0, elemSize, elemSize),
				                        new Rectangle(tileSize, 0, tileSize, tileSize), 128, 64);
				BitmapDrawer.DrawScaled(src, terrainPixels, size,
				                        new Rectangle(1 * elemSize, 0, elemSize, elemSize),
				                        new Rectangle(0, 0, tileSize, tileSize), 96, 96);
			}
		}
		
		public void RedrawBackground() {
			if (Framebuffer == null || (Framebuffer.Width != Width || Framebuffer.Height != Height)) {
				if (Framebuffer != null)
					Framebuffer.Dispose();
				Framebuffer = platformDrawer.CreateFrameBuffer(Width, Height);
			}
			
			if (ClassicBackground && terrainPixels != null) {
				using (FastBitmap bmp = LockBits()) {
					ClearTile(0, 0, Width, 48, tileSize, bmp);
					ClearTile(0, 48, Width, Height - 48, 0, bmp);
				}
			} else {
				ResetArea(0, 0, Width, Height);
			}
			
			DrawTitle();
			Dirty = true;
		}

        public void DrawTitle() {
			using (IDrawer2D drawer = Drawer) {
				drawer.SetBitmap(Framebuffer);

				drawer.UseBitmappedChat = (useBitmappedFont || ClassicBackground) && fontPng;
				DrawTextArgs args = new DrawTextArgs("&eClassical&fSharp", logoFont, false);
				Size size = drawer.MeasureText(ref args);
				int xStart = Width / 2 - size.Width / 2;
				
				args.Text = "&0Classical&0Sharp";
				drawer.DrawText(ref args, xStart + 4, 4);
				args.Text = "&eClassical&fSharp";
				drawer.DrawText(ref args, xStart, 0);
				drawer.UseBitmappedChat = false;
			}
		}
		
		/// <summary> Redraws the specified region with the background pixels. </summary>
		public void ResetArea(int x, int y, int width, int height) {
			using (FastBitmap dst = LockBits())
				ResetArea(x, y, width, height, dst);
		}
		
		/// <summary> Redraws the specified region with the background pixels. </summary>
		public void ResetArea(int x, int y, int width, int height, FastBitmap dst) {
			if (ClassicBackground && terrainPixels != null) {
				ClearTile(x, y, width, height, 0, dst);
			} else {
				PackedCol col = LauncherSkin.BackgroundCol;
				Gradient.Noise(dst, new Rectangle(x, y, width, height), col, 6);
			}
		}
		
		void ClearTile(int x, int y, int width, int height, int srcX, FastBitmap dst) {
			Rectangle srcRect = new Rectangle(srcX, 0, tileSize, tileSize);
			Rectangle dstRect = new Rectangle(x, y, width, height);
			BitmapDrawer.DrawTiled(terrainPixels, dst, srcRect, dstRect);
		}
	}
}
