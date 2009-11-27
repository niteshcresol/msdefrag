using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FS.KnownBootSector
{
    /// <summary>
    /// Includes the common functionality for all boot sectors
    /// </summary>
    public abstract class BaseBootSector : IBootSector
    {
        private const UInt16 BOOT_SECTOR_SIGNATURE = 0xAA55;

        public BaseBootSector(byte[] buffer)
        {
            Data = buffer;
            AssertValid();
        }

        [Conditional("DEBUG")]
        protected void AssertValid()
        {
            Debug.Assert(EndOfSector == BOOT_SECTOR_SIGNATURE);
        }

        #region IBootSector Members

        /// <summary>
        /// The end of sector signature, shall always be 0xAA55
        /// </summary>
        public UInt16 EndOfSector
        {
            get
            {
                return BitConverter.ToUInt16(Data, 0x1FE);
            }
        }

        public abstract Filesystem Filesystem
        {
            get;
        }

        public byte[] Data
        {
            get; private set;
        }

        public virtual ushort BytesPerSector
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong SectorsPerCluster
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong TotalSectors
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong OemId
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong Mft1StartLcn
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong Mft2StartLcn
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ushort SectorsPerTrack
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ushort NumberOfHeads
        {
            get { throw new NotImplementedException(); }
        }

        public virtual uint ClustersPerMftRecord
        {
            get { throw new NotImplementedException(); }
        }

        public virtual uint ClustersPerIndexRecord
        {
            get { throw new NotImplementedException(); }
        }

        public abstract ulong Serial
        { get; }

        public abstract byte MediaType
        { get; }

        #endregion

    }
}
