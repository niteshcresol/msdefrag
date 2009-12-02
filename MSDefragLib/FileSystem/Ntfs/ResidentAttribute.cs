using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class ResidentAttribute : Attribute, ISizeHelper
    {
        public UInt32 ValueLength
        { get; private set; }

        public UInt16 ValueOffset
        { get; private set; }

        // 0x0001 = Indexed
        public UInt16 Flags
        { get; private set; }

        private ResidentAttribute()
        {
        }

        public static new ResidentAttribute Parse(BinaryReader reader)
        {
            ResidentAttribute a = new ResidentAttribute();
            a.InternalParse(reader);
            a.ValueLength = reader.ReadUInt32();
            a.ValueOffset = reader.ReadUInt16();
            a.Flags = reader.ReadUInt16();
            return a;
        }

        #region ISizeHelper Members

        public override long Size
        {
            get { return base.Size + 4 + 2 + 2; }
        }

        #endregion
    }
}
