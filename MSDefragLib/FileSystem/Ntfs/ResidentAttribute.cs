using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class ResidentAttribute
    {
        public Attribute m_attribute;
        public UInt32 ValueLength;
        public UInt16 ValueOffset;
        public UInt16 Flags;                           // 0x0001 = Indexed

        public ResidentAttribute()
        {
        }

        public ResidentAttribute(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            m_attribute = new Attribute(buffer, ref offset);
            ValueLength = buffer.ToUInt32(ref offset);
            ValueOffset = buffer.ToUInt16(ref offset);
            Flags = buffer.ToUInt16(ref offset);
        }
    }
}
