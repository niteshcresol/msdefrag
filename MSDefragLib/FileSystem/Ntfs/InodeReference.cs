using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class InodeReference
    {
        private InodeReference()
        {
        }

        public static InodeReference Parse(BinaryReader reader)
        {
            InodeReference r = new InodeReference();
            UInt32 lowPart = reader.ReadUInt32();
            UInt16 highPart = reader.ReadUInt16();
            r.BaseInodeNumber = (UInt64)lowPart + ((UInt64)highPart << 32);
            r.SequenceNumber = reader.ReadUInt16();
            return r;
        }

        /// <summary>
        /// 48 bit inode number
        /// </summary>
        public UInt64 BaseInodeNumber
        { get; private set; }

        public UInt16 SequenceNumber
        { get; private set; }

    }
}
