using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NtfsFileRecordHeader : ISizeHelper
    {
        public NtfsRecordHeader RecHdr;

        public UInt16 SequenceNumber;           /* Sequence number */
        public UInt16 LinkCount;                /* Hard link count */

        public UInt16 AttributeOffset;          /* Offset to the first Attribute */
        public UInt16 Flags;                    /* Flags. bit 1 = in use, bit 2 = directory, bit 4 & 8 = unknown. */

        public UInt32 BytesInUse;               /* Real size of the FILE record */
        public UInt32 BytesAllocated;           /* Allocated size of the FILE record */

        public InodeReference BaseFileRecord;  /* File reference to the base FILE record */

        public UInt16 NextAttributeNumber;      /* Next Attribute Id */
        public UInt16 Padding;                  /* Align to 4 UCHAR boundary (XP) */

        public UInt32 MFTRecordNumber;          /* Number of this MFT Record (XP) */

        public UInt16 UpdateSeqNum;             /*  */

        private NtfsFileRecordHeader()
        {
        }

        public static NtfsFileRecordHeader Parse(BinaryReader reader)
        {
            NtfsFileRecordHeader r = new NtfsFileRecordHeader();
            r.RecHdr = NtfsRecordHeader.Parse(reader);
            r.SequenceNumber = reader.ReadUInt16();
            r.LinkCount = reader.ReadUInt16();
            r.AttributeOffset = reader.ReadUInt16();
            r.Flags = reader.ReadUInt16();
            r.BytesInUse = reader.ReadUInt32();
            r.BytesAllocated = reader.ReadUInt32();
            r.BaseFileRecord = InodeReference.Parse(reader);
            r.NextAttributeNumber = reader.ReadUInt16();
            r.Padding = reader.ReadUInt16();
            r.MFTRecordNumber = reader.ReadUInt32();
            r.UpdateSeqNum = reader.ReadUInt16();
            return r;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return RecHdr.Size + 2 + 2 + 2 + 2 + 4 + 4 + BaseFileRecord.Size + 2 + 2 + 4 + 2; }
        }

        #endregion
    }
}
