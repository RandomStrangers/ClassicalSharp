﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using ClassicalSharp;

namespace Launcher.Drawing {
	/// <summary> Per-platform class used to transfer a framebuffer directly to the native window. </summary>
	public abstract class PlatformDrawer {	
		internal OpenTK.INativeWindow window;
		
		/// <summary> Initialises the variables for this platform drawer. </summary>
		public abstract void Init();
		
		/// <summary> Creates a framebuffer bitmap of the given dimensions. </summary>
		public virtual Bitmap CreateFrameBuffer(int width, int height) { 
			return Platform.CreateBmp(width, height);
		}
		
		/// <summary> Updates the variables when the native window changes dimensions. </summary>
		public virtual void Resize() { }
		
		/// <summary> Redraws a portion of the framebuffer to the window. </summary>
		/// <remarks> r is only a hint, the entire framebuffer may still be
		/// redrawn on some platforms. </remarks>
		public abstract void Redraw(Bitmap framebuffer, Rectangle r);
	}
}
