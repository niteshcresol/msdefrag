using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class ResidentAttribute : ISizeHelper
    {
        public Attribute m_attribute;
        public UInt32 ValueLength;
        public UInt16 ValueOffset;
        public UInt16 Flags;                           // 0x0001 = Indexed

        private ResidentAttribute()
        {
        }

        public static ResidentAttribute Parse(BinaryReader reader)
        {
            ResidentAttribute a = new ResidentAttribute();
            a.m_attribute = Attribute.Parse(reader);
            a.ValueLength = reader.ReadUInt32();
            a.ValueOffset = reader.ReadUInt16();
            a.Flags = reader.ReadUInt16();
            return a;
        }

        #region ISizeHelper Members

        public long Size
        {
            get { return m_attribute.Size + 4 + 2 + 2; }
        }

        #endregion
    }
}
