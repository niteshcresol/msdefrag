using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.Defragmenter
{
    internal class DiskDefragmenter : BaseDefragmenter
    {
        private MSDefragLib lib = new MSDefragLib();

        #region IDefragmenter Members

        public override event ClustersModifiedHandler ClustersModified
        {
            add
            {
                lib.ShowChangedClustersEvent += value;
            }
            remove
            {
                lib.ShowChangedClustersEvent -= value;
            }
        }

        public override event NewMessageHandler NewMessage
        {
            add
            {
                lib.ShowDebugEvent += value;
            }
            remove
            {
                lib.ShowDebugEvent -= value;
            }
        }

        public override void Start(string parameter)
        {
            lib.RunJkDefrag(@"C:\*", 2, 10, null, null);
            //lib.RunJkDefrag(@"T:\*", 2, 10, null, null);
        }

        public override void Stop(Int32 timeoutMs)
        {
            if ((lib.Data != null) && (lib.Data.Running == RunningState.RUNNING))
            {
                lib.StopJkDefrag(timeoutMs);
            }
        }

        public override int NumSquares
        {
            get
            {
                return lib.NumSquares;
            }
            set
            {
                lib.NumSquares = value;
            }
        }

        #endregion
    }
}
