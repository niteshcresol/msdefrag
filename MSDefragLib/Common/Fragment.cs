using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    /* List in memory of the fragments of a file. */
    public class Fragment
    {
        public const UInt64 VIRTUALFRAGMENT = UInt64.MaxValue;

        /// <summary>
        /// Logical cluster number, location on disk.
        /// </summary>
        public UInt64 Lcn;

        /// <summary>
        /// Virtual cluster number of next fragment.
        /// </summary>
        public UInt64 NextVcn;
    };
}
