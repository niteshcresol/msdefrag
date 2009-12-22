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
            else
            {
                int runLengthSize = (runDataValue & 0x0F);
                int runOffsetSize = ((runDataValue & 0xF0) >> 4);

                length = ReadLength(reader, runLengthSize);
                offset = ReadOffset(reader, runOffsetSize);
                return true;
            }
        }

        private static UInt64 ReadLength(BinaryReader runData, int length)
        {
            if (length > 8)
                throw new InvalidDataException("The length shall never be more than 8 bytes");

            Byte[] runLength = new Byte[8];
            for (int i = 0; i < 8; i++)
            {
                if (i < length)
                    runLength[i] = runData.ReadByte();
                else
                    runLength[i] = 0;
            }
            return BitConverter.ToUInt64(runLength, 0);
        }

        private static Int64 ReadOffset(BinaryReader runData, int length)
        {
            if (length == 0) return 0;
            if (length > 8)
                throw new InvalidDataException("The offset shall never be more than 8 bytes");
            
            Byte[] runOffset = new Byte[8];
            for (int i = 0; i < 8; i++)
            {
                if (i < length)
                    runOffset[i] = runData.ReadByte();
                else
                    runOffset[i] = (Byte)((runOffset[length-1] >= 0x80) ? 0xFF : 0);
            }
            return BitConverter.ToInt64(runOffset, 0);
        }
    }
}
