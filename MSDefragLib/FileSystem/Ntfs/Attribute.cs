using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class Attribute : ISizeHelper
    {
        public AttributeType m_attributeType;
        public UInt32 m_length;
        public Boolean m_nonResident;
        public Byte m_nameLength;
        public UInt16 m_nameOffset;
        public UInt16 m_flags;                    /* 0x0001 = Compressed, 0x4000 = Encrypted, 0x8000 = Sparse */
        public UInt16 m_attributeNumber;

        private Attribute()
        {
        }

        public static Attribute Parse(BinaryReader reader)
        {
            Attribute attribute = new Attribute();
            attribute.m_attributeType = AttributeType.Parse(reader);
            if (attribute.m_attributeType.Type != AttributeTypeEnum.AttributeEndOfList)
            {
                attribute.m_length = reader.ReadUInt32();
                attribute.m_nonResident = reader.ReadBoolean();
                attribute.m_nameLength = reader.ReadByte();
                attribute.m_nameOffset = reader.ReadUInt16();
                attribute.m_flags = reader.ReadUInt16();
                attribute.m_attributeNumber = reader.ReadUInt16();
            }
            return attribute;
        }

        #region ISizeHelper Members

        public long Size
        {
            get 
            {
                if (m_attributeType.Type == AttributeTypeEnum.AttributeEndOfList)
                    return m_attributeType.Size;
                return m_attributeType.Size + 12;
            }
        }

        #endregion
    }
}
