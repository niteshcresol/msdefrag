using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class NtfsDiskInfoStructure
    {
        public NtfsDiskInfoStructure()
        {
            buffers = new Buffers();
        }

        public UInt64 BytesPerSector;
        public UInt64 SectorsPerCluster;
        public UInt64 TotalSectors;
        public UInt64 MftStartLcn;
        public UInt64 Mft2StartLcn;
        public UInt64 BytesPerMftRecord;
        public UInt64 ClustersPerIndexRecord;

        Buffers buffers;
    }
}
