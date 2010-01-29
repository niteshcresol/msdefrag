using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSDefragLib.Defragmenter
{
    public abstract class BaseDefragmenter : IDefragmenter
    {
        #region IDefragmenter Members

        private Thread defragThread = null;
        private Thread eventDispatcherThread = null;

        public event ProgressHandler ProgressEvent
        {
            add { defragEventDispatcher.ProgressEvent += value; }
            remove { defragEventDispatcher.ProgressEvent -= value; }
        }
        public event UpdateDiskMapHandler UpdateDiskMapEvent
        {
            add { defragEventDispatcher.UpdateDiskMapEvent += value; }
            remove { defragEventDispatcher.UpdateDiskMapEvent -= value; }
        }

        public abstract void Start(string parameter);
        public abstract void Stop(int timeoutMs);

        public abstract UInt64 NumClusters { get; set; }

        public abstract void ResendAllClusters();

        public abstract DefragEventDispatcher defragEventDispatcher { get; set; }

        //public abstract event LogMessageHandler LogMessage;

        public void StartDefragmentation(string parameter)
        {
            defragThread = new Thread(Defrag);
            defragThread.Priority = ThreadPriority.Lowest;

            defragThread.Start();

            eventDispatcherThread = new Thread(EventDispatcher);
            eventDispatcherThread.Priority = ThreadPriority.Normal;

            eventDispatcherThread.Start();
        }

        private void Defrag()
        {
            Start(@"C:\*");
        }

        private void EventDispatcher()
        {
            defragEventDispatcher.StartEventDispatcher();
        }

        public void StopDefragmentation(int timeoutMs)
        {
            Stop(5000);

            if (defragThread.IsAlive)
            {
                try
                {
                    defragThread.Abort();
                }
                catch (System.Exception)
                {

                }

                while (defragThread.IsAlive)
                {
                    Thread.Sleep(1000);
                }
            }

            if (eventDispatcherThread.IsAlive)
            {
                try
                {
                    eventDispatcherThread.Abort();
                }
                catch (System.Exception)
                {

                }

                while (eventDispatcherThread.IsAlive)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        #endregion
    }
}
