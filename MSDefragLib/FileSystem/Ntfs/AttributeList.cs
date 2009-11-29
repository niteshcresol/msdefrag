using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class AttributeList
    {
        public AttributeType m_attributeType;
        public UInt16 m_length;
        public Byte m_nameLength;
        public Byte m_nameOffset;
        public UInt64 m_lowestVcn;
        public InodeReference m_fileReferenceNumber;
        public UInt16 m_instance;
        public UInt16[] m_alignmentOrReserved; // [3];

        public AttributeList(ByteArray buffer, ref Int64 offset)
        {
            Parse(buffer, ref offset);
        }

        public void Parse(ByteArray buffer, ref Int64 offset)
        {
            m_attributeType = new AttributeType(buffer, ref offset);

            if (m_attributeType.Type == AttributeTypeEnum.AttributeEndOfList)
            {
                return;
            }
            
            m_length = buffer.ToUInt16(ref offset);
            m_nameLength = buffer.ToByte(ref offset);
            m_nameOffset = buffer.ToByte(ref offset);
            m_lowestVcn = buffer.ToUInt64(ref offset);
            m_fileReferenceNumber = new InodeReference(buffer, ref offset);
            m_instance = buffer.ToUInt16(ref offset);
            m_alignmentOrReserved = new UInt16[3];
        }
    }
}
