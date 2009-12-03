using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class DiskInfoStructure
    {
        public DiskInfoStructure(FS.IBootSector bootSector)
        {
            /* Extract data from the bootblock. */
            BytesPerSector = bootSector.BytesPerSector;
            SectorsPerCluster = bootSector.SectorsPerCluster;

            TotalSectors = bootSector.TotalSectors;
            MftStartLcn = bootSector.Mft1StartLcn;
            Mft2StartLcn = bootSector.Mft2StartLcn;

            UInt64 clustersPerMftRecord = bootSector.ClustersPerMftRecord;
            ClustersPerIndexRecord = bootSector.ClustersPerIndexRecord;

            if (clustersPerMftRecord >= 128)
            {
                BytesPerMftRecord = (UInt64)(1 << (256 - (Int16)clustersPerMftRecord));
            }
            else
            {
                BytesPerMftRecord = clustersPerMftRecord * BytesPerCluster;
            }
        }

        public UInt64 BytesPerCluster
        {
            get
            {
                return BytesPerSector * SectorsPerCluster;
            }
        }

        public UInt64 BytesPerSector;
        public UInt64 SectorsPerCluster;
        public UInt64 TotalSectors;
        public UInt64 MftStartLcn;
        public UInt64 Mft2StartLcn;
        public UInt64 BytesPerMftRecord;
        public UInt64 ClustersPerIndexRecord;
    }
}
