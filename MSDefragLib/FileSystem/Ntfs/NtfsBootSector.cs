using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Class for describing boot sector
    /// </summary>
    class NtfsBootSector
    {
        public UInt16 bytesPerSector;
        public Byte sectorPerCluster;
        public UInt16 reserved;
        public Byte[] reserved2 = new Byte[3];
        public UInt16 reserved3;
        public Byte mediaDescriptor;
        public UInt16 reserved4;
        public UInt16 unused;
        public UInt16 unused2;
        public UInt32 unused3;
        public UInt32 reserved5;
        public UInt32 unused4;
        public UInt64 totalSectors;
        public UInt64 mftLCN;
        public UInt64 mft2LCN;
        public Byte clustersPerMftRecord;
        public Byte[] unused5 = new Byte[3];
        public Byte clustersPerIndexBuffer;
        public Byte[] unused6 = new Byte[3];
        public UInt64 volumeSerialNumber;
        public UInt32 unused7;

        public NtfsBootSector(ByteArray Buffer, ref Int64 offset)
        {
            Parse(Buffer, ref offset);
        }

        public void Parse(ByteArray Buffer, ref Int64 offset)
        {
            bytesPerSector = Buffer.ToUInt16(ref offset);
            sectorPerCluster = Buffer.ToByte(ref offset);
            reserved = Buffer.ToUInt16(ref offset);
            reserved2[0] = Buffer.ToByte(ref offset);
            reserved2[1] = Buffer.ToByte(ref offset);
            reserved2[2] = Buffer.ToByte(ref offset);
            reserved3 = Buffer.ToUInt16(ref offset);
            mediaDescriptor = Buffer.ToByte(ref offset);
            reserved4 = Buffer.ToUInt16(ref offset);
            unused = Buffer.ToUInt16(ref offset);
            unused2 = Buffer.ToUInt16(ref offset);
            unused3 = Buffer.ToUInt32(ref offset);
            reserved5 = Buffer.ToUInt32(ref offset);
            unused4 = Buffer.ToUInt32(ref offset);
            totalSectors = Buffer.ToUInt64(ref offset);
            mftLCN = Buffer.ToUInt64(ref offset);
            mft2LCN = Buffer.ToUInt64(ref offset);
            clustersPerMftRecord = Buffer.ToByte(ref offset);
            unused5[0] = Buffer.ToByte(ref offset);
            unused5[1] = Buffer.ToByte(ref offset);
            unused5[2] = Buffer.ToByte(ref offset);
            clustersPerIndexBuffer = Buffer.ToByte(ref offset);
            unused6[0] = Buffer.ToByte(ref offset);
            unused6[1] = Buffer.ToByte(ref offset);
            unused6[2] = Buffer.ToByte(ref offset);
            volumeSerialNumber = Buffer.ToUInt64(ref offset);
            unused7 = Buffer.ToUInt32(ref offset);
        }
    }
}
