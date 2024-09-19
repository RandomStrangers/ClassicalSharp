﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using OpenTK.Input;
using ClassicalSharp.GraphicsAPI;
#if ANDROID
using Android.Graphics;
#endif

namespace ClassicalSharp.Gui.Widgets {
	
	public delegate void ButtonValueSetter(Game game, string raw);
	public delegate string ButtonValueGetter(Game game);
	
	public class ButtonWidget : Widget {
		public string OptName;
		public ButtonValueGetter GetValue;
		public ButtonValueSetter SetValue;		
		public ButtonWidget(Game game) : base(game) { }
		
		public static ButtonWidget Create(Game game, int width, string text, Font font, ClickHandler onClick) {
			ButtonWidget widget = new ButtonWidget(game);
			widget.MinWidth  = width;
			widget.MenuClick = onClick;
			widget.Set(text, font);			
			return widget;
		}
		
		public ButtonWidget SetLocation(Anchor horAnchor, Anchor verAnchor, int xOffset, int yOffset) {
			HorizontalAnchor = horAnchor; VerticalAnchor = verAnchor;
			XOffset = xOffset; YOffset = yOffset;
			Reposition();
			return this;
		}
		
		Texture texture;
		public int MinWidth;	
		public override void Init() { }
		
		const float uWidth = 200/256f;
		const int minHeight = 40;
		static Texture shadowTex = new Texture(0, 0, 0, 0, 0,
		                                       new TextureRec(0,   66/256f, uWidth, 20/256f));
		static Texture selectedTex = new Texture(0, 0, 0, 0, 0,
		                                         new TextureRec(0, 86/256f, uWidth, 20/256f));
		static Texture disabledTex = new Texture(0, 0, 0, 0, 0,
		                                         new TextureRec(0, 46/256f, uWidth, 20/256f));

		public void Set(string text, Font font) {
			game.Graphics.DeleteTexture(ref texture);
			if (IDrawer2D.EmptyText(text)) {
				texture = default(Texture);
				int height = game.Drawer2D.FontHeight(font, true);
				texture.Height = (ushort)height;
			} else {
				DrawTextArgs args = new DrawTextArgs(text, font, true);
				texture = game.Drawer2D.MakeTextTexture(ref args, 0, 0);
			}
			
			Width  = Math.Max(texture.Width,       MinWidth);
			Height = Math.Max((int)texture.Height, minHeight);
			
			Reposition();		
			texture.X1 = X + (Width  / 2 - texture.Width  / 2);
			texture.Y1 = Y + (Height / 2 - texture.Height / 2);
		}
		
		static PackedCol normCol = new PackedCol(224, 224, 224),
		activeCol = new PackedCol(255, 255, 160),
		disabledCol = new PackedCol(160, 160, 160);
		
		public override void Render(double delta) {
			IGraphicsApi gfx = game.Graphics;
			Texture back = Active ? selectedTex : shadowTex;
			if (Disabled) back = disabledTex;
			
			back.ID = game.UseClassicGui ? game.Gui.GuiClassicTex : game.Gui.GuiTex;
			back.X1 = X; back.Y1 = Y;
			back.Width = (ushort)Width; back.Height = (ushort)Height;
			
			if (Width == 400) {
				// Button can be drawn normally
				back.U1 = 0; back.U2 = uWidth;
				back.Render(gfx);
			} else {
				// Split button down the middle
				float scale = (Width / 400f) * 0.5f;
				gfx.BindTexture(back.ID); // avoid bind twice
				
				back.Width = (ushort)(Width / 2);
				back.U1 = 0; back.U2 = uWidth * scale;
				gfx.Draw2DTexture(ref back, PackedCol.White);
				
				back.X1 += (short)(Width / 2);
				back.U1 = uWidth - uWidth * scale; back.U2 = uWidth;
				gfx.Draw2DTexture(ref back, PackedCol.White);
			}
			
			if (!texture.IsValid) return;
			PackedCol col = Disabled ? disabledCol : (Active ? activeCol : normCol);
			texture.Render(gfx, col);
		}
		
		public override void Dispose() {
			game.Graphics.DeleteTexture(ref texture);
		}
		
		public override void Reposition() {
			int oldX = X, oldY = Y;
			base.Reposition();
			
			texture.X1 += X - oldX;
			texture.Y1 += Y - oldY;
		}
	}
}