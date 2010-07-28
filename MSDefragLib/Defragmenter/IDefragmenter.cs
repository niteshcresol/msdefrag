using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public interface IDefragmenter
    {
        event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent;
        event EventHandler<LogMessagesEventArgs> LogMessageEvent;
        event EventHandler<ProgressEventArgs> ProgressEvent;

        void StartDefragmentation(String parameter);
        void StopDefragmentation(Int32 timeoutMs);
        void StartReparseThread(Int32 numClusters);

        DefragEventDispatcher defragEventDispatcher { get; set; }
        Int32 NumFilteredClusters { get; set; }

        void Pause();
        void Continue();
    }
}
