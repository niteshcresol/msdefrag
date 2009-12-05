using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{

    class Helper
    {
        public static BinaryReader BinaryReader(ByteArray buffer)
        {
            return BinaryReader(buffer, 0);
        }

        public static BinaryReader BinaryReader(ByteArray buffer, Int64 offset)
        {
            Int64 count = buffer.m_bytes.Length - offset;
            Debug.Assert(count > 0);
            System.IO.Stream stream = new MemoryStream(buffer.m_bytes, (int)offset, (int)count);
            BinaryReader reader = new BinaryReader(stream);
            return reader;
        }

        public static String ParseString(BinaryReader reader, int length)
        {
            StringBuilder sb = new StringBuilder();
            for (int ii = 0; ii < length; ii++)
            {
                UInt16 i = reader.ReadUInt16();
                sb.Append((Char)i);
            }
            return sb.ToString();
        }
    }
}
