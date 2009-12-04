using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Non-resident attributes are stored in intervals of clusters 
    /// called runs. Each run is represented by its starting cluster
    /// and its length. The starting cluster of a run is coded as an
    /// offset to the starting cluster of the previous run.
    ///  
    /// Normal, compressed and sparse files are all defined by runs.
    /// 
    /// The examples start simple, then quickly get complicated.
    /// 
    /// This is a table written in the content part of a non-resident
    /// file attribute, which allows to have access to its stream. 
    /// </summary>
    public class RunData
    {
        public static Boolean Parse(BinaryReader reader, out UInt64 length, out Int64 offset)
        {
            Byte runDataValue = reader.ReadByte();
            if (runDataValue == 0)
            {
                length = 0;
                offset = 0;
                return false;
            }

            /* Decode the RunData and calculate the next Lcn. */
            int runLengthSize = (runDataValue & 0x0F);
            int runOffsetSize = ((runDataValue & 0xF0) >> 4);

            length = ReadLength(reader, runLengthSize);
            offset = ReadOffset(reader, runOffsetSize);
            return true;
        }

        private static UInt64 ReadLength(BinaryReader runData, int length)
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

        private static Int64 ReadOffset(BinaryReader runData, int length)
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
