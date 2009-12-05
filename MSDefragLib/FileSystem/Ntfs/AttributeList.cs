using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Length = {m_length}")]
    class AttributeList : IAttribute
    {
        public AttributeType Type
        { get; private set; }

        /// <summary>
        /// Only the lower word is used (Uint16)
        /// </summary>
        public UInt32 Length
        { get; private set; }

        public Byte NameLength
        { get; private set; }

        /// <summary>
        /// Only the lower byte is used (Byte)
        /// </summary>
        public UInt16 NameOffset
        { get; private set; }

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
            list.Type = AttributeType.Parse(reader);
            if (list.Type.Type != AttributeTypeEnum.AttributeEndOfList)
            {
                list.Length = reader.ReadUInt16();
                list.NameLength = reader.ReadByte();
                list.NameOffset = reader.ReadByte();
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
    }
}
