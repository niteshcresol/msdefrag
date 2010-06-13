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
            filteredClusters = new List<MapClusterState>();
            logMessages = new List<LogMessage>();
        }

        #endregion

        #region EventDispatcher public functions

        public void UpdateProgress(Double progress, Double all)
        {
            progressStatus = (all != 0) ? progress / all : 0.0;
        }

        public void AddFilteredClusters(IList<MapClusterState> clusters)
        {
            if (clusters == null) return;

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
            lock (logMessages)
            {
                LogMessage logMessage = new LogMessage(level, message);
                logMessages.Add(logMessage);
            }
        }

        public Int16 NumberFilteredClusters
        {
            get;
            set;
        }

        #endregion

        #region Events

        public void StartEventDispatcher()
        {
            Pause = false;
            Continue = false;

            try
            {
                Int16 waitTime = 0;
                Int16 MaxWaitTime = 999;

                Int16 SleepTimer = 300;
                Int16 MinCluster = 400;

                while (true)
                {
                    Thread.Sleep(SleepTimer);

                    SendProgressEvent();
                    SendLogMessages();

                    if (filteredClusters.Count > MinCluster || waitTime > MaxWaitTime)
                    {
                        UpdateFilteredDiskMap();

                        waitTime = 0;
                    }
                    else
                    {
                        waitTime += SleepTimer;
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        public event EventHandler<ProgressEventArgs> ProgressEvent;
        public event EventHandler<FilteredClusterEventArgs> UpdateFilteredDiskMapEvent;
        public event EventHandler<LogMessagesEventArgs> UpdateLogMessagesEvent;

        private void SendProgressEvent()
        {
            ProgressEventArgs e = new ProgressEventArgs(progressStatus);

            if (ProgressEvent != null)
            {
                ProgressEvent(this, e);
            }
        }

        private void UpdateFilteredDiskMap()
        {
            if (Continue == true)
            {
                //filteredClusters = new List<MapClusterState>();

                Pause = false;
                Continue = false;
            }

            if (Pause == true)
            {
                filteredClusters = new List<MapClusterState>();
                return;
            }

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
            lock (logMessages)
            {
                if (logMessages.Count > 0)
                {
                    IList<LogMessage> oldList = logMessages;

                    logMessages = new List<LogMessage>();
                    LogMessagesEventArgs e = new LogMessagesEventArgs(oldList);

                    UpdateLogMessagesEvent(this, e);
                }
            }
        }

        #endregion

        #region Variables

        private Double progressStatus;
        private List<MapClusterState> filteredClusters;
        private List<LogMessage> logMessages;

        public Boolean Pause;
        public Boolean Continue;

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
    public class FilteredClusterEventArgs : EventArgs
    {
        private IList<MapClusterState> clusters;

        public IList<MapClusterState> Clusters
        {
            get { return clusters; }
        }

        public FilteredClusterEventArgs(IList<MapClusterState> list)
        {
            clusters = list;
        }
    }
    public class LogMessagesEventArgs : EventArgs
    {
        public LogMessagesEventArgs(IList<LogMessage> list)
        {
            messages = list;
        }

        private IList<LogMessage> messages;

        public IList<LogMessage> Messages
        {
            get { return messages; }
        }
    }

    #endregion
}
