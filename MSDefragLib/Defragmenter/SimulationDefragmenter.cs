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

        public override event ClustersModifiedHandler ClustersModified;
        public override event LogMessageHandler LogMessage;
        public override event ProgressHandler Progress;

        private DefragmenterState Data;

        public override void Start(string parameter)
        {
            Data = new DefragmenterState();

            Data.Running = RunningState.RUNNING;
            Data.TotalClusters = 40000000;

            List<eClusterState> clusterData = new List<eClusterState>((Int32)Data.TotalClusters);

            for (Int32 ii = 0; ii < (Int32)Data.TotalClusters; ii++)
            {
                clusterData.Add(eClusterState.Free);
            }

            Random rnd = new Random();

            Int32 maxNumTest = 450025;

            for (int testNumber = 0; testNumber < maxNumTest; testNumber++)
            {
                Int32 clusterBegin = rnd.Next((Int32)Data.TotalClusters);
                Int32 clusterEnd = rnd.Next(clusterBegin, clusterBegin + 50000);

                if (clusterEnd > (Int32)Data.TotalClusters)
                {
                    clusterEnd = (Int32)Data.TotalClusters;
                }

                if (Data.Running != RunningState.RUNNING)
                {
                    break;
                }

                eClusterState col = (eClusterState)rnd.Next((Int32)eClusterState.MaxValue);

                for (Int32 clusterNum = clusterBegin; (Data.Running == RunningState.RUNNING) && (clusterNum < clusterEnd); clusterNum++)
                {
                    clusterData[clusterNum] = col;
                }

                ShowChangedClusters(clusterBegin, clusterEnd);
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

        public override int NumClusters
        {
            get;
            set;
        }

        #endregion

        private void OnClustersModified(EventArgs e)
        {
            if (ClustersModified != null)
            {
                ClustersModified(this, e);
            }
        }

        private void OnLogMessage(EventArgs e)
        {
            if (LogMessage != null)
            {
                LogMessage(this, e);
            }
        }

        private void OnShowProgress(EventArgs e)
        {
            if (Progress != null)
            {
                Progress(this, e);
            }
        }

        public void ShowChangedClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
        }

        public void ShowDebug(UInt32 level, String output)
        {
            if (level < 6)
            {
                FileSystem.Ntfs.MSScanNtfsEventArgs e = new FileSystem.Ntfs.MSScanNtfsEventArgs(level, output);
                OnLogMessage(e);
            }
        }

        public void ShowProgress(Double progress, Double all)
        {
            ProgressEventArgs e = new ProgressEventArgs(progress, all);

            OnShowProgress(e);
        }
    }
}
