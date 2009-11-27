using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FS
{
    /// <summary>
    /// Knwon disk types
    /// </summary>
    public enum Filesystem
    {
        UnknownType = 0,
        MBR = -1,
        NTFS = 1,
        FAT12 = 12,
        FAT16 = 16,
        FAT32 = 32
    }

}
