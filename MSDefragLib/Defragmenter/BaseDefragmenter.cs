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

        public abstract int NumSquares { get; set; }

        public abstract event ShowChangedClustersHandler ShowChangedClustersEvent;

        public abstract event ShowDebugHandler ShowDebugEvent;

        #endregion
    }
}
