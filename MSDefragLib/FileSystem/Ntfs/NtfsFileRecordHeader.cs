using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NtfsFileRecordHeader
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

        public NtfsFileRecordHeader(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            RecHdr = NtfsRecordHeader.Parse(Helper.BinaryReader(buffer, offset));
            offset += RecHdr.Size;

            SequenceNumber = buffer.ToUInt16(ref offset);
            LinkCount = buffer.ToUInt16(ref offset);
            AttributeOffset = buffer.ToUInt16(ref offset);
            Flags = buffer.ToUInt16(ref offset);
            BytesInUse = buffer.ToUInt32(ref offset);
            BytesAllocated = buffer.ToUInt32(ref offset);

            //HACK: remove later
            BaseFileRecord = InodeReference.Parse(Helper.BinaryReader(buffer, offset));
            offset += BaseFileRecord.Size;

            NextAttributeNumber = buffer.ToUInt16(ref offset);
            Padding = buffer.ToUInt16(ref offset);
            MFTRecordNumber = buffer.ToUInt32(ref offset);
            UpdateSeqNum = buffer.ToUInt16(ref offset);
        }
    }
}
