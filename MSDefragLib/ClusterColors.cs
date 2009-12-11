using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public enum ClusterColors : int
    {
        COLOREMPTY = 0,
        COLORALLOCATED,
        COLORUNFRAGMENTED,
        COLORUNMOVABLE,
        COLORFRAGMENTED,
        COLORBUSY,
        COLORMFT,
        COLORSPACEHOG,
        COLORBACK,

        COLORMAX
    }
}
