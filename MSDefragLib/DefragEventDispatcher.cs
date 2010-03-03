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
            changedClusters = new List<ClusterStructure>();
        }

        #endregion

        #region EventDispatcher public functions

        public void UpdateProgress(Double progress, Double all)
        {
            progressStatus = (all != 0) ? progress / all : 0.0;
        }

        public void AddChangedClusters(IList<ClusterStructure> clusters)
        {
            lock (changedClusters)
            {
                foreach (ClusterStructure cluster in clusters)
                {
                    changedClusters.Add(cluster);
                }
            }
        }

        public void AddLogMessage(Int16 level, String message)
        {
        }

        #endregion

        #region Events

        public event ProgressHandler ProgressEvent;
        public event UpdateDiskMapHandler UpdateDiskMapEvent;

        public void StartEventDispatcher()
        {
            while (true)
            {
                Thread.Sleep(300);

                SendProgressEvent();
                SendLogMessages();
                UpdateDiskMap();
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
                    IList<ClusterStructure> oldList = changedClusters;

                    changedClusters = new List<ClusterStructure>();
                    ChangedClusterEventArgs e = new ChangedClusterEventArgs(oldList);

                    UpdateDiskMapEvent(this, e);
                }
            }
        }

        private void SendLogMessages()
        {
        }

        #endregion

        #region Variables

        Double progressStatus;
        List<ClusterStructure> changedClusters;

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
        public IList<ClusterStructure> m_list;

        public ChangedClusterEventArgs(IList<ClusterStructure> list)
        {
            m_list = list;
        }
    }

    #endregion
}
