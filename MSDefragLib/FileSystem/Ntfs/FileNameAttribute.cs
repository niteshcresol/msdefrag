using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class FileNameAttribute : ISizeHelper
    {
        public InodeReference m_parentDirectory;
        public UInt64 m_creationTime;
        public UInt64 m_changeTime;
        public UInt64 m_lastWriteTime;
        public UInt64 m_lastAccessTime;
        public UInt64 m_allocatedSize;
        public UInt64 m_dataSize;
        public UInt32 m_fileAttributes;
        public UInt32 m_alignmentOrReserved;
        public Byte m_nameLength;
        public Byte m_nameType;                   /* NTFS=0x01, DOS=0x02 */
        public String m_name/*[1]*/;

        private FileNameAttribute()
        {
        }

        public static FileNameAttribute Parse(BinaryReader reader)
        {
            FileNameAttribute f = new FileNameAttribute();
            f.m_parentDirectory = InodeReference.Parse(reader);
            f.m_creationTime = reader.ReadUInt64();
            f.m_changeTime = reader.ReadUInt64();
            f.m_lastWriteTime = reader.ReadUInt64();
            f.m_lastAccessTime = reader.ReadUInt64();
            f.m_allocatedSize = reader.ReadUInt64();
            f.m_dataSize = reader.ReadUInt64();
            f.m_fileAttributes = reader.ReadUInt32();
            f.m_alignmentOrReserved = reader.ReadUInt32();
            f.m_nameLength = reader.ReadByte();
            f.m_nameType = reader.ReadByte();
            f.m_name = Helper.ParseString(reader, f.m_nameLength);
            return f;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return m_parentDirectory.Size + 8 + 8 + 8 + 8 + 8 + 8 + 4 + 4 + 1 + 1 + m_nameLength; }
        }

        #endregion
    }
}
