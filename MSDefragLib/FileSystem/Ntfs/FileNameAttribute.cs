using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [Flags]
    public enum NameType : byte
    {
        NTFS = 0x01,    // long name
        DOS = 0x02      // 8.3 name
    }

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
        public String Name
        { get; private set; }

        private FileNameAttribute()
        {
        }

        [Conditional("DEBUG")]
        public void AssertValid()
        {
            Debug.Assert((m_nameType == 0x01) || (m_nameType == 0x02) || (m_nameType == 0x03));
        }

        // NTFS=0x01, DOS=0x02
        private Byte m_nameType;

        public NameType NameType
        { get { return (NameType)m_nameType; } }

        public static FileNameAttribute Parse(BinaryReader reader)
        {
            FileNameAttribute filename = new FileNameAttribute();
            filename.m_parentDirectory = InodeReference.Parse(reader);
            filename.m_creationTime = reader.ReadUInt64();
            filename.m_changeTime = reader.ReadUInt64();
            filename.m_lastWriteTime = reader.ReadUInt64();
            filename.m_lastAccessTime = reader.ReadUInt64();
            filename.m_allocatedSize = reader.ReadUInt64();
            filename.m_dataSize = reader.ReadUInt64();
            filename.m_fileAttributes = reader.ReadUInt32();
            filename.m_alignmentOrReserved = reader.ReadUInt32();
            int nameLength = reader.ReadByte();
            filename.m_nameType = reader.ReadByte();
            filename.Name = Helper.ParseString(reader, nameLength);
            filename.AssertValid();
            return filename;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return m_parentDirectory.Size + 8 + 8 + 8 + 8 + 8 + 8 + 4 + 4 + 1 + 1 + Name.Length*2; }
        }

        #endregion
    }
}
