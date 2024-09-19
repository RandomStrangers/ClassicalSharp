﻿#region --- License ---
/* Copyright (c) 2006, 2007 Stefanos Apostolopoulos
 * Contributions from Erik Ylvisaker
 * See license.txt for license info
 */
#endregion

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace OpenTK.Platform.Windows {
	
	[SuppressUnmanagedCodeSecurity]
	internal static class API {
		
		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);
		
		[DllImport("user32.dll")]
		internal static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int cx, int cy, SetWindowPosFlags flags);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AdjustWindowRect([In, Out] ref Win32Rectangle lpRect, WindowStyle dwStyle, bool bMenu);
		
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern IntPtr CreateWindowEx(int exStyle, IntPtr ClassAtom, IntPtr WindowName, WindowStyle Style,
		                                             int X, int Y, int Width, int Height, IntPtr HandleToParentWindow, IntPtr Menu, IntPtr Instance, IntPtr Param);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyWindow(IntPtr windowHandle);
		
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern ushort RegisterClassEx(ref ExtendedWindowClass window_class);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern short UnregisterClass(IntPtr className, IntPtr instance);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int SetWindowLong(IntPtr hWnd, GetWindowLong nIndex, int dwNewLong);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint GetWindowLong(IntPtr hWnd, GetWindowLong nIndex);
		
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool BringWindowToTop(IntPtr hWnd);
		
		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(IntPtr intPtr);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern bool PeekMessage(ref MSG msg, IntPtr hWnd, int messageFilterMin, int messageFilterMax, int flags);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern bool PostMessage(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr DispatchMessage(ref MSG msg);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern bool TranslateMessage(ref MSG lpMsg);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr DefWindowProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);
		
		[DllImport("user32.dll")]
		internal static extern IntPtr GetDC(IntPtr hwnd);
		[DllImport("user32.dll")]
		internal static extern bool ReleaseDC(IntPtr hwnd, IntPtr DC);

		[DllImport("gdi32.dll")]
		internal static extern int ChoosePixelFormat(IntPtr dc, ref PixelFormatDescriptor pfd);
		[DllImport("gdi32.dll")]
		internal static extern int DescribePixelFormat(IntPtr deviceContext, int pixel, int pfdSize, ref PixelFormatDescriptor pixelFormat);
		[DllImport("gdi32.dll", SetLastError = true)]
		internal static extern bool SetPixelFormat(IntPtr dc, int format, ref PixelFormatDescriptor pfd);
		[DllImport("gdi32.dll", SetLastError = true)]
		internal static extern bool SwapBuffers(IntPtr dc);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr LoadLibrary(string dllName);
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("user32.dll", SetLastError = true)]
		//internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT point);
		internal static extern bool ScreenToClient(IntPtr hWnd, ref Point point);
		[DllImport("user32.dll", SetLastError = true)]
		//internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
		internal static extern bool ClientToScreen(IntPtr hWnd, ref Point point);
		[DllImport("user32.dll", SetLastError = true)]
		internal extern static bool GetClientRect(IntPtr windowHandle, out Win32Rectangle clientRectangle);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetSystemMetrics(int nIndex);
		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern int GetDeviceCaps(IntPtr hDc, int nIndex);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool OpenClipboard(IntPtr hWndNewOwner);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool EmptyClipboard();
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool CloseClipboard();
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetClipboardData(uint uFormat);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
		
		[DllImport("kernel32.dll")]
		public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
		[DllImport("kernel32.dll")]
		public static extern IntPtr GlobalFree(IntPtr hMem);
		[DllImport("kernel32.dll")]
		public static extern IntPtr GlobalLock(IntPtr hMem);
		[DllImport("kernel32.dll")]
		public static extern bool GlobalUnlock(IntPtr hMem);

		[DllImport("user32.dll")]
		public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
		[DllImport("user32.dll")]
		internal static extern bool GetCursorPos(ref POINT point);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetCursorPos(int x, int y);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ShowCursor(int value);
		
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern ushort GetKeyState(int code);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint MapVirtualKey(short vkey, MapVirtualKeyType uMapType);
	}
	
	#pragma warning disable 0649
	internal struct CreateStruct {
		internal IntPtr lpCreateParams;
		internal IntPtr hInstance;
		internal IntPtr hMenu;
		internal IntPtr hwndParent;
		internal int cy;
		internal int cx;
		internal int y;
		internal int x;
		internal int style;
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string lpszName;
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string lpszClass;
		internal uint dwExStyle;
	}

	struct StyleStruct {
		public WindowStyle Old;
		public WindowStyle New;
	}
	#pragma warning restore 0649
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct PixelFormatDescriptor {
		internal short Size;
		internal short Version;
		internal PixelFormatDescriptorFlags Flags;
		internal PixelType PixelType;
		internal byte ColorBits;
		internal byte RedBits;
		internal byte RedShift;
		internal byte GreenBits;
		internal byte GreenShift;
		internal byte BlueBits;
		internal byte BlueShift;
		internal byte AlphaBits;
		internal byte AlphaShift;
		internal byte AccumBits;
		internal byte AccumRedBits;
		internal byte AccumGreenBits;
		internal byte AccumBlueBits;
		internal byte AccumAlphaBits;
		internal byte DepthBits;
		internal byte StencilBits;
		internal byte AuxBuffers;
		internal byte LayerType;
		byte Reserved;
		internal int LayerMask;
		internal int VisibleMask;
		internal int DamageMask;
		
		public const short DefaultVersion = 1;
		public const short DefaultSize = 40;
	}
	
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal class DeviceMode {
		internal DeviceMode() {
			Size = (short)Marshal.SizeOf(this);
		}

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string DeviceName;
		internal short SpecVersion;
		internal short DriverVersion;
		short Size;
		internal short DriverExtra;
		internal int Fields;

		internal POINT Position;
		internal uint DisplayOrientation;
		internal uint DisplayFixedOutput;

		internal short Color;
		internal short Duplex;
		internal short YResolution;
		internal short TTOption;
		internal short Collate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string FormName;
		internal short LogPixels;
		internal int BitsPerPel;
		internal int PelsWidth;
		internal int PelsHeight;
		internal int DisplayFlags;
		internal int DisplayFrequency;
		internal int ICMMethod;
		internal int ICMIntent;
		internal int MediaType;
		internal int DitherType;
		internal int Reserved1;
		internal int Reserved2;
		internal int PanningWidth;
		internal int PanningHeight;
	}
	
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal class WindowsDisplayDevice {
		
		internal WindowsDisplayDevice() {
			size = Marshal.SizeOf(this);
		}
		readonly int size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string DeviceName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		internal string DeviceString;
		internal DisplayDeviceStateFlags StateFlags;    // uint
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		internal string DeviceID;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		internal string DeviceKey;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct WindowClass {
		internal ClassStyle Style;
		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal WindowProcedure WindowProcedure;
		internal int ClassExtraBytes;
		internal int WindowExtraBytes;
		//[MarshalAs(UnmanagedType.
		internal IntPtr Instance;
		internal IntPtr Icon;
		internal IntPtr Cursor;
		internal IntPtr BackgroundBrush;
		//[MarshalAs(UnmanagedType.LPStr)]
		internal IntPtr MenuName;
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string ClassName;

		internal static int SizeInBytes = Marshal.SizeOf(default(WindowClass));
	}
	
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	struct ExtendedWindowClass
	{
		public uint Size;
		public ClassStyle Style;
		[MarshalAs(UnmanagedType.FunctionPtr)]
		public WindowProcedure WndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr Instance;
		public IntPtr Icon;
		public IntPtr Cursor;
		public IntPtr Background;
		public IntPtr MenuName;
		public IntPtr ClassName;
		public IntPtr IconSm;

		public static uint SizeInBytes = (uint)Marshal.SizeOf(default(ExtendedWindowClass));
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct WindowPosition
	{
		internal IntPtr hwnd;
		internal IntPtr hwndInsertAfter;
		internal int x;
		internal int y;
		internal int cx;
		internal int cy;
		[MarshalAs(UnmanagedType.U4)]
		internal SetWindowPosFlags flags;
	}
	
	[Flags]
	internal enum SetWindowPosFlags : int
	{
		NOSIZE          = 0x0001,
		NOMOVE          = 0x0002,
		NOZORDER        = 0x0004,
		NOREDRAW        = 0x0008,
		NOACTIVATE      = 0x0010,
		FRAMECHANGED    = 0x0020, /* The frame changed: send WM_NCCALCSIZE */
		SHOWWINDOW      = 0x0040,
		HIDEWINDOW      = 0x0080,
		NOCOPYBITS      = 0x0100,
		NOOWNERZORDER   = 0x0200, /* Don't do owner Z ordering */
		NOSENDCHANGING  = 0x0400, /* Don't send WM_WINDOWPOSCHANGING */

		DRAWFRAME       = FRAMECHANGED,
		NOREPOSITION    = NOOWNERZORDER,

		DEFERERASE      = 0x2000,
		ASYNCWINDOWPOS  = 0x4000
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Win32Rectangle {
		
		internal Win32Rectangle(int width, int height) {
			left = 0; top = 0;
			right = width;
			bottom = height;
		}

		internal int left, top, right, bottom;
		internal int Width { get { return right - left; } }
		internal int Height { get { return bottom - top; } }

		internal static Win32Rectangle From(Rectangle value) {
			Win32Rectangle rect = new Win32Rectangle();
			rect.left = value.Left;
			rect.right = value.Right;
			rect.top = value.Top;
			rect.bottom = value.Bottom;
			return rect;
		}
	}

	internal enum SizeMessage {
		MAXHIDE = 4,
		MAXIMIZED = 2,
		MAXSHOW = 3,
		MINIMIZED = 1,
		RESTORED = 0
	}
	
	internal enum DisplayModeSettings {
		Current = -1,
		Registry = -2
	}

	[Flags]
	internal enum DisplayDeviceStateFlags {
		AttachedToDesktop = 0x00000001,
		PrimaryDevice     = 0x00000004,
	}

	[Flags]
	internal enum WindowStyle : uint {
		Overlapped = 0x00000000,
		Popup = 0x80000000,
		ClipSiblings = 0x04000000,
		ClipChildren = 0x02000000,
		Maximize = 0x01000000,
		Caption = 0x00C00000,    // Border | DialogFrame
		SystemMenu = 0x00080000,
		ThickFrame = 0x00040000,

		MinimizeBox = 0x00020000,
		MaximizeBox = 0x00010000,

		// Common window styles:
		OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
	}
	
	internal enum GetWindowLong : int {
		STYLE = (-16),
	}
	
	[Flags]
	internal enum PixelFormatDescriptorFlags : int {
		DOUBLEBUFFER = 0x01,
		DRAW_TO_WINDOW = 0x04,
		SUPPORT_OPENGL = 0x20,
		DEPTH_DONTCARE = 0x20000000,
	}
	
	internal enum PixelType : byte {
		RGBA = 0,
		INDEXED = 1
	}
	
	[Flags]
	internal enum ClassStyle { OwnDC = 0x0020, }
	
	internal enum WindowMessage : uint {
		NULL = 0x0000,
		CREATE = 0x0001,
		DESTROY = 0x0002,
		MOVE = 0x0003,
		SIZE = 0x0005,
		ACTIVATE = 0x0006,
		KILLFOCUS = 0x0008,
		CLOSE = 0x0010,
		ERASEBKGND = 0x0014,
		SHOWWINDOW = 0x0018,
		WINDOWPOSCHANGED = 0x0047,
		STYLECHANGED = 0x007D,
		SETICON = 0x0080,
		KEYDOWN = 0x0100,
		KEYFIRST = 0x0100,
		KEYUP = 0x0101,
		CHAR = 0x0102,
		SYSKEYDOWN = 0x0104,
		SYSKEYUP = 0x0105,
		SYSCHAR = 0x0106,
		MOUSEMOVE = 0x0200,
		LBUTTONDOWN = 0x0201,
		LBUTTONUP = 0x0202,
		RBUTTONDOWN = 0x0204,
		RBUTTONUP = 0x0205,
		MBUTTONDOWN = 0x0207,
		MBUTTONUP = 0x0208,
		MOUSEWHEEL = 0x020A,
		XBUTTONDOWN = 0x020B,
		XBUTTONUP = 0x020C,
		ENTERMENULOOP = 0x0211,
		EXITMENULOOP = 0x0212,
		ENTERSIZEMOVE = 0x0231,
		EXITSIZEMOVE = 0x0232,
		MOUSEHOVER = 0x02A1,
		MOUSELEAVE = 0x02A3,
	}
	
	internal enum ShowWindowCommand {
		HIDE            = 0,
		SHOWNORMAL      = 1,
		NORMAL          = 1,
		SHOWMINIMIZED   = 2,
		SHOWMAXIMIZED   = 3,
		MAXIMIZE        = 3,
		SHOWNOACTIVATE  = 4,
		SHOW            = 5,
		MINIMIZE        = 6,
		SHOWMINNOACTIVE = 7,
		SHOWNA          = 8,
		RESTORE         = 9,
		SHOWDEFAULT     = 10,
		FORCEMINIMIZE   = 11,
		//MAX             = 11,

		// Old ShowWindow() Commands
		//HIDE_WINDOW        = 0,
		//SHOW_OPENWINDOW    = 1,
		//SHOW_ICONWINDOW    = 2,
		//SHOW_FULLSCREEN    = 3,
		//SHOW_OPENNOACTIVATE= 4,
	}
	
	internal enum MapVirtualKeyType {
		VirtualKeyToScanCode = 0,
		ScanCodeToVirtualKey = 1,
		VirtualKeyToCharacter = 2,
		ScanCodeToVirtualKeyExtended = 3,
		VirtualKeyToScanCodeExtended = 4,
	}

	internal delegate IntPtr WindowProcedure(IntPtr handle, WindowMessage message, IntPtr wParam, IntPtr lParam);
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct MSG {
		internal IntPtr HWnd;
		internal WindowMessage Message;
		internal IntPtr WParam;
		internal IntPtr LParam;
		internal uint Time;
		internal POINT Point;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT {
		internal int X;
		internal int Y;

		internal POINT(int x, int y) {
			this.X = x;
			this.Y = y;
		}

		internal Point ToPoint() {
			return new Point(X, Y);
		}
	}
}