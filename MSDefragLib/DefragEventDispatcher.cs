using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSDefragLib
{
    public class DefragEventDispatcher
    {
        #region Constructor

        public DefragEventDispatcher()
        {
            //progressStatus = 0.0;
            changedClusters = new List<ClusterState>();
            filteredClusters = new List<MapClusterState>();
        }

        #endregion

        #region EventDispatcher public functions

        public void UpdateProgress(Double progress, Double all)
        {
            progressStatus = (all != 0) ? progress / all : 0.0;
        }

        public void AddChangedClusters(IList<ClusterState> clusters)
        {
            lock (changedClusters)
            {
                foreach (ClusterState cluster in clusters)
                {
                    changedClusters.Add(cluster);
                }
            }
        }

        public void AddFilteredClusters(IList<MapClusterState> clusters)
        {
            lock (filteredClusters)
            {
                foreach (MapClusterState cluster in clusters)
                {
                    filteredClusters.Add(cluster);
                }
            }
        }

        //public void AddLogMessage(Int16 level, String message)
        //{
        //}

        public Int16 NumberFilteredClusters
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event EventHandler<ProgressEventArgs> ProgressEvent;
        //public event UpdateDiskMapEventHandler UpdateDiskMapEvent;
        public event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent;

        public void StartEventDispatcher()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(300);

                    SendProgressEvent();
                    //SendLogMessages();
                    //UpdateDiskMap();
                    UpdateFilteredDiskMap();
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        private void SendProgressEvent()
        {
            ProgressEventArgs e = new ProgressEventArgs(progressStatus);

            if (ProgressEvent != null)
            {
                ProgressEvent(this, e);
            }
        }

        //private void UpdateDiskMap()
        //{
        //    lock (changedClusters)
        //    {
        //        if (changedClusters.Count > 0)
        //        {
        //            IList<ClusterState> oldList = changedClusters;

        //            changedClusters = new List<ClusterState>();
        //            ChangedClusterEventArgs e = new ChangedClusterEventArgs(oldList);

        //            UpdateDiskMapEvent(this, e);
        //        }
        //    }
        //}

        private void UpdateFilteredDiskMap()
        {
            lock (filteredClusters)
            {
                if (filteredClusters.Count > 0)
                {
                    IList<MapClusterState> oldList = filteredClusters;

                    filteredClusters = new List<MapClusterState>();
                    FilteredClusterEventArgs e = new FilteredClusterEventArgs(oldList);

                    UpdateFilteredDiskMapEvent(this, e);
                }
            }
        }

        //private void SendLogMessages()
        //{
        //}

        #endregion

        #region Variables

        private Double progressStatus;
        private List<ClusterState> changedClusters;
        private List<MapClusterState> filteredClusters;

        #endregion
    }

    #region Event Classes

    public class ProgressEventArgs : EventArgs
    {
        private Double progress;

        public Double Progress
        {
            get { return progress; }
            set { progress = value; }
        }

        public ProgressEventArgs(Double value)
        {
            Progress = (Double)(value * 100);
        }
    }

    public class ChangedClusterEventArgs : EventArgs
    {
        private IList<ClusterState> clusters;

        public IList<ClusterState> Clusters
        {
            get
            {
                return clusters;
            }
        }

        public ChangedClusterEventArgs(IList<ClusterState> list)
        {
            clusters = list;
        }
    }

    public class FilteredClusterEventArgs : EventArgs
    {
        private IList<MapClusterState> clusters;

        public IList<MapClusterState> Clusters
        {
            get
            {
                return clusters;
            }
        }

        public FilteredClusterEventArgs(IList<MapClusterState> list)
        {
            clusters = list;
        }
    }

    #endregion
}
