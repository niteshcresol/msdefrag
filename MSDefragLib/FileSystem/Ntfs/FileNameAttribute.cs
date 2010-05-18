using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [Flags]
    public enum NameTypes : byte
    {
        /// <summary>
        /// This is the largest namespace. It is case sensitive and allows all
        /// Unicode characters except for: '\0' and '/'.  Beware that in
        /// WinNT/2k/2003 by default files which eg have the same name except
        /// for their case will not be distinguished by the standard utilities
        /// and thus a "del filename" will delete both "filename" and "fileName"
        /// without warning.  However if for example Services For Unix (SFU) are
        /// installed and the case sensitive option was enabled at installation
        /// time, then you can create/access/delete such files.
        /// Note that even SFU places restrictions on the filenames beyond the
        /// '\0' and '/' and in particular the following set of characters is
        /// not allowed: '"', '/', '<', '>', '\'.  All other characters,
        /// including the ones no allowed in WIN32 namespace are allowed.
        /// Tested with SFU 3.5 (this is now free) running on Windows XP.
        /// </summary>
        POSIX = 0x00,   // POSIX name
        /// <summary>
        /// The standard WinNT/2k NTFS long filenames. Case insensitive.  All
        /// Unicode chars except: '\0', '"', '*', '/', ':', '<', '>', '?', '\',
        /// and '|'.  Further, names cannot end with a '.' or a space.
        /// </summary>
        NTFS = 0x01,    // long name
        /// <summary>
        /// The standard DOS filenames (8.3 format). Uppercase only.  All 8-bit
        /// characters greater space, except: '"', '*', '+', ',', '/', ':', ';',
        /// '<', '=', '>', '?', and '\'.
        /// </summary>
        DOS = 0x02,      // 8.3 name
        /// <summary>
        /// means that both the Win32 and the DOS filenames are identical and
        /// hence have been saved in this single filename record.
        /// </summary>
        WIN32_DOS = 0x03
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
            Debug.Assert((_nameType == 0x00) || (_nameType == 0x01) ||
                (_nameType == 0x02) || (_nameType == 0x03));
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
        public NameTypes NameType
        { get { return (NameTypes)_nameType; } }

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
