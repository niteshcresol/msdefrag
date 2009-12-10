using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{

    /* List of clusters used by the MFT. */
    public class ExcludesStruct
    {
        public UInt64 Start;
        public UInt64 End;
    };

    public enum RunningState : int
    {
        STOPPED = 0,
        RUNNING,
        STOPPING
    }

    public class MSDefragDataStruct
    {
	    public UInt16 Phase;                             /* The current Phase (1...3). */
        public UInt16 Zone;                              /* The current Zone (0..2) for Phase 3. */
        public RunningState Running;                     /* If not RUNNING then stop defragging. */
	    public int RedrawScreen;                         /* 0:no, 1:request, 2: busy. */
	    public Boolean UseLastAccessTime;                /* If TRUE then use LastAccessTime for SpaceHogs. */
	    public int CannotMoveDirs;                       /* If bigger than 20 then do not move dirs. */

	    public String IncludeMask;                       /* Example: "c:\t1\*" */
	    public Disk Disk;

	    public UInt16 FreeSpace;                          /* Percentage of total disk size 0..100. */

	    /* Tree in memory with information about all the files. */

	    public ItemStruct ItemTree;
	    public int BalanceCount;
	    public List<String> Excludes;                     /* Array with exclude masks. */
	    public Boolean UseDefaultSpaceHogs;               /* TRUE: use the built-in SpaceHogs. */
	    public List<String> SpaceHogs;                    /* Array with SpaceHog masks. */
	    public UInt64[] Zones/*[4]*/ = new UInt64[4];     /* Begin (LCN) of the zones. */

	    public List<ExcludesStruct> MftExcludes/*[3]*/;   /* List of clusters reserved for the MFT. */

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
	    public double AverageDistance;                /* Between end and begin of files. */

	    /* Counters used to calculate the percentage of work done. */

        public UInt64 PhaseTodo;                     /* Number of items to do in this Phase. */
        public UInt64 PhaseDone;                     /* Number of items already done in this Phase. */

	    /* Variables used to throttle the speed. */

        public Int64 StartTime;

	    /* The array with error messages. */
	    List<String> DebugMsg;
    }
}
