using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    public class BitmapFile
    {
        public BitmapFile(Disk disk, DiskInformation diskInfo, FragmentList fragments,
            UInt64 bitmapBytes, UInt64 dataBytes)
        {
            _bitmapBytes = bitmapBytes;
            _dataBytes = dataBytes;
            _diskInfo = diskInfo;
            MftBitmap = new ByteArray();
            MftBitmap.m_bytes = disk.Load(diskInfo, fragments);
        }

        /// <summary>
        /// Construct an array of all the items in memory, indexed by m_iNode.
        ///
        /// NOTE:
        /// The maximum number of Inodes is primarily determined by the size of the
        /// bitmap. But that is rounded up to 8 Inodes, and the MFT can be shorter.
        /// </summary>
        public UInt64 MaxInode
        {
            get
            {
                return Math.Min(_bitmapBytes * 8, _diskInfo.BytesToInode(_dataBytes));
            }
        }

        public UInt64 UsedInodes
        {
            get
            {
                UInt64 used = 0;
                BitArray bits = new BitArray(MftBitmap.m_bytes);
                UInt64 c = 0;
                foreach (bool bit in bits)
                {
                    if (++c > MaxInode)
                        break;
                    if (bit)
                        used++;
                }
                return used;
            }
        }

        public BitArray Bits
        {
            get
            {
                return new BitArray(MftBitmap.m_bytes);
            }
        }

        private UInt64 _bitmapBytes;
        private UInt64 _dataBytes;
        private DiskInformation _diskInfo;

        public ByteArray MftBitmap
        { get; private set; }
    }
}
