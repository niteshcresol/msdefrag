using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class RunData
    {
        public static UInt64 ReadLength(BinaryReader runData, int length)
        {
            Debug.Assert(length <= 8);
            UlongBytes runLength = new UlongBytes();
            runLength.Value = 0;
            for (int i = 0; i < length; i++)
            {
                runLength.Bytes[i] = runData.ReadByte();
            }
            return runLength.Value;
        }

        public static Int64 ReadOffset(BinaryReader runData, int length)
        {
            if (length == 0) return 0;
            Debug.Assert(length <= 8);
            UlongBytes runOffset = new UlongBytes();
            runOffset.Value = 0;
            for (int j = 0; j < length; j++)
            {
                runOffset.Bytes[j] = runData.ReadByte();
            }

            if (runOffset.Bytes[length - 1] >= 0x80)
            {
                int i = length;
                while (i < 8)
                    runOffset.Bytes[i++] = 0xFF;
            }
            return (Int64)runOffset.Value;
        }
    }
}
