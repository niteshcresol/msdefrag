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

    [DebuggerDisplay("Name = {Name}")]
    public class FileNameAttribute
    {
        private FileNameAttribute()
        {
        }

        [Conditional("DEBUG")]
        public void AssertValid()
        {
            Debug.Assert((_nameType == 0x01) || (_nameType == 0x02) || (_nameType == 0x03));
        }

        public static FileNameAttribute Parse(BinaryReader reader)
        {
            FileNameAttribute filename = new FileNameAttribute();
            filename.ParentDirectory = InodeReference.Parse(reader);
            filename.CreationTime = reader.ReadUInt64();
            filename.ChangeTime = reader.ReadUInt64();
            filename.LastWriteTime = reader.ReadUInt64();
            filename.LastAccessTime = reader.ReadUInt64();
            filename.AllocatedSize = reader.ReadUInt64();
            filename.DataSize = reader.ReadUInt64();
            filename.FileAttributes = reader.ReadUInt32();
            filename.AlignmentOrReserved = reader.ReadUInt32();
            int nameLength = reader.ReadByte();
            filename._nameType = reader.ReadByte();
            filename.Name = Helper.ParseString(reader, nameLength);
            filename.AssertValid();
            return filename;
        }

        private Byte _nameType;

        /// <summary>
        /// NTFS or DOS name
        /// </summary>
        public NameType NameType
        { get { return (NameType)_nameType; } }

        public InodeReference ParentDirectory
        { get; private set; }

        public UInt64 CreationTime
        { get; private set; }

        public UInt64 ChangeTime
        { get; private set; }

        public UInt64 LastWriteTime
        { get; private set; }

        public UInt64 LastAccessTime
        { get; private set; }

        public UInt64 AllocatedSize
        { get; private set; }

        public UInt64 DataSize
        { get; private set; }

        public UInt32 FileAttributes
        { get; private set; }

        public UInt32 AlignmentOrReserved
        { get; private set; }

        public String Name
        { get; private set; }
    }
}
