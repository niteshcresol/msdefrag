using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSDefragLib
{
    public class DefragmenterFactory
    {
        public IDefragmenter defragmenter;

        private Thread defragThread = null;
        private Thread eventDispatcherThread = null;

        public void CreateSimulation()
        {
            defragmenter = new Defragmenter.SimulationDefragmenter();
        }

        public void Create()
        {
            defragmenter = new Defragmenter.DiskDefragmenter();
        }

        public void Start()
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
            defragmenter.Start(@"C:\*");
        }

        private void EventDispatcher()
        {
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        public void Stop(int timeoutMs)
        {
            defragmenter.Stop(5000);

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

        public UInt64 NumClusters
        {
            get { return defragmenter.NumClusters; }

            set {}
        }
    }
}
