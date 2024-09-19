﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ClassicalSharp;
using ClassicalSharp.Textures;

namespace Launcher.Patcher {
	
	public sealed class ZipWriter {
		
		BinaryWriter writer;
		Stream stream;
		DateTime now;
		
		public ZipWriter(Stream stream) {
			this.stream = stream;
			writer = new BinaryWriter(stream);
			now = Utils.LocalNow();
		}
		
		internal ZipEntry[] entries;
		internal int entriesCount;
		
		public void WriteZipEntry(ZipEntry entry, byte[] data) {
			entry.CompressedDataSize = (int)entry.UncompressedDataSize;
			entry.LocalHeaderOffset = (int)stream.Position;
			entries[entriesCount++] = entry;
			WriteLocalFileEntry(entry, data, data.Length);
		}
		
		public void WriteNewImage(Bitmap bmp, string filename) {
			MemoryStream data = new MemoryStream();
			bmp.Save(data, ImageFormat.Png);
			byte[] buffer = data.GetBuffer();
			WriteNewEntry(filename, buffer, (int)data.Length);
		}
		
		public void WriteNewString(string text, string filename) {
			byte[] data = Encoding.ASCII.GetBytes(text);
			WriteNewEntry(filename, data, data.Length);
		}
		
		public void WriteNewEntry(string filename, byte[] data, int dataLength) {
			ZipEntry entry = new ZipEntry();
			entry.UncompressedDataSize = dataLength;
			entry.Crc32 = Utils.CRC32(data, dataLength);
			entry.CompressedDataSize = dataLength;
			entry.LocalHeaderOffset = (int)stream.Position;
			
			entry.Path = filename;
			entries[entriesCount++] = entry;
			WriteLocalFileEntry(entry, data, dataLength);
		}
		
		public void WriteCentralDirectoryRecords() {
			int dirOffset = (int)stream.Position;
			for (int i = 0; i < entriesCount; i++) {
				WriteCentralDirectoryHeaderEntry(entries[i]);
			}
			int dirSize = (int)(stream.Position - dirOffset);
			WriteEndOfCentralDirectoryRecord((ushort)entriesCount, dirSize, dirOffset);
		}
		
		void WriteLocalFileEntry(ZipEntry entry, byte[] data, int length) {
			writer.Write(0x04034b50); // signature
			writer.Write((ushort)20); // version needed
			writer.Write((ushort)0);  // bitflags
			writer.Write((ushort)0);  // compression method
			WriteCurrentDate(writer); // last modified
			writer.Write(entry.Crc32);
			writer.Write(entry.CompressedDataSize);
			writer.Write(entry.UncompressedDataSize);
			writer.Write((ushort)entry.Path.Length);
			writer.Write((ushort)0);  // extra field length
			for (int i = 0; i < entry.Path.Length; i++)
				writer.Write((byte)entry.Path[i]);
			
			writer.Write(data, 0, length);
		}
		
		void WriteCentralDirectoryHeaderEntry(ZipEntry entry) {
			writer.Write(0x02014b50); // signature
			writer.Write((ushort)20); // version		
			writer.Write((ushort)20); // version needed
			writer.Write((ushort)0);  // bitflags
			writer.Write((ushort)0);  // compression method
			WriteCurrentDate(writer); // last modified
			writer.Write(entry.Crc32);
			writer.Write(entry.CompressedDataSize);
			writer.Write(entry.UncompressedDataSize);
			
			writer.Write((ushort)entry.Path.Length);
			writer.Write((ushort)0);  // extra field length
			writer.Write((ushort)0);  // file comment length
			writer.Write((ushort)0);  // disk number
			writer.Write((ushort)0);  // internal attributes
			writer.Write(0);          // external attributes
			writer.Write(entry.LocalHeaderOffset);
			for (int i = 0; i < entry.Path.Length; i++)
				writer.Write((byte)entry.Path[i]);
		}
		
		void WriteCurrentDate(BinaryWriter writer) {
			int modTime = (now.Second / 2) | (now.Minute << 5) | (now.Hour << 11);
			int modDate = (now.Day) | (now.Month << 5) | ((now.Year - 1980) << 9);
			
			writer.Write((ushort)modTime);
			writer.Write((ushort)modDate);
		}
		
		void WriteEndOfCentralDirectoryRecord(ushort entries, int centralDirSize, int centralDirOffset) {
			writer.Write(0x06054b50); // signature
			writer.Write((ushort)0);  // disk number	
			writer.Write((ushort)0);  // disk number of start
			writer.Write(entries);    // disk entries
			writer.Write(entries);    // total entries
			writer.Write(centralDirSize);
			writer.Write(centralDirOffset);
			writer.Write((ushort)0);  // comment length
		}
	}
}
