using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public delegate void ClustersModifiedHandler(object sender, EventArgs e);
    public delegate void NewMessageHandler(object sender, EventArgs e);

    public interface IDefragmenter
    {
        void Start(String parameter);
        void Stop(Int32 timeoutMs);

        Int32 NumSquares { get; set; }

        event ClustersModifiedHandler ClustersModified;
        event NewMessageHandler NewMessage;
    }
}
