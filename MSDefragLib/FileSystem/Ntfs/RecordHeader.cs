using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class RecordHeader
    {
        private RecordHeader()
        {
        }

        public static RecordHeader Parse(BinaryReader reader)
        {
            RecordHeader r = new RecordHeader();
            r.Type = reader.ReadUInt32();
            r.UsaOffset = reader.ReadUInt16();
            r.UsaCount = reader.ReadUInt16();
            r.Lsn = reader.ReadUInt64();
            return r;
        }

        public UInt32 Type;                     /* File type, for example 'FILE' */

        public UInt16 UsaOffset;                /* Offset to the Update Sequence Array */
        public UInt16 UsaCount;                 /* Size in words of Update Sequence Array */

        public UInt64 Lsn;                      /* $LogFile Sequence Number (LSN) */

    }
}
