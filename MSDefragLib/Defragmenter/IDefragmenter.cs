using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public delegate void UpdateDiskMapHandler(object sender, EventArgs e);
    public delegate void UpdateFilteredDiskMapHandler(object sender, EventArgs e);
    //public delegate void LogMessageHandler(object sender, EventArgs e);
    public delegate void ProgressHandler(object sender, EventArgs e);

    public interface IDefragmenter
    {
        event UpdateDiskMapHandler UpdateDiskMapEvent;
        event UpdateFilteredDiskMapHandler UpdateFilteredDiskMapEvent;
        //event LogMessageHandler LogMessage;
        event ProgressHandler ProgressEvent;

        void StartDefragmentation(String parameter);
        void StopDefragmentation(Int32 timeoutMs);

        UInt64 NumClusters { get; set; }

        DefragEventDispatcher defragEventDispatcher { get; set; }
    }
}
