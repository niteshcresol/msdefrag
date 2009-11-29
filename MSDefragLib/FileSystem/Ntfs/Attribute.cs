using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class Attribute
    {
        public AttributeType m_attributeType;
        public UInt32 m_length;
        public Boolean m_nonResident;
        public Byte m_nameLength;
        public UInt16 m_nameOffset;
        public UInt16 m_flags;                    /* 0x0001 = Compressed, 0x4000 = Encrypted, 0x8000 = Sparse */
        public UInt16 m_attributeNumber;

        public Attribute(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            //HACK: temporary hack to demonstrate the usage of the binary reader
            BinaryReader reader = new BinaryReader(new MemoryStream(buffer.m_bytes, (int)offset, 4));
            m_attributeType = AttributeType.Parse(reader);
            offset += 4;

            if (m_attributeType.Type == AttributeTypeEnum.AttributeEndOfList)
            {
                return;
            }

            m_length = buffer.ToUInt32(ref offset);
            m_nonResident = buffer.ToBoolean(ref offset);
            m_nameLength = buffer.ToByte(ref offset);
            m_nameOffset = buffer.ToUInt16(ref offset);
            m_flags = buffer.ToUInt16(ref offset);
            m_attributeNumber = buffer.ToUInt16(ref offset);
        }
    }
}
