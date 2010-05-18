using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    //public delegate void UpdateDiskMapEventHandler(object sender, EventArgs e);
    //public delegate void UpdateFilteredDiskMapEventHandler(object sender, EventArgs e);
    //public delegate void LogMessageHandler(object sender, EventArgs e);
    //public delegate void ProgressEventHandler(object sender, EventArgs e);

    public interface IDefragmenter
    {
        //event UpdateDiskMapEventHandler UpdateDiskMapEvent;
        event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent;
        //event LogMessageHandler LogMessage;
        event EventHandler<ProgressEventArgs> ProgressEvent;

        void StartDefragmentation(String parameter);
        void StopDefragmentation(Int32 timeoutMs);

        UInt64 NumClusters { get; set; }

        DefragEventDispatcher defragEventDispatcher { get; set; }
    }
}
