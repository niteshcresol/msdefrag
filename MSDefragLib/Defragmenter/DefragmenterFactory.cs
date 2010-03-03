using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSDefragLib
{
    public class DefragmenterFactory
    {
        public static IDefragmenter CreateSimulation()
        {
            return new Defragmenter.SimulationDefragmenter();
        }

        public static IDefragmenter Create()
        {
            return new Defragmenter.DiskDefragmenter();
        }
    }
}
