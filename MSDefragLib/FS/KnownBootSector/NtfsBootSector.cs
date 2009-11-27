using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FS.KnownBootSector
{
    /// <summary>
    /// Class for describing boot sector
    /// http://www.ntfs.com/ntfs-partition-boot-sector.htm
    /// </summary>
    class NtfsBootSector : BaseBootSector
    {
        public NtfsBootSector(byte[] buffer)
            : base(buffer)
        {
            AssertValid();
        }

        [Conditional("DEBUG")]
        protected void AssertValid()
        {
            base.AssertValid();

            // 'NTFS    '
            Debug.Assert(OemId == 0x202020205346544E);
        }

        #region IBootSector Members

        public override Filesystem Filesystem
        {
            get { return Filesystem.NTFS; }
        }

        public override ushort BytesPerSector
        {
            get
            {
                return BitConverter.ToUInt16(Data, 11);
            }
        }

        public override ulong SectorsPerCluster
        {
            get
            {
                // TODO: check for impossible values
                return BitConverter.ToUInt64(Data, 13);
            }
        }

        public override ulong TotalSectors
        {
            get
            {
                return BitConverter.ToUInt64(Data, 40);
            }
        }

        public override ulong Mft1StartLcn
        {
            get
            {
                return BitConverter.ToUInt64(Data, 48);
            }
        }

        public override ulong Mft2StartLcn
        {
            get
            {
                return BitConverter.ToUInt64(Data, 56);
            }
        }

        public override ushort SectorsPerTrack
        {
            get
            {
                return BitConverter.ToUInt16(Data, 24);
            }
        }

        public override ushort NumberOfHeads
        {
            get
            {
                return BitConverter.ToUInt16(Data, 26);
            }
        }

        public override uint ClustersPerIndexRecord
        {
            get
            {
                return BitConverter.ToUInt32(Data, 68);
            }
        }

        public override uint ClustersPerMftRecord
        {
            get
            {
                return BitConverter.ToUInt32(Data, 64);
            }
        }

        #endregion

        public override UInt64 OemId
        {
            get
            {
                return BitConverter.ToUInt64(Data, 0x03);
            }
        }

        public override ulong Serial
        {
            get
            {
                return BitConverter.ToUInt64(Data, 72);
            }
        }

        public override byte MediaType
        {
            get
            {
                return Data[21];
            }
        }
    }
}
