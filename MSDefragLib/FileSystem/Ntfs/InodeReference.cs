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
        public UInt32 m_iNodeNumberLowPart;
        public UInt16 m_iNodeNumberHighPart;

        public UInt64 BaseInodeNumber
        {
            private set { }
            get
            {
                UInt64 retValue = 0;

                retValue = (UInt64) m_iNodeNumberLowPart + ((UInt64)m_iNodeNumberHighPart << 32);

                return retValue;
            }
        }

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
    }
}
