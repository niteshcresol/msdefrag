using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSDefragLib.Defragmenter
{
    public abstract class BaseDefragmenter : IDefragmenter
    {
        #region EventDispatcher

        public DefragEventDispatcher defragEventDispatcher { get; set; }

        public BaseDefragmenter()
        {
            defragEventDispatcher = new DefragEventDispatcher();
        }

        public void ShowLogMessage(Int16 level, String message)
        {
            defragEventDispatcher.AddLogMessage(level, message);
        }

        public void ShowFilteredClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
            IList<MapClusterState> clusters = diskMap.GetFilteredClusters(clusterBegin, clusterEnd);

            defragEventDispatcher.AddFilteredClusters(clusters);
        }

        public void ShowProgress(Double progress, Double all)
        {
            defragEventDispatcher.UpdateProgress(progress, all);
        }

        public abstract void ReparseClusters();
        public abstract void StopReparsingClusters();

        public void ResendAllClusters()
        {
            if (diskMap == null || defragEventDispatcher == null)
            {
                return;
            }

            //ReparseClusters();

            IList<MapClusterState> clusters = diskMap.GetAllFilteredClusters();
            defragEventDispatcher.AddFilteredClusters(clusters);
        }

        public void Pause()
        {
            defragEventDispatcher.Pause = true;
        }

        public void Continue()
        {
            defragEventDispatcher.Continue = true;

            ResendAllClusters();
        }

        #endregion

        #region Events

        public event EventHandler<ProgressEventArgs> ProgressEvent
        {
            add { defragEventDispatcher.ProgressEvent += value; }
            remove { defragEventDispatcher.ProgressEvent -= value; }
        }

        public event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent
        {
            add { defragEventDispatcher.UpdateFilteredDiskMapEvent += value; }
            remove { defragEventDispatcher.UpdateFilteredDiskMapEvent -= value; }
        }

        public event EventHandler<LogMessagesEventArgs> LogMessageEvent
        {
            add { defragEventDispatcher.UpdateLogMessagesEvent += value; }
            remove { defragEventDispatcher.UpdateLogMessagesEvent -= value; }
        }

        #endregion

        #region Threading

        private Thread defragThread;
        private Thread eventDispatcherThread;

        public abstract void BeginDefragmentation(string parameter);
        public abstract void FinishDefragmentation(int timeoutMS);

        public void StartDefragmentation(string parameter)
        {
            defragThread = new Thread(Defrag);
            defragThread.Name = "Defrag Engine";
            defragThread.Priority = ThreadPriority.Normal;

            defragThread.Start();

            eventDispatcherThread = new Thread(EventDispatcher);
            eventDispatcherThread.Name = "Defrag Event Dispatcher";
            eventDispatcherThread.Priority = ThreadPriority.Normal;

            eventDispatcherThread.Start();
        }

        public void StopDefragmentation(int timeoutMs)
        {
            FinishDefragmentation(5000);

            if (defragThread != null && defragThread.IsAlive)
            {
                defragThread.Interrupt();
                defragThread.Join();
            }

            if (eventDispatcherThread != null && eventDispatcherThread.IsAlive)
            {
                eventDispatcherThread.Interrupt();
                eventDispatcherThread.Join();
            }
        }

        private void Defrag()
        {
            BeginDefragmentation(@"C:\*");
        }

        private void EventDispatcher()
        {
            defragEventDispatcher.StartEventDispatcher();
        }

        #endregion

        #region DiskMap

        public abstract DiskMap diskMap { get; set; }

        public Int32 NumFilteredClusters
        {
            get
            {
                if (diskMap != null)
                    return diskMap.NumFilteredClusters;

                return 0;
            }

            set
            {
                if (diskMap != null)
                {
                    if (diskMap.NumFilteredClusters != value)
                    {
                        diskMap.NumFilteredClusters = value;
                        ReparseClusters();
                    }
                }
            }
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState newState)
        {
            if (diskMap == null)
            {
                return;
            }

            diskMap.SetClusterState(clusterBegin, clusterEnd, newState, defragEventDispatcher.Pause == false);

            ShowFilteredClusters(clusterBegin, clusterEnd);
        }

        public void SetClusterState(ItemStruct item, eClusterState newState)
        {
            if (diskMap == null)
            {
                return;
            }

            diskMap.AddCluster(item, newState);

            ShowFilteredClusters(0, diskMap.totalClusters);
        }

        public void ResetClusterStates()
        {
            if (diskMap == null)
            {
                return;
            }

            diskMap.ResetClusterStates();
        }

        #endregion
    }
}
