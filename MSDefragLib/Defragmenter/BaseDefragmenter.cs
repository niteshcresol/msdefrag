using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.Defragmenter
{
    public abstract class BaseDefragmenter : IDefragmenter
    {
        #region IDefragmenter Members

        public abstract void Start(string parameter);
        public abstract void Stop(int timeoutMs);

        public abstract int NumClusters { get; set; }

        public abstract event ClustersModifiedHandler ClustersModified;
        public abstract event LogMessageHandler LogMessage;
        public abstract event ProgressHandler Progress;

        #endregion
    }
}
