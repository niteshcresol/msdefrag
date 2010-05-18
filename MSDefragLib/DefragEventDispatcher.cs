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
            progressStatus = 0.0;
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

        public void AddLogMessage(Int16 level, String message)
        {
        }

        public Int16 NumFilteredClusters
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event ProgressHandler ProgressEvent;
        public event UpdateDiskMapHandler UpdateDiskMapEvent;
        public event UpdateFilteredDiskMapHandler UpdateFilteredDiskMapEvent;

        public void StartEventDispatcher()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(300);

                    SendProgressEvent();
                    SendLogMessages();
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

        private void UpdateDiskMap()
        {
            lock (changedClusters)
            {
                if (changedClusters.Count > 0)
                {
                    IList<ClusterState> oldList = changedClusters;

                    changedClusters = new List<ClusterState>();
                    ChangedClusterEventArgs e = new ChangedClusterEventArgs(oldList);

                    UpdateDiskMapEvent(this, e);
                }
            }
        }

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

        private void SendLogMessages()
        {
        }

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
        public Double Progress;

        public ProgressEventArgs(Double progress)
        {
            Progress = (Double)(progress * 100);
        }
    }

    public class ChangedClusterEventArgs : EventArgs
    {
        public IList<ClusterState> m_list;

        public ChangedClusterEventArgs(IList<ClusterState> list)
        {
            m_list = list;
        }
    }

    public class FilteredClusterEventArgs : EventArgs
    {
        public IList<MapClusterState> m_list;

        public FilteredClusterEventArgs(IList<MapClusterState> list)
        {
            m_list = list;
        }
    }

    #endregion
}
