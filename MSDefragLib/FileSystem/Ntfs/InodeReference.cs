using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class InodeReference : ISizeHelper
    {
        public UInt32 m_iNodeNumberLowPart;
        public UInt16 m_iNodeNumberHighPart;

        public UInt16 m_sequenceNumber;

        private InodeReference()
        {
        }

        public static InodeReference Parse(BinaryReader reader)
        {
            InodeReference r = new InodeReference();
            r.m_iNodeNumberLowPart = reader.ReadUInt32();
            r.m_iNodeNumberHighPart = reader.ReadUInt16();
            r.m_sequenceNumber = reader.ReadUInt16();
            return r;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return 4 + 2 + 2; }
        }

        #endregion
    }
}
