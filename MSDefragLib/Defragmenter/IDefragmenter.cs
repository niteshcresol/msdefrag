using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public delegate void ClustersModifiedHandler(object sender, EventArgs e);
    public delegate void LogMessageHandler(object sender, EventArgs e);
    public delegate void ProgressHandler(object sender, EventArgs e);

    public interface IDefragmenter
    {
        void Start(String parameter);
        void Stop(Int32 timeoutMs);

        UInt64 NumClusters { get; set; }

        event ClustersModifiedHandler ClustersModified;
        event LogMessageHandler LogMessage;
        event ProgressHandler Progress;
    }
}
