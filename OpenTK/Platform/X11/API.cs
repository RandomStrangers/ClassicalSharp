#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Security;
using Window = System.IntPtr;
using VisualID = System.IntPtr;
using KeySym = System.IntPtr;
using Display = System.IntPtr;
using Bool = System.Boolean;
using Status = System.Int32;
using Drawable = System.IntPtr;
using Time = System.IntPtr;
using KeyCode = System.Byte;    // Or maybe ushort?

namespace OpenTK.Platform.X11 {
	
	[SuppressUnmanagedCodeSecurity]
	public static class API {

		[DllImport("libX11")]
		public extern static IntPtr XOpenDisplay(IntPtr display);
		[DllImport("libX11")]
		public extern static int XCloseDisplay(IntPtr display);
		[DllImport("libX11")]
		public extern static int XDefaultScreen(IntPtr display);
		[DllImport("libX11")]
		public static extern IntPtr XDefaultRootWindow(IntPtr display);

		[DllImport("libX11")]
		public extern static IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height, int border_width, int depth, int xclass, IntPtr visual, IntPtr valuemask, ref XSetWindowAttributes attributes);
		[DllImport("libX11")]
		public extern static int XDestroyWindow(IntPtr display, IntPtr window);
		
		[DllImport("libX11")]
		public extern static int XMapWindow(IntPtr display, IntPtr window);
		[DllImport("libX11")]
		public extern static int XUnmapWindow(IntPtr display, IntPtr window);
		[DllImport("libX11")]
		public extern static IntPtr XRootWindow(IntPtr display, int screen_number);

		[DllImport("libX11")]
		public extern static Bool XCheckWindowEvent(Display display, Window w, EventMask event_mask, ref XEvent event_return);
		[DllImport("libX11")]
		public extern static Bool XCheckTypedWindowEvent(Display display, Window w, XEventName event_type, ref XEvent event_return);

		[DllImport("libX11")]
		public extern static int XMoveResizeWindow(IntPtr display, IntPtr window, int x, int y, int width, int height);
		[DllImport("libX11")]
		public extern static int XMoveWindow(IntPtr display, IntPtr w, int x, int y);
		[DllImport("libX11")]
		public extern static int XResizeWindow(IntPtr display, IntPtr window, int width, int height);

		[DllImport("libX11")]
		public extern static int XFlush(IntPtr display);

		[DllImport("libX11")]
		public extern static int XStoreName(IntPtr display, IntPtr window, string window_name);
		[DllImport("libX11")]
		public extern static int XFetchName(IntPtr display, IntPtr window, ref IntPtr window_name);

		[DllImport("libX11")]
		public extern static int XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr event_mask, ref XEvent send_event);
		public static int XSendEvent(IntPtr display, IntPtr window, bool propagate, EventMask event_mask, ref XEvent send_event) {
			return XSendEvent(display, window, propagate, new IntPtr((int)event_mask), ref send_event);
		}
		
		[DllImport("libX11")]
		public extern static bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
		                                        out int root_x, out int root_y, out int win_x, out int win_y, out int keys_buttons);
		[DllImport("libX11")]
		public extern static uint XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y,
		                                       uint src_width, uint src_height, int dest_x, int dest_y);

		[DllImport("libX11")]
		public extern static int XFree(IntPtr data);
		[DllImport("libX11")]
		public static extern void XSync(Display display, bool discard);

		[DllImport("libX11")]
		public extern static int XRaiseWindow(IntPtr display, IntPtr window);

		[DllImport("libX11")]
		public extern static IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

		[DllImport("libX11")]
		public extern static int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);

		[DllImport("libX11")]
		public extern static bool XTranslateCoordinates(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y, out int intdest_x_return, out int dest_y_return, out IntPtr child_return);

		// Colormaps
		[DllImport("libX11")]
		public extern static int XDisplayWidth(IntPtr display, int screen_number);
		[DllImport("libX11")]
		public extern static int XDisplayHeight(IntPtr display, int screen_number);
		[DllImport("libX11")]
		public extern static int XDefaultDepth(IntPtr display, int screen_number);
		
		[DllImport("libX11")]
		public extern static int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, PropertyMode mode, IntPtr[] data, int nelements);
		[DllImport("libX11")]
		public extern static int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, PropertyMode mode, IntPtr data, int nelements);
		[DllImport("libX11")]
		public extern static int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);
		[DllImport("libX11")]
		public extern static int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset, IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format, out IntPtr nitems, out IntPtr bytes_after, ref IntPtr prop);
		
		[DllImport("libX11")]
		public extern static int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);
		[DllImport("libX11")]
		public extern static int XUndefineCursor(IntPtr display, IntPtr window);
		[DllImport("libX11")]
		public extern static int XFreeCursor(IntPtr display, IntPtr cursor);

		// Drawing
		[DllImport("libX11")]
		public extern static IntPtr XCreateGC(IntPtr display, IntPtr window, IntPtr valuemask, IntPtr values);
		[DllImport("libX11")]
		public extern static int XFreeGC(IntPtr display, IntPtr gc);

		[DllImport("libX11")]
		public extern static int XIconifyWindow(IntPtr display, IntPtr window, int screen_number);

		[DllImport("libX11")]
		public extern static IntPtr XCreatePixmapFromBitmapData(IntPtr display, IntPtr drawable, byte[] data, int width, int height, IntPtr fg, IntPtr bg, int depth);
		[DllImport("libX11")]
		public extern static IntPtr XCreatePixmap(IntPtr display, IntPtr d, int width, int height, int depth);
		[DllImport("libX11")]
		public extern static IntPtr XCreatePixmapCursor(IntPtr display, IntPtr source, IntPtr mask,
		                                                ref XColor foregroundCol, ref XColor backgroundCol, int x_hot, int y_hot);
		[DllImport("libX11")]
		public extern static IntPtr XFreePixmap(IntPtr display, IntPtr pixmap);

		[DllImport("libX11")]
		public extern static void XSetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints);
		[DllImport("libX11")]
		public static extern IntPtr XGetWMHints(Display display, Window w); // returns XWMHints*
		[DllImport("libX11")]
		public static extern void XSetWMHints(Display display, Window w, ref XWMHints wmhints);
		[DllImport("libX11")]
		public static extern IntPtr XAllocWMHints();
		
		[DllImport("libX11")]
		public extern static bool XkbSetDetectableAutoRepeat(IntPtr display, bool detectable, out bool supported);

		[DllImport("libX11")]
		public static extern IntPtr XCreateColormap(Display display, Window window, IntPtr visual, int alloc);

		[DllImport("libX11")]
		public static extern int XBitmapBitOrder(Display display);
		[DllImport("libX11")]
		public static extern IntPtr XCreateImage(Display display, IntPtr visual,
		                                         uint depth, ImageFormat format, int offset, IntPtr data, int width, int height,
		                                         int bitmap_pad, int bytes_per_line);
		[DllImport("libX11")]
		public static extern void XPutImage(Display display, IntPtr drawable,
		                                    IntPtr gc, IntPtr image, int src_x, int src_y, int dest_x, int dest_y, int width, int height);

		[DllImport("libX11")]
		public static extern int XLookupString(ref XKeyEvent event_struct, [Out] byte[] buffer_return,
		                                       int bytes_buffer, [Out] KeySym[] keysym_return, IntPtr status_in_out);

		[DllImport("libX11")]
		public static extern int XRefreshKeyboardMapping(ref XMappingEvent event_map);
		
		[DllImport("libX11")]
		public extern static IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);
		[DllImport("libX11")]
		public extern static int XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property, IntPtr requestor, IntPtr time);
		[DllImport("libX11")]
		public extern static int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

		internal static IntPtr CreatePixmapFromImage(Display display, System.Drawing.Bitmap image) {
			int width = image.Width, height = image.Height;

			System.Drawing.Imaging.BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
			                                                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
			                                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			
			IntPtr CopyFromParent = IntPtr.Zero;
			IntPtr ximage = XCreateImage(display, CopyFromParent, 24, ImageFormat.ZPixmap,
			                             0, data.Scan0, width, height, 32, 0);
			IntPtr pixmap = XCreatePixmap(display, XDefaultRootWindow(display),
			                              width, height, 24);
			IntPtr gc = XCreateGC(display, pixmap, IntPtr.Zero, IntPtr.Zero);
			
			XPutImage(display, pixmap, gc, ximage, 0, 0, 0, 0, width, height);
			
			XFreeGC(display, gc);
			image.UnlockBits(data);
			
			return pixmap;
		}
		
		public static IntPtr CreateMaskFromImage(Display display, System.Drawing.Bitmap image) {
			int width = image.Width;
			int height = image.Height;
			int stride = (width + 7) >> 3;
			byte[] mask = new byte[stride * height];
			bool msbfirst = (XBitmapBitOrder(display) == 1); // 1 = MSBFirst
			
			for (int y = 0; y < height; ++y) {
				for (int x = 0; x < width; ++x) {
					byte bit = (byte)(1 << (msbfirst ? (7 - (x & 7)) : (x & 7)));
					int offset = y * stride + (x >> 3);
					
					if (image.GetPixel(x, y).A >= 128)
						mask[offset] |= bit;
				}
			}
			
			return XCreatePixmapFromBitmapData(display, XDefaultRootWindow(display),
			                                   mask, width, height, new IntPtr(1), IntPtr.Zero, 1);
		}

		public static Display DefaultDisplay;
		public static int DefaultScreen;
		public static IntPtr RootWindow;

		static API() {
			DefaultDisplay = API.XOpenDisplay(IntPtr.Zero);
			if (DefaultDisplay == IntPtr.Zero)
				throw new PlatformException("Could not establish connection to the X-Server.");

			DefaultScreen = API.XDefaultScreen(DefaultDisplay);
			RootWindow = API.XRootWindow(DefaultDisplay, DefaultScreen);
			Debug.Print("Display connection: {0}", DefaultDisplay);

			//AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
		}

		static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
			if (DefaultDisplay != IntPtr.Zero) {
				API.XCloseDisplay(DefaultDisplay);
				DefaultDisplay = IntPtr.Zero;
			}
		}

		[DllImport("libX11")]
		public static extern KeySym XLookupKeysym(ref XKeyEvent key_event, int index);
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct XVisualInfo {
		public IntPtr Visual;
		public VisualID VisualID;
		public int Screen;
		public int Depth;
		public XVisualClass Class;
		public long RedMask;
		public long GreenMask;
		public long blueMask;
		public int ColormapSize;
		public int BitsPerRgb;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XRRScreenSize {
		internal int Width, Height;
		internal int MWidth, MHeight;
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XineramaScreenInfo {
		public int ScreenNumber;
		public short X, Y;
		public short Width, Height;
	}

	public enum XVisualClass : int {
		StaticGray = 0,
		GrayScale = 1,
		StaticColor = 2,
		PseudoColor = 3,
		TrueColor = 4,
		DirectColor = 5,
	}
}
