using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    //public delegate void LogMessageHandler(object sender, EventArgs e);

    public interface IDefragmenter
    {
        event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent;
        event EventHandler<LogMessagesEventArgs> LogMessageEvent;
        event EventHandler<ProgressEventArgs> ProgressEvent;

        void StartDefragmentation(String parameter);
        void StopDefragmentation(Int32 timeoutMs);

        void SetNumFilteredClusters(UInt32 num);

        UInt64 NumClusters { get; set; }

        DefragEventDispatcher defragEventDispatcher { get; set; }
    }
}
