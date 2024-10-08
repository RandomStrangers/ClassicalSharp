﻿using System;

namespace SharpWave {
	
	public static class MemUtils {
		
		static MemUtils() {
			use64Bit = IntPtr.Size == 8;
		}
		static bool use64Bit;
		
		public static unsafe void memcpy( IntPtr srcPtr, IntPtr dstPtr, int bytes ) {
			byte* srcByte, dstByte;
			if( use64Bit ) {
				ulong* srcLong = (ulong*)srcPtr, dstLong = (ulong*)dstPtr;
				while( bytes >= 8 ) {
					*dstLong++ = *srcLong++;
					bytes -= 8;
				}
				srcByte = (byte*)srcLong; dstByte = (byte*)dstLong;
			} else {
				uint* srcInt = (uint*)srcPtr, dstInt = (uint*)dstPtr;
				while( bytes >= 4 ) {
					*dstInt++ = *srcInt++;
					bytes -= 4;
				}
				srcByte = (byte*)srcInt; dstByte = (byte*)dstInt;
			}
			
			for( int i = 0; i < bytes; i++ ) {
				*dstByte++ = *srcByte++;
			}
		}
	}
	
	public unsafe static class VolumeMixer {
		
		public static void Mix16( short* samples, int numSamples, int volumePercent ) {
			int numBulkSamples = numSamples & ~0x07;
			
			// Unrolled loop, do 8 samples per iteration
			for( int i = 0; i < numBulkSamples; i += 8 ) {
				samples[0] = (short)(samples[0] * volumePercent / 100);
				samples[1] = (short)(samples[1] * volumePercent / 100);
				samples[2] = (short)(samples[2] * volumePercent / 100);
				samples[3] = (short)(samples[3] * volumePercent / 100);
				
				samples[4] = (short)(samples[4] * volumePercent / 100);
				samples[5] = (short)(samples[5] * volumePercent / 100);
				samples[6] = (short)(samples[6] * volumePercent / 100);
				samples[7] = (short)(samples[7] * volumePercent / 100);
				
				samples += 8;
			}
			
			// Fixup the few last samples
			for( int i = numBulkSamples; i < numSamples; i++ ) {
				samples[0] = (short)(samples[0] * volumePercent / 100);
				samples++;
			}
		}
		
		public static void Mix8( byte* samples, int numSamples, int volumePercent ) {
			for( int i = 0; i < numSamples; i++ ) {
				samples[0] = (byte)(127 + (samples[0] - 127) * volumePercent / 100);
				samples++;
			}
		}
	}
}