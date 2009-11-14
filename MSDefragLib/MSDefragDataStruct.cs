using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public enum DiskType
    {
        UnknownType = 0,
        NTFS = 1,
        FAT12 = 12,
        FAT16 = 16,
        FAT32 = 32
    };

    public class DiskStruct
    {
	    public IntPtr    VolumeHandle;

	    public String    MountPoint;          /* Example: "c:" */
	    String    MountPointSlash;     /* Example: "c:\" */
	    String    VolumeName/*[52]*/;       /* Example: "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}" */
	    String    VolumeNameSlash/*[52]*/;  /* Example: "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}\" */

	    public DiskType  Type;

	    public UInt64   MftLockedClusters;    /* Number of clusters at begin of MFT that cannot be moved. */
    };

    /* List in memory of the fragments of a file. */

    public class FragmentListStruct
    {
	    public UInt64 Lcn;                            /* Logical cluster number, location on disk. */
	    public UInt64 NextVcn;                        /* Virtual cluster number of next fragment. */
	    public FragmentListStruct Next;
    };

    /* List in memory of all the files on disk, sorted by LCN (Logical Cluster Number). */

    public class ItemStruct
    {
	    public ItemStruct Parent;              /* Parent item. */
	    public ItemStruct Smaller;             /* Next smaller item. */
	    public ItemStruct Bigger;              /* Next bigger item. */

	    public String LongFilename;                /* Long filename. */
	    public String LongPath;                    /* Full path on disk, long filenames. */
	    public String ShortFilename;               /* Short filename (8.3 DOS). */
	    public String ShortPath;                   /* Full path on disk, short filenames. */

	    public UInt64   Bytes;                        /* Total number of bytes. */
	    public UInt64   Clusters;                     /* Total number of clusters. */
	    public UInt64   CreationTime;                 /* 1 second = 10000000 */
	    public UInt64   MftChangeTime;
	    public UInt64   LastAccessTime;

	    public FragmentListStruct Fragments;   /* List of fragments. */

	    public UInt64   ParentInode;                  /* The Inode number of the parent directory. */

	    public ItemStruct ParentDirectory;

	    public Boolean      Directory;                    /* YES: it's a directory. */
	    public Boolean      Unmovable;                    /* YES: file can't/couldn't be moved. */
	    public Boolean      Exclude;                      /* YES: file is not to be defragged/optimized. */
	    public Boolean      SpaceHog;                     /* YES: file to be moved to end of disk. */
    };

    /* List of clusters used by the MFT. */

    public class ExcludesStruct
    {
        public UInt64 Start;
        public UInt64 End;
    };

    public class MSDefragDataStruct
    {
	    public UInt16 Phase;                             /* The current Phase (1...3). */
        UInt16 Zone;                              /* The current Zone (0..2) for Phase 3. */
	    public Boolean Running;                          /* If not RUNNING then stop defragging. */
	    public int RedrawScreen;                     /* 0:no, 1:request, 2: busy. */
	    public Boolean UseLastAccessTime;                /* If TRUE then use LastAccessTime for SpaceHogs. */
	    public int CannotMoveDirs;                    /* If bigger than 20 then do not move dirs. */

	    public String IncludeMask;                    /* Example: "c:\t1\*" */
	    public DiskStruct Disk;

	    public UInt16 FreeSpace;                      /* Percentage of total disk size 0..100. */

	    /* Tree in memory with information about all the files. */

	    public ItemStruct ItemTree;
	    public int BalanceCount;
	    public List<String> Excludes;                      /* Array with exclude masks. */
	    public Boolean UseDefaultSpaceHogs;              /* TRUE: use the built-in SpaceHogs. */
	    public List<String> SpaceHogs;                     /* Array with SpaceHog masks. */
	    public UInt64[] Zones/*[4]*/ = new UInt64[4];                      /* Begin (LCN) of the zones. */

	    public List<ExcludesStruct> MftExcludes/*[3]*/;  /* List of clusters reserved for the MFT. */

	    /* Counters filled before Phase 1. */

        public UInt64 TotalClusters;                 /* Size of the volume, in clusters. */
        public UInt64 BytesPerCluster;               /* Number of bytes per cluster. */

	    /* Counters updated before/after every Phase. */

        public UInt64 CountFreeClusters;             /* Number of free clusters. */
        public UInt64 CountGaps;                     /* Number of gaps. */
        public UInt64 BiggestGap;                    /* Size of biggest gap, in clusters. */
        public UInt64 CountGapsLess16;               /* Number of gaps smaller than 16 clusters. */
        public UInt64 CountClustersLess16;           /* Number of clusters in gaps that are smaller than 16 clusters. */

	    /* Counters updated after every Phase, but not before Phase 1 (analyze). */

        public UInt64 CountDirectories;              /* Number of analysed subdirectories. */
        public UInt64 CountAllFiles;                 /* Number of analysed files. */
        public UInt64 CountFragmentedItems;          /* Number of fragmented files. */
        public UInt64 CountAllBytes;                 /* Bytes in analysed files. */
        public UInt64 CountFragmentedBytes;          /* Bytes in fragmented files. */
        public UInt64 CountAllClusters;              /* Clusters in analysed files. */
        public UInt64 CountFragmentedClusters;       /* Clusters in fragmented files. */
	    double AverageDistance;                /* Between end and begin of files. */

	    /* Counters used to calculate the percentage of work done. */

        public UInt64 PhaseTodo;                     /* Number of items to do in this Phase. */
        public UInt64 PhaseDone;                     /* Number of items already done in this Phase. */

	    /* Variables used to throttle the speed. */

	    public int Speed;                            /* Speed as a percentage 1..100. */
        public Int64 StartTime;
        public Int64 RunningTime;
        public Int64 LastCheckpoint;

	    /* The array with error messages. */
	    List<String> DebugMsg;
    }
}
