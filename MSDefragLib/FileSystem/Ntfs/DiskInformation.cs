using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// In NTFS, the Cluster is the fundamental unit of disk usage. The 
    /// number of sectors that make up a cluster is always a power of 2,
    /// and the number is fixed when the volume is formatted. This number
    /// is called the Cluster Factor and is usually quoted in bytes,
    /// e.g. 8KB, 2KB. NTFS addresses everything by its Logical Cluster
    /// Number. 
    /// </summary>
    public class DiskInformation
    {
        public DiskInformation(FS.IBootSector bootSector)
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

        public UInt64 ClusterToInode(UInt64 cluster)
        {
            return cluster * BytesPerCluster / BytesPerMftRecord;
        }

        public UInt64 InodeToCluster(UInt64 inode)
        {
            return inode * BytesPerMftRecord / BytesPerCluster;
        }

        public UInt64 ClusterToBytes(UInt64 cluster)
        {
            return cluster * BytesPerCluster;
        }

        public UInt64 BytesToCluster(UInt64 bytes)
        {
            return bytes / BytesPerCluster;
        }

        public UInt64 InodeToBytes(UInt64 inode)
        {
            return inode * BytesPerMftRecord;
        }

        public UInt64 BytesToInode(UInt64 bytes)
        {
            return bytes / BytesPerMftRecord;
        }

        public UInt64 BytesPerCluster
        {
            get
            {
                return BytesPerSector * SectorsPerCluster;
            }
        }

        public UInt64 BytesPerSector
        { get; private set; }

        public UInt64 SectorsPerCluster
        { get; private set; }

        public UInt64 TotalSectors
        { get; private set; }

        public UInt64 MftStartLcn
        { get; private set; }

        public UInt64 Mft2StartLcn
        { get; private set; }
        
        public UInt64 BytesPerMftRecord
        { get; private set; }
        
        public UInt64 ClustersPerIndexRecord
        { get; private set; }
    }
}
