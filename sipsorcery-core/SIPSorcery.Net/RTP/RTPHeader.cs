//-----------------------------------------------------------------------------
// Filename: RTPHeader.cs
//
// Description: RTP Header as defined in RFC3550.
// 
//
// RTP Header:
// 0                   1                   2                   3
// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |V=2|P|X|  CC   |M|     PT      |       sequence number         |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |                           timestamp                           |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |           synchronization source (SSRC) identifier            |
// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
// |            contributing source (CSRC) identifiers             |
// |                             ....                              |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//
// (V)ersion (2 bits) = 2
// (P)adding (1 bit) = Inidcates whether the packet contains additional padding octets.
// e(X)tension (1 bit) = If set he fixed header must be followed by exactly one header extension.
// CSRC Count (CC) (4 bits) = Number of Contributing Source identifiers following fixed header.
// (M)arker (1 bit) = Used by profiles to enable marks to be set in the data.
// Payload Type (PT) (7 bits) = RTP payload type.
//  - GSM: 000 0011 (3)
//  - PCMU (G711): 000 0000 (0)
// Sequence Number (16 bits) = Increments by one for each RTP packet sent, initial value random.
// Timestamp (32 bits) = The sampling instant of the first bit in the RTP data packet.
// Synchronisation Source Id (SSRC) (32 bits) = The unique synchrosnisation source for the data stream.
// Contributing Source Identifier Ids (0 to 15 items 32 bits each, see CC) = List of contributing sources for the payload in this field.
//
//
// Wallclock time (absolute date and time) is represented using the
// timestamp format of the Network Time Protocol (NTP), which is in
// seconds relative to 0h UTC on 1 January 1900 [4].  The full
// resolution NTP timestamp is a 64-bit unsigned fixed-point number with
// the integer part in the first 32 bits and the fractional part in the
// last 32 bits.  In some fields where a more compact representation is
// appropriate, only the middle 32 bits are used; that is, the low 16
// bits of the integer part and the high 16 bits of the fractional part.
// The high 16 bits of the integer part must be determined
// independently.
//
// History:
// 22 May 2005	Aaron Clauson	Created.
//
// License: 
// Aaron Clauson
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Net;
using SIPSorcery.Sys;

#if UNITTEST
using NUnit.Framework;
#endif

namespace SIPSorcery.Net
{
	public class RTPHeader
	{
		public const int MIN_HEADER_LEN = 12;

        //public readonly static DateTime ZeroTime = new DateTime(2007, 1, 1).ToUniversalTime();

		public const int RTP_VERSION = 2;

		public int Version = RTP_VERSION;						// 2 bits.
		public int PaddingFlag = 0;								// 1 bit.
		public int HeaderExtensionFlag = 0;						// 1 bit.
		public int CSRCCount = 0;								// 4 bits
		public int MarkerBit = 0;								// 1 bit.
		public int PayloadType = (int)RTPPayloadTypesEnum.PCMU;	// 7 bits.
		public UInt16 SequenceNumber;							// 16 bits.
		public uint Timestamp;									// 32 bits.
		public uint SyncSource;									// 32 bits.
		public int[] CSRCList;									// 32 bits.

		public RTPHeader()
		{
			Random rnd = new Random(DateTime.Now.Millisecond);
			
			int randomStart = 1;
			int randomEnd = UInt16.MaxValue;
			
			// Generate a random value for the sequence number.
			//SequenceNumber = Convert.ToUInt16(rnd.Next(randomStart, randomEnd));
			SequenceNumber = 0;

			randomEnd = int.MaxValue;

			// Generate a random value for the sync source.
			SyncSource = (uint)rnd.Next(randomStart, randomEnd) + (uint)rnd.Next(randomStart, randomEnd);

			// Generate a random value for the timestamp.
			//Timestamp = (uint)rnd.Next(randomStart, randomEnd) + (uint)rnd.Next(randomStart, randomEnd);
		}

		/// <summary>
		/// Extract and load the RTP header from an RTP packet.
		/// </summary>
		/// <param name="packet"></param>
		public RTPHeader(byte[] packet)
		{
			if(packet.Length < MIN_HEADER_LEN)
			{
				throw new ApplicationException("The packet did not contain the minimum number of bytes for an RTP header packet.");
			}

			UInt16 firstWord = BitConverter.ToUInt16(packet, 0);

			if(BitConverter.IsLittleEndian)
			{
				firstWord = NetConvert.DoReverseEndian(firstWord);
				SequenceNumber = NetConvert.DoReverseEndian(BitConverter.ToUInt16(packet, 2));
				Timestamp = NetConvert.DoReverseEndian(BitConverter.ToUInt32(packet,4));
				SyncSource = NetConvert.DoReverseEndian(BitConverter.ToUInt32(packet,8));
			}
			else
			{
				SequenceNumber = BitConverter.ToUInt16(packet, 2);
				Timestamp = BitConverter.ToUInt32(packet,4);
				SyncSource = BitConverter.ToUInt32(packet,8);
			}

			Version = firstWord >> 14;
			PaddingFlag = (firstWord >> 13) & 0x1;
			HeaderExtensionFlag = (firstWord >> 12) & 0x1;
			CSRCCount = firstWord & 0xf;
			MarkerBit = firstWord >> 9 & 0x1;
			PayloadType = firstWord & 0x7f;
		}

		public byte[] GetHeader(UInt16 sequenceNumber, uint timestamp, uint syncSource)
		{
			SequenceNumber = sequenceNumber;
			Timestamp = timestamp;
			SyncSource = syncSource;

			return GetBytes();
		}


		public byte[] GetBytes()
		{
			byte[] header = new byte[12];

			UInt16 firstWord = Convert.ToUInt16(Version * 16384 + PaddingFlag * 8192 + HeaderExtensionFlag * 4096 + CSRCCount * 256 + MarkerBit * 128 + PayloadType);

			if(BitConverter.IsLittleEndian)
			{
				Buffer.BlockCopy(BitConverter.GetBytes(NetConvert.DoReverseEndian(firstWord)), 0, header, 0, 2);
				Buffer.BlockCopy(BitConverter.GetBytes(NetConvert.DoReverseEndian(SequenceNumber)), 0, header, 2, 2);
				Buffer.BlockCopy(BitConverter.GetBytes(NetConvert.DoReverseEndian(Timestamp)), 0, header, 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(NetConvert.DoReverseEndian(SyncSource)), 0, header, 8, 4);	
			}
			else
			{
				Buffer.BlockCopy(BitConverter.GetBytes(firstWord), 0, header, 0, 2);
				Buffer.BlockCopy(BitConverter.GetBytes(SequenceNumber), 0, header, 2, 2);
				Buffer.BlockCopy(BitConverter.GetBytes(Timestamp), 0, header, 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(SyncSource), 0, header, 8, 4);
			}

			return header;
		}

		
		/*public static uint GetWallclockUTCStamp(DateTime time)
		{
			return Convert.ToUInt32(time.ToUniversalTime().Subtract(DateTime.Now.Date).TotalMilliseconds);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns>The timestamp as a UTC DateTime object.</returns>
		public static DateTime GetWallclockUTCTimeFromStamp(uint timestamp)
		{
            return DateTime.Now.Date.AddMilliseconds(timestamp);
		}*/


		#region Unit testing.

		#if UNITTEST
	
		[TestFixture]
		public class RTPHeaderUnitTest
		{
			[TestFixtureSetUp]
			public void Init()
			{
				
			}

			[TestFixtureTearDown]
			public void Dispose()
			{			
				
			}

			[Test]
			public void SampleTest()
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
				
				Assert.IsTrue(true, "True was false.");
			}

			[Test]
			public void GetHeaderTest()
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

				RTPHeader rtpHeader = new RTPHeader();
				byte[] headerBuffer = rtpHeader.GetHeader(1, 0, 1);

				int byteNum = 1;
				foreach(byte headerByte in headerBuffer)
				{
					Console.WriteLine(byteNum + ": " + headerByte.ToString("x"));
					byteNum++;
				}
			}

			[Test]
			public void HeaderRoundTripTest()
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

				RTPHeader src = new RTPHeader();
				byte[] headerBuffer = src.GetHeader(1, 0, 1);
				RTPHeader dst = new RTPHeader(headerBuffer);

				Console.WriteLine("Versions: " + src.Version + ", " + dst.Version);
				Console.WriteLine("PaddingFlag: " + src.PaddingFlag + ", " + dst.PaddingFlag);
				Console.WriteLine("HeaderExtensionFlag: " + src.HeaderExtensionFlag + ", " + dst.HeaderExtensionFlag);
				Console.WriteLine("CSRCCount: " + src.CSRCCount + ", " + dst.CSRCCount);
				Console.WriteLine("MarkerBit: " + src.MarkerBit + ", " + dst.MarkerBit);
				Console.WriteLine("PayloadType: " + src.PayloadType + ", " + dst.PayloadType);
				Console.WriteLine("SequenceNumber: " + src.SequenceNumber + ", " + dst.SequenceNumber);
				Console.WriteLine("Timestamp: " + src.Timestamp + ", " + dst.Timestamp);
				Console.WriteLine("SyncSource: " + src.SyncSource + ", " + dst.SyncSource);

				Console.WriteLine("Raw Header: " + System.Text.Encoding.ASCII.GetString(headerBuffer, 0, headerBuffer.Length));

				Assert.IsTrue(src.Version == dst.Version, "Version was mismatched.");
				Assert.IsTrue(src.PaddingFlag == dst.PaddingFlag, "PaddingFlag was mismatched.");
				Assert.IsTrue(src.HeaderExtensionFlag == dst.HeaderExtensionFlag, "HeaderExtensionFlag was mismatched.");
				Assert.IsTrue(src.CSRCCount == dst.CSRCCount, "CSRCCount was mismatched.");
				Assert.IsTrue(src.MarkerBit == dst.MarkerBit, "MarkerBit was mismatched.");
				Assert.IsTrue(src.SequenceNumber == dst.SequenceNumber, "PayloadType was mismatched.");
				Assert.IsTrue(src.HeaderExtensionFlag == dst.HeaderExtensionFlag, "SequenceNumber was mismatched.");
				Assert.IsTrue(src.Timestamp == dst.Timestamp, "Timestamp was mismatched.");
				Assert.IsTrue(src.SyncSource == dst.SyncSource, "SyncSource was mismatched.");
			}

			[Test]
			public void CustomisedHeaderRoundTripTest()
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

				RTPHeader src = new RTPHeader();
				src.Version = 3;
				src.PaddingFlag = 1;
				src.HeaderExtensionFlag = 1;
				src.MarkerBit = 1;
				src.CSRCCount = 3;
				src.PayloadType = (int)RTPPayloadTypesEnum.GSM;

				byte[] headerBuffer = src.GetHeader(1, 0, 1);

				RTPHeader dst = new RTPHeader(headerBuffer);

				Console.WriteLine("Versions: " + src.Version + ", " + dst.Version);
				Console.WriteLine("PaddingFlag: " + src.PaddingFlag + ", " + dst.PaddingFlag);
				Console.WriteLine("HeaderExtensionFlag: " + src.HeaderExtensionFlag + ", " + dst.HeaderExtensionFlag);
				Console.WriteLine("CSRCCount: " + src.CSRCCount + ", " + dst.CSRCCount);
				Console.WriteLine("MarkerBit: " + src.MarkerBit + ", " + dst.MarkerBit);
				Console.WriteLine("PayloadType: " + src.PayloadType + ", " + dst.PayloadType);
				Console.WriteLine("SequenceNumber: " + src.SequenceNumber + ", " + dst.SequenceNumber);
				Console.WriteLine("Timestamp: " + src.Timestamp + ", " + dst.Timestamp);
				Console.WriteLine("SyncSource: " + src.SyncSource + ", " + dst.SyncSource);

				string rawHeader = null;
				foreach(byte headerByte in headerBuffer)
				{
					rawHeader += headerByte.ToString("x");
				}

				Console.WriteLine("Raw Header: " + rawHeader);

				Assert.IsTrue(src.Version == dst.Version, "Version was mismatched.");
				Assert.IsTrue(src.PaddingFlag == dst.PaddingFlag, "PaddingFlag was mismatched.");
				Assert.IsTrue(src.HeaderExtensionFlag == dst.HeaderExtensionFlag, "HeaderExtensionFlag was mismatched.");
				Assert.IsTrue(src.CSRCCount == dst.CSRCCount, "CSRCCount was mismatched.");
				Assert.IsTrue(src.MarkerBit == dst.MarkerBit, "MarkerBit was mismatched.");
				Assert.IsTrue(src.SequenceNumber == dst.SequenceNumber, "PayloadType was mismatched.");
				Assert.IsTrue(src.HeaderExtensionFlag == dst.HeaderExtensionFlag, "SequenceNumber was mismatched.");
				Assert.IsTrue(src.Timestamp == dst.Timestamp, "Timestamp was mismatched.");
				Assert.IsTrue(src.SyncSource == dst.SyncSource, "SyncSource was mismatched.");
			}
		}

		#endif

		#endregion
	}
}