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

        //public override event ClustersModifiedHandler ClustersModified
        //{
        //    add
        //    {
        //        lib.ShowChangedClustersEvent += value;
        //    }
        //    remove
        //    {
        //        lib.ShowChangedClustersEvent -= value;
        //    }
        //}

        //public override event LogMessageHandler LogMessage
        //{
        //    add
        //    {
        //        lib.LogMessageEvent += value;
        //    }
        //    remove
        //    {
        //        lib.LogMessageEvent -= value;
        //    }
        //}

        //public override event ProgressHandler Progress
        //{
        //    add
        //    {
        //        lib.ProgressEvent += value;
        //    }
        //    remove
        //    {
        //        lib.ProgressEvent -= value;
        //    }
        //}

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

        public override void ResendAllClusters()
        {
            lib.ResendAllClusters();
        }

        public override UInt64 NumClusters
        {
            get
            {
                return lib.Data.TotalClusters;
            }

            set {}
        }

        public override DefragEventDispatcher defragEventDispatcher
        {
            get
            {
                return defragEventDispatcher;
            }

            set {}
        }

        #endregion
    }
}
