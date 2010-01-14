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
        public override event NewMessageHandler NewMessage;
        public override event ProgressHandler Progress;

        private DefragmenterState Data;
        private const Int32 MAX_DIRTY_SQUARES = 300;

        private IList<ClusterSquare> _dirtySquares = new List<ClusterSquare>(MAX_DIRTY_SQUARES);

        public override IList<ClusterSquare> DirtySquares
        {
            get
            {
                lock (_dirtySquares)
                {
                    IList<ClusterSquare> oldlist = _dirtySquares;
                    _dirtySquares = new List<ClusterSquare>(MAX_DIRTY_SQUARES);
                    return oldlist;
                }
            }
        }

        public override void Start(string parameter)
        {
            Data = new DefragmenterState();

            Data.Running = RunningState.RUNNING;

            Random rnd = new Random();

            Int32 maxNumTest = 450025;

            for (int testNumber = 0; testNumber < maxNumTest; testNumber++)
            {
                Int32 squareBegin = rnd.Next(NumSquares);
                Int32 squareEnd = rnd.Next(squareBegin, squareBegin + 10);

                if (squareEnd > NumSquares)
                {
                    squareEnd = NumSquares;
                }

                if (Data.Running != RunningState.RUNNING)
                {
                    break;
                }

                eClusterState col = (eClusterState)rnd.Next((Int32)eClusterState.MaxValue);

                for (Int32 squareNum = squareBegin; (Data.Running == RunningState.RUNNING) && (squareNum < squareEnd); squareNum++)
                {
                    ClusterSquare clusterSquare = new ClusterSquare(squareNum, 0, 20000);
                    clusterSquare.m_color = col;

                    lock (_dirtySquares)
                    {
                        _dirtySquares.Add(clusterSquare);

//                        ShowChangedClusters();
                    }
                }

                ShowProgress(testNumber, maxNumTest);

                //if (testNumber % 313 == 0)
                //{
                    //ShowDebug(4, "Test: " + testNumber);
                    //ShowDebug(5, String.Format("Done: {0:P}", (Double)((Double) testNumber / (Double) maxNumTest)));
                //}

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

        public override int NumSquares
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

        private void OnNewMessage(EventArgs e)
        {
            if (NewMessage != null)
            {
                NewMessage(this, e);
            }
        }

        private void OnShowProgress(EventArgs e)
        {
            if (Progress != null)
            {
                Progress(this, e);
            }
        }

        public void ShowChangedClusters()
        {
            if (_dirtySquares.Count() >= MAX_DIRTY_SQUARES)
            {
                ChangedClusterEventArgs e = new ChangedClusterEventArgs(DirtySquares);

                OnClustersModified(e);
            }
        }

        public void ShowDebug(UInt32 level, String output)
        {
            if (level < 6)
            {
                FileSystem.Ntfs.MSScanNtfsEventArgs e = new FileSystem.Ntfs.MSScanNtfsEventArgs(level, output);
                OnNewMessage(e);
            }
        }

        public void ShowProgress(Double progress, Double all)
        {
            ProgressEventArgs e = new ProgressEventArgs(progress, all);

            OnShowProgress(e);
        }
    }
}
