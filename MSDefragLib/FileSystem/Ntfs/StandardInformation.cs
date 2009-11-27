using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class StandardInformation
    {
        public UInt64 CreationTime;
        public UInt64 FileChangeTime;
        public UInt64 MftChangeTime;
        public UInt64 LastAccessTime;
        public UInt32 FileAttributes;                  /* READ_ONLY=0x01, HIDDEN=0x02, SYSTEM=0x04, VOLUME_ID=0x08, ARCHIVE=0x20, DEVICE=0x40 */
        public UInt32 MaximumVersions;
        public UInt32 VersionNumber;
        public UInt32 ClassId;
        public UInt32 OwnerId;                         // NTFS 3.0 only
        public UInt32 SecurityId;                      // NTFS 3.0 only
        public UInt64 QuotaCharge;                     // NTFS 3.0 only
        public UInt64 Usn;                             // NTFS 3.0 only

        public StandardInformation(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            CreationTime = buffer.ToUInt64(ref offset);
            FileChangeTime = buffer.ToUInt64(ref offset);
            MftChangeTime = buffer.ToUInt64(ref offset);
            LastAccessTime = buffer.ToUInt64(ref offset);
            FileAttributes = buffer.ToUInt32(ref offset);
            MaximumVersions = buffer.ToUInt32(ref offset);
            VersionNumber = buffer.ToUInt32(ref offset);
            ClassId = buffer.ToUInt32(ref offset);
            OwnerId = buffer.ToUInt32(ref offset);
            SecurityId = buffer.ToUInt32(ref offset);
            QuotaCharge = buffer.ToUInt64(ref offset);
            Usn = buffer.ToUInt64(ref offset);
        }
    }
}
