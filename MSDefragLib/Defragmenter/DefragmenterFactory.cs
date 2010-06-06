using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MSDefragLib.Defragmenter;

namespace MSDefragLib
{
    public class DefragmenterFactory
    {
        public static IDefragmenter Create(EnumDefragType defragType)
        {
            IDefragmenter Defragmenter = null;

            switch (defragType)
            {
                case EnumDefragType.defragTypeDefragmentation:
                    Defragmenter = new DiskDefragmenter();
                    break;
                default:
                    Defragmenter = new SimulationDefragmenter();
                    break;
            }

            return Defragmenter;
        }
    }

    public enum EnumDefragType
    {
        defragTypeDefragmentation = 0,
        defragTypeSimulation
    }

}
