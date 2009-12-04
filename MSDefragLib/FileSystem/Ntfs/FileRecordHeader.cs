using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class FileRecordHeader : ISizeHelper
    {
        public RecordHeader RecHdr
        { get; private set; }

        public UInt16 SequenceNumber            /* Sequence number */
        { get; private set; }

        public UInt16 LinkCount                 /* Hard link count */
        { get; private set; }

        public UInt16 AttributeOffset           /* Offset to the first Attribute */
        { get; private set; }

        public UInt16 Flags                     /* Flags. bit 1 = in use, bit 2 = directory, bit 4 & 8 = unknown. */
        { get; private set; }

        public Boolean IsInUse
        { private set{} get { return ((Flags & 1) == 1); } }

        public Boolean IsDirectory
        { private set{} get { return ((Flags & 2) == 2); } }

        public Boolean IsUnknown
        { private set { } get { return ((Flags & 252) != 0); } }

        public UInt32 BytesInUse                /* Real size of the FILE record */
        { get; private set; }

        public UInt32 BytesAllocated            /* Allocated size of the FILE record */
        { get; private set; }

        public InodeReference BaseFileRecord    /* File reference to the base FILE record */
        { get; private set; }

        public UInt16 NextAttributeNumber       /* Next Attribute Id */
        { get; private set; }

        public UInt16 Padding                   /* Align to 4 UCHAR boundary (XP) */
        { get; private set; }

        public UInt32 MFTRecordNumber           /* Number of this MFT Record (XP) */
        { get; private set; }

        public UInt16 UpdateSeqNum              /*  */
        { get; private set; }

        private FileRecordHeader()
        { }

        public static FileRecordHeader Parse(BinaryReader reader)
        {
            FileRecordHeader r = new FileRecordHeader();
            r.RecHdr = RecordHeader.Parse(reader);
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
