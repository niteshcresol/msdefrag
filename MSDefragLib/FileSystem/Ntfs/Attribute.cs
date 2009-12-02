using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [Flags]
    public enum AttributeFlags : ushort
    {
        Compressed = 0x0001,
        Encrypted = 0x4000,
        Sparse = 0x8000
    }

    public class Attribute : ISizeHelper
    {
        public AttributeType m_attributeType;
        public UInt32 m_length;
        public Boolean m_nonResident;
        public Byte m_nameLength;
        public UInt16 m_nameOffset;
        public AttributeFlags m_flags;
        public UInt16 m_attributeNumber;

        protected Attribute()
        {
        }

        protected void InternalParse(BinaryReader reader)
        {
            m_attributeType = AttributeType.Parse(reader);
            if (m_attributeType.Type != AttributeTypeEnum.AttributeEndOfList)
            {
                m_length = reader.ReadUInt32();
                m_nonResident = reader.ReadBoolean();
                m_nameLength = reader.ReadByte();
                m_nameOffset = reader.ReadUInt16();
                m_flags = (AttributeFlags)reader.ReadUInt16();
                m_attributeNumber = reader.ReadUInt16();
            }
        }

        public static Attribute Parse(BinaryReader reader)
        {
            Attribute attribute = new Attribute();
            attribute.InternalParse(reader);
            return attribute;
        }

        #region ISizeHelper Members

        public virtual long Size
        {
            get 
            {
                if (m_attributeType.Type == AttributeTypeEnum.AttributeEndOfList)
                    return m_attributeType.Size;
                return m_attributeType.Size + 4 + 1 + 1 + 2 + 2 + 2;
            }
        }

        #endregion
    }
}
