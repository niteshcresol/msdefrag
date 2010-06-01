using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MSDefragLib.Defragmenter;

namespace MSDefragLib
{
    internal class SimulationLib
    {
        private BaseDefragmenter defragmenter;

        public SimulationLib(SimulationDefragmenter defrag)
        {
            defragmenter = defrag;

            Data = new DefragmenterState(10, null, null);

            Data.Running = RunningState.Running;
            Data.TotalClusters = 400000;

            diskMap = new DiskMap((UInt32)Data.TotalClusters);
        }

        public void RunSimulation()
        {
            Random rnd = new Random();

            UInt32 maxNumTest = 450025;

            for (UInt32 testNumber = 0; (Data.Running == RunningState.Running) && (testNumber < maxNumTest); testNumber++)
            {
                UInt32 clusterBegin = (UInt32)(rnd.Next((Int32)Data.TotalClusters));
                UInt32 clusterEnd = Math.Min((UInt32)(rnd.Next((Int32)clusterBegin, (Int32)clusterBegin + 50000)), (UInt32)Data.TotalClusters);

                eClusterState col = (eClusterState)rnd.Next((Int32)eClusterState.MaxValue);

                defragmenter.DisplayCluster(clusterBegin, clusterEnd, col);

                defragmenter.ShowProgress(testNumber, maxNumTest);

                Thread.Sleep(1);
            }

            Data.Running = RunningState.Stopped;
        }

        public void StopSimulation(Int32 timeoutMs)
        {
            // Sanity check

            if (Data.Running != RunningState.Running)
                return;

            // All loops in the library check if the Running variable is set to Running.
            // If not then the loop will exit. In effect this will stop the defragger.

            Data.Running = RunningState.Stopping;

            // Wait for a maximum of TimeOut milliseconds for the defragger to stop.
            // If TimeOut is zero then wait indefinitely.
            // If TimeOut is negative then immediately return without waiting.

            int TimeWaited = 0;

            while (TimeWaited <= timeoutMs)
            {
                if (Data.Running == RunningState.Stopped)
                    break;

                Thread.Sleep(100);

                TimeWaited += 100;
            }
        }

        public DefragmenterState Data { get; set; }

        public DiskMap diskMap;
    }
}
