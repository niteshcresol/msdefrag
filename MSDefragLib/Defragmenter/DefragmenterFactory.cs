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
        public static IDefragmenter Create()
        {
            IDefragmenter Defragmenter = new DiskDefragmenter();

            return Defragmenter;
        }
    }
}
