using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NtfsRecordHeader : ISizeHelper
    {
        public UInt32 Type;                     /* File type, for example 'FILE' */

        public UInt16 UsaOffset;                /* Offset to the Update Sequence Array */
        public UInt16 UsaCount;                 /* Size in words of Update Sequence Array */

        public UInt64 Lsn;                      /* $LogFile Sequence Number (LSN) */

        private NtfsRecordHeader()
        {
        }

        public static NtfsRecordHeader Parse(BinaryReader reader)
        {
            NtfsRecordHeader r = new NtfsRecordHeader();
            r.Type = reader.ReadUInt32();
            r.UsaOffset = reader.ReadUInt16();
            r.UsaCount = reader.ReadUInt16();
            r.Lsn = reader.ReadUInt64();
            return r;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return 4 + 2 + 2 + 8; }
        }

        #endregion
    }
}
