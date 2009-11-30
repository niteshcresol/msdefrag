using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Length = {m_length}")]
    class AttributeList : ISizeHelper
    {
        public AttributeType m_attributeType;
        public UInt16 m_length;
        public Byte m_nameLength;
        public Byte m_nameOffset;
        public UInt64 m_lowestVcn;
        public InodeReference m_fileReferenceNumber;
        public UInt16 m_instance;
        public UInt16[] m_alignmentOrReserved; // [3];

        private AttributeList()
        {
        }

        public static AttributeList Parse(BinaryReader reader)
        {
            AttributeList list = new AttributeList();
            list.m_attributeType = AttributeType.Parse(reader);
            if (list.m_attributeType.Type != AttributeTypeEnum.AttributeEndOfList)
            {
                list.m_length = reader.ReadUInt16();
                list.m_nameLength = reader.ReadByte();
                list.m_nameOffset = reader.ReadByte();
                list.m_lowestVcn = reader.ReadUInt64();
                list.m_fileReferenceNumber = InodeReference.Parse(reader);
                list.m_instance = reader.ReadUInt16();
                list.m_alignmentOrReserved = new UInt16[3];
                list.m_alignmentOrReserved[0] = reader.ReadUInt16();
                list.m_alignmentOrReserved[1] = reader.ReadUInt16();
                list.m_alignmentOrReserved[2] = reader.ReadUInt16();
            }
            return list;
        }

        #region ISizeHelper Members

        public long Size
        {
            get
            {
                if (m_attributeType.Type == AttributeTypeEnum.AttributeEndOfList)
                    return m_attributeType.Size;
                return m_attributeType.Size + 2 + 1 + 1 + 8 + m_fileReferenceNumber.Size + 2 + 3 * 2;
            }
        }

        #endregion
    }
}
