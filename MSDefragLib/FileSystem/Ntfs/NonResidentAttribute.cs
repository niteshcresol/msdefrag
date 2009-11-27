using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NonResidentAttribute
    {
        public Attribute m_attribute;
        public UInt64 m_startingVcn;
        public UInt64 m_lastVcn;
        public UInt16 m_runArrayOffset;
        public Byte m_compressionUnit;
        public List<Byte> m_alignmentOrReserved/*[5]*/;
        public UInt64 m_allocatedSize;
        public UInt64 m_dataSize;
        public UInt64 m_initializedSize;
        public UInt64 m_compressedSize;                  // Only when compressed

        public NonResidentAttribute()
        {
        }

        public NonResidentAttribute(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            m_attribute = new Attribute(buffer, ref offset);
            m_startingVcn = buffer.ToUInt64(ref offset);
            m_lastVcn = buffer.ToUInt64(ref offset);
            m_runArrayOffset = buffer.ToUInt16(ref offset);
            m_compressionUnit = buffer.ToByte(ref offset);
            m_alignmentOrReserved = new List<Byte>();
            m_alignmentOrReserved.Add(buffer.ToByte(ref offset));
            m_alignmentOrReserved.Add(buffer.ToByte(ref offset));
            m_alignmentOrReserved.Add(buffer.ToByte(ref offset));
            m_alignmentOrReserved.Add(buffer.ToByte(ref offset));
            m_alignmentOrReserved.Add(buffer.ToByte(ref offset));
            m_allocatedSize = buffer.ToUInt64(ref offset);
            m_dataSize = buffer.ToUInt64(ref offset);
            m_initializedSize = buffer.ToUInt64(ref offset);
            m_compressedSize = buffer.ToUInt64(ref offset);
        }
    }
}
