using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class InodeReference
    {
        public UInt32 m_iNodeNumberLowPart;
        public UInt16 m_iNodeNumberHighPart;

        public UInt16 m_sequenceNumber;

        public InodeReference(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray Buffer, ref Int64 offset)
        {
            m_iNodeNumberLowPart = Buffer.ToUInt32(ref offset);
            m_iNodeNumberHighPart = Buffer.ToUInt16(ref offset);
            m_sequenceNumber = Buffer.ToUInt16(ref offset);
        }
    }
}
