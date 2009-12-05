using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Length = {Length}")]
    class AttributeList : IAttribute
    {
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
                list.LowestVcn = reader.ReadUInt64();
                list.FileReferenceNumber = InodeReference.Parse(reader);
                list.Instance = reader.ReadUInt16();
                list.AlignmentOrReserved = new UInt16[3];
                list.AlignmentOrReserved[0] = reader.ReadUInt16();
                list.AlignmentOrReserved[1] = reader.ReadUInt16();
                list.AlignmentOrReserved[2] = reader.ReadUInt16();
            }
            return list;
        }

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

        public UInt64 LowestVcn
        { get; private set; }

        public InodeReference FileReferenceNumber
        { get; private set; }

        public UInt16 Instance
        { get; private set; }

        public UInt16[] AlignmentOrReserved // [3];
        { get; private set; }

    }
}
