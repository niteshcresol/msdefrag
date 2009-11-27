using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NtfsRecordHeader
    {
        public UInt32 Type;                     /* File type, for example 'FILE' */

        public UInt16 UsaOffset;                /* Offset to the Update Sequence Array */
        public UInt16 UsaCount;                 /* Size in words of Update Sequence Array */

        public UInt64 Lsn;                      /* $LogFile Sequence Number (LSN) */

        public NtfsRecordHeader(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            Type = buffer.ToUInt32(ref offset);
            UsaOffset = buffer.ToUInt16(ref offset);
            UsaCount = buffer.ToUInt16(ref offset);
            Lsn = buffer.ToUInt64(ref offset);
        }
    }
}
