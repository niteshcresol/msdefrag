using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// To prevent the MFT becoming fragmented, Windows maintains a buffer
    /// around it. No new files will be created in this buffer region until
    /// the other disk space is used up. The buffer size is configurable and
    /// can be 12.5%, 25%, 37.5% or 50% of the disk. Each time the rest of
    /// the disk becomes full, the buffer size is halved.
    /// 
    /// On a freshly formatted volume, inodes 0x0B to 0x0F are marked as in
    /// use, but empty. Inodes 0x10 to 0x17 are marked as free and not used.
    /// This doesn't change until the volume is under a lot of stress.
    ///  
    /// When the $MFT becomes very fragmented it won't fit into one FILE
    /// Record and an extension record is needed. If a new record was simply
    /// allocated at the end of the $MFT then we encounter a problem. The
    /// $DATA Attribute describing the location of the new record is in the 
    /// new record.
    /// 
    /// The new records are therefore allocated from inode 0x0F, onwards. 
    /// The $MFT is always a minimum of 16 FILE Records long, therefore
    /// always exists. After inodes 0x0F to 0x17 are used up, higher,
    /// unreserved, inodes are used. 
    /// </summary>
    public class MasterFileTable
    {
        public MasterFileTable(String drive)
        {
        }
    }
}
