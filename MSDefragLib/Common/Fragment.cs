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

        public Fragment(UInt64 lcn, UInt64 vcn, UInt64 length, Boolean isVirtual)
        {
            //if (offset != 0)
            //{
            //    ShowDebug(6, String.Format("    Extent: Lcn={0:G}, Vcn={1:G}, NextVcn={2:G}", Lcn, Vcn - RunLength.Value, Vcn));
            //}
            //else
            //{
            //    ShowDebug(6, String.Format("    Extent (virtual): Vcn={0:G}, NextVcn={1:G}", Vcn - RunLength.Value, Vcn));
            //}

            Length = length;
            Vcn = vcn;

            if (isVirtual)
                Lcn = VIRTUALFRAGMENT;
            else
                Lcn = lcn;
        }

        /// <summary>
        /// Is this a logical fragment or a virtual one
        /// </summary>
        public Boolean IsLogical
        { get { return Lcn != VIRTUALFRAGMENT; } }
        public Boolean IsVirtual
        { get { return Lcn == VIRTUALFRAGMENT; } }

        /// <summary>
        /// Logical cluster number, location on disk.
        /// </summary>
        public UInt64 Lcn
        { get; private set; }

        /// <summary>
        /// Virtual cluster number, offset from beginning of file.
        /// </summary>
        public UInt64 Vcn
        { get; private set; }

        /// <summary>
        /// Length of this fragment
        /// </summary>
        public UInt64 Length
        { get; private set; }

        /// <summary>
        /// Virtual cluster number of next fragment.
        /// </summary>
        public UInt64 NextVirtualCluster
        { get { return Vcn + Length; } }

        /// <summary>
        /// Logical cluster number of next fragment.
        /// </summary>
        public UInt64 NextLogicalCluster
        { get { return Lcn + Length; } }
    };
}
