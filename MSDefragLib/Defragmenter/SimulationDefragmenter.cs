using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.Defragmenter
{
    internal class SimulationDefragmenter : BaseDefragmenter
    {
        #region IDefragmenter Members

        DefragEventDispatcher m_eventDispatcher;

        public override DefragEventDispatcher defragEventDispatcher
        {
            get
            {
                return m_eventDispatcher;
            }
            set
            {
                m_eventDispatcher = value;
            }
        }

        private DefragmenterState Data;
        DiskMap diskMap;

        public SimulationDefragmenter()
        {
            defragEventDispatcher = new DefragEventDispatcher();

            Data = new DefragmenterState();

            Data.Running = RunningState.RUNNING;
            Data.TotalClusters = 400000;

            diskMap = new DiskMap((Int32)Data.TotalClusters);
        }

        public override void Start(string parameter)
        {
            Random rnd = new Random();

            Int32 maxNumTest = 450025;

            for (int testNumber = 0; testNumber < maxNumTest; testNumber++)
            {
                UInt64 clusterBegin = (UInt64)(rnd.Next((Int32)Data.TotalClusters));
                UInt64 clusterEnd = (UInt64)(rnd.Next((Int32)clusterBegin, (Int32)clusterBegin + 50000));

                if (clusterEnd > Data.TotalClusters)
                {
                    clusterEnd = Data.TotalClusters;
                }

                if (Data.Running != RunningState.RUNNING)
                {
                    break;
                }

                eClusterState col = (eClusterState)rnd.Next((Int32)eClusterState.MaxValue);

                for (UInt64 clusterNum = clusterBegin; (Data.Running == RunningState.RUNNING) && (clusterNum < clusterEnd); clusterNum++)
                {
                    diskMap.SetClusterState(clusterNum, col);
                }

                //ShowChangedClusters(clusterBegin, clusterEnd);
                ShowFilteredClusters(clusterBegin, clusterEnd);
                ShowProgress(testNumber, maxNumTest);

                 Thread.Sleep(1);
            }

            Data.Running = RunningState.STOPPED;
        }

        public override void Stop(Int32 timeoutMs)
        {
            /* Sanity check. */
            if (Data.Running != RunningState.RUNNING)
                return;

            /* All loops in the library check if the Running variable is set to
            RUNNING. If not then the loop will exit. In effect this will stop
            the defragger. */
            Data.Running = RunningState.STOPPING;

            /* Wait for a maximum of TimeOut milliseconds for the defragger to stop.
            If TimeOut is zero then wait indefinitely. If TimeOut is negative then
            immediately return without waiting. */
            int TimeWaited = 0;

            while (TimeWaited <= timeoutMs)
            {
                if (Data.Running == RunningState.STOPPED)
                    break;

                Thread.Sleep(100);
                TimeWaited *= 100;
            }
        }

        public override void ResendAllClusters()
        {
            ShowChangedClusters(0, Data.TotalClusters);
        }

        public override UInt64 NumClusters
        {
            get { return Data.TotalClusters; }
            set {}
        }

        #endregion

        //private void OnLogMessage(EventArgs e)
        //{
        //    if (LogMessage != null)
        //    {
        //        LogMessage(this, e);
        //    }
        //}

        public void ShowChangedClusters(UInt64 clusterBegin, UInt64 clusterEnd)
        {
            IList<ClusterState> clusters = diskMap.GetClusters(clusterBegin, clusterEnd);

            defragEventDispatcher.AddChangedClusters(clusters);
        }

        public void ShowFilteredClusters(UInt64 clusterBegin, UInt64 clusterEnd)
        {
            IList<MapClusterState> clusters = diskMap.GetFilteredClusters(clusterBegin, clusterEnd);

            defragEventDispatcher.AddFilteredClusters(clusters);
        }

        public void ShowLogMessage(UInt32 level, String message)
        {
            //if (level < 6)
            //{
            //    FileSystem.Ntfs.MSScanNtfsEventArgs e = new FileSystem.Ntfs.MSScanNtfsEventArgs(level, output);
            //    OnLogMessage(e);
            //}
        }

        public void ShowProgress(Double progress, Double all)
        {
            defragEventDispatcher.UpdateProgress(progress, all);
        }
    }
}
