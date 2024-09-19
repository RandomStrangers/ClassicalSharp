﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ClassicalSharp.Textures {

	public struct ZipEntry {
		public int CompressedDataSize;
		public int UncompressedDataSize;
		public int LocalHeaderOffset;
		public uint Crc32;
		public string Path;
	}
	
	public delegate void ZipEntryProcessor(string path, byte[] data, ZipEntry entry);	
	public delegate bool ZipEntrySelector(string path);	
	
	/// <summary> Extracts files from a stream that represents a .zip file. </summary>
	public sealed class ZipReader {
		
		public ZipEntryProcessor ProcessZipEntry;
		public ZipEntrySelector SelectZipEntry;
		public ZipEntry[] entries;
		int index;
		
		static Encoding enc = Encoding.ASCII;
		public void Extract(Stream stream) {
			BinaryReader reader = new BinaryReader(stream);
			uint sig = 0;
			
			// At -22 for nearly all zips, but try a bit further back in case of comment
			int len = Math.Min(257, (int)stream.Length);
			for (int i = 22; i < len; i++) {
				stream.Seek(-i, SeekOrigin.End);
				sig = reader.ReadUInt32();
				if (sig == 0x06054b50) break;
			}			
			if (sig != 0x06054b50) {
				Utils.LogDebug("Failed to find end of central directory header");
				return;
			}
			
			int entriesCount, centralDirectoryOffset;
			ReadEndOfCentralDirectory(reader, out entriesCount, out centralDirectoryOffset);
			entries = new ZipEntry[entriesCount];
			reader.BaseStream.Seek(centralDirectoryOffset, SeekOrigin.Begin);
			
			// Read all the central directory entries
			while (true) {
				sig = reader.ReadUInt32();
				if (sig == 0x02014b50) {
					ReadCentralDirectory(reader, entries);
				} else if (sig == 0x06054b50) {
					break;
				} else {
					Utils.LogDebug("Unsupported signature: " + sig.ToString("X8"));
					return;
				}
			}
			
			// Now read the local file header entries
			for (int i = 0; i < entriesCount; i++) {
				ZipEntry entry = entries[i];
				reader.BaseStream.Seek(entry.LocalHeaderOffset, SeekOrigin.Begin);
				sig = reader.ReadUInt32();
				
				if (sig != 0x04034b50) {
					Utils.LogDebug(entry.Path + " is an invalid entry");
					continue;
				}
				ReadLocalFileHeader(reader, entry);
			}
			entries = null;
			index = 0;
		}
		
		void ReadLocalFileHeader(BinaryReader reader, ZipEntry entry) {
			reader.ReadUInt16(); // version needed
			reader.ReadUInt16(); // flags
			ushort compressionMethod = reader.ReadUInt16();
			reader.ReadUInt32(); // last modified
			reader.ReadUInt32(); // CRC32
			
			int compressedSize = reader.ReadInt32();
			if (compressedSize == 0) compressedSize = entry.CompressedDataSize;
			int uncompressedSize = reader.ReadInt32();
			if (uncompressedSize == 0) uncompressedSize = entry.UncompressedDataSize;
			
			ushort pathLen  = reader.ReadUInt16();
			ushort extraLen = reader.ReadUInt16();
			string path = enc.GetString(reader.ReadBytes(pathLen));
			if (SelectZipEntry != null && !SelectZipEntry(path)) return;
			
			reader.ReadBytes(extraLen);
			byte[] data = DecompressEntry(reader, compressionMethod, compressedSize, uncompressedSize);
			if (data != null) ProcessZipEntry(path, data, entry);
		}
		
		void ReadCentralDirectory(BinaryReader reader, ZipEntry[] entries) {
			ZipEntry entry;
			reader.ReadUInt16(); // OS
			reader.ReadUInt16(); // version neede
			reader.ReadUInt16(); // flags
			reader.ReadUInt16(); // compression method
			reader.ReadUInt32(); // last modified
			uint crc32 = reader.ReadUInt32();
			int compressedSize   = reader.ReadInt32();
			int uncompressedSize = reader.ReadInt32();
			ushort pathLen    = reader.ReadUInt16();
			ushort extraLen   = reader.ReadUInt16();			
			ushort commentLen = reader.ReadUInt16();
			
			reader.ReadUInt16(); // disk number
			reader.ReadUInt16(); // internal attributes
			reader.ReadUInt32(); // external attributes
			int localHeaderOffset = reader.ReadInt32();
			string path = enc.GetString(reader.ReadBytes(pathLen));
			reader.ReadBytes(extraLen);
			reader.ReadBytes(commentLen);
			
			entry.CompressedDataSize   = compressedSize;
			entry.UncompressedDataSize = uncompressedSize;
			entry.LocalHeaderOffset = localHeaderOffset;
			entry.Path = path;
			entry.Crc32 = crc32;
			entries[index++] = entry;
		}
		
		void ReadEndOfCentralDirectory(BinaryReader reader, out int entriesCount, out int centralDirectoryOffset) {
			reader.ReadUInt16(); // disk number
			reader.ReadUInt16(); // disk number start
			reader.ReadUInt16(); // disk entries
			entriesCount = reader.ReadUInt16();
			reader.ReadInt32(); // central directory size
			centralDirectoryOffset = reader.ReadInt32();
			reader.ReadUInt16(); // comment length
		}
		
		byte[] DecompressEntry(BinaryReader reader, ushort compressionMethod, int compressedSize, int uncompressedSize) {
			if (compressionMethod == 0) { // Store/Raw
				return reader.ReadBytes(uncompressedSize);
			} else if (compressionMethod == 8) { // Deflate
				byte[] data = new byte[uncompressedSize];
				int index = 0, read = 0;
				
				using (DeflateStream ds = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, true)) {
					while (index < uncompressedSize) {
						read = ds.Read(data, index, data.Length - index);
						if (read == 0) break;
						index += read;
					}
				}
				return data;
			} else {
				Utils.LogDebug("Unsupported .zip entry compression method: " + compressionMethod);
				reader.ReadBytes(compressedSize);
				return null;
			}
		}
	}
}
