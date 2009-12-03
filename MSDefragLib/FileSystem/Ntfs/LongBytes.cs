using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class UlongBytes
    {
        public Byte[] Bytes = new Byte[8];

        public UInt64 Value
        {
            set
            {
                Bytes = BitConverter.GetBytes(value);
            }

            get
            {
                return BitConverter.ToUInt64(Bytes, 0); ;
            }
        }
    };
}
