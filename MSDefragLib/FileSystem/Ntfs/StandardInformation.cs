using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class StandardInformation
    {
        private StandardInformation()
        {
        }

        public static StandardInformation Parse(BinaryReader reader)
        {
            StandardInformation s = new StandardInformation();
            s.CreationTime = reader.ReadUInt64();
            s.FileChangeTime = reader.ReadUInt64();
            s.MftChangeTime = reader.ReadUInt64();
            s.LastAccessTime = reader.ReadUInt64();
            s.FileAttributes = reader.ReadUInt32();
            s.MaximumVersions = reader.ReadUInt32();
            s.VersionNumber = reader.ReadUInt32();
            s.ClassId = reader.ReadUInt32();
            s.OwnerId = reader.ReadUInt32();
            s.SecurityId = reader.ReadUInt32();
            s.QuotaCharge = reader.ReadUInt64();
            s.Usn = reader.ReadUInt64();
            return s;
        }

        public UInt64 CreationTime
        { get; private set; }

        public UInt64 FileChangeTime
        { get; private set; }

        public UInt64 MftChangeTime
        { get; private set; }

        public UInt64 LastAccessTime
        { get; private set; }

        public UInt32 FileAttributes                  /* READ_ONLY=0x01, HIDDEN=0x02, SYSTEM=0x04, VOLUME_ID=0x08, ARCHIVE=0x20, DEVICE=0x40 */
        { get; private set; }

        public UInt32 MaximumVersions
        { get; private set; }

        public UInt32 VersionNumber
        { get; private set; }

        public UInt32 ClassId
        { get; private set; }

        public UInt32 OwnerId                         // NTFS 3.0 only
        { get; private set; }

        public UInt32 SecurityId                      // NTFS 3.0 only
        { get; private set; }

        public UInt64 QuotaCharge                     // NTFS 3.0 only
        { get; private set; }

        public UInt64 Usn                             // NTFS 3.0 only
        { get; private set; }

    }
}
