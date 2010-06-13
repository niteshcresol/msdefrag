using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    /// <summary>
    /// List in memory of the fragments of a file.
    /// Add the size of the fragment to the total number of clusters.
    /// There are two kinds of fragments: real and virtual.
    /// The latter do not occupy clusters on disk, but are information
    /// used by compressed and sparse files. 
    /// </summary>
    [DebuggerDisplay("Lcn={Lcn}, Vcn={Vcn}, Len={Length}")]
    public class Fragment
    {
        public const UInt64 VIRTUALFRAGMENT = UInt64.MaxValue;

        public Fragment(UInt64 lcn, UInt64 vcn, UInt64 length, Boolean isVirtual)
        {
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
        /// When representing the data runs of a file, the clusters are given
        /// virtual cluster numbers. Cluster zero refers to the first cluster
        /// of the file. The data runs map the VCNs to LCNs so that the file
        /// can be located on the volume. 
        /// </summary>
        public UInt64 Vcn
        { get; private set; }

        /// <summary>
        /// Length of this fragment in clusters
        /// </summary>
        public UInt64 Length
        { get; private set; }

        /// <summary>
        /// Virtual cluster number of next fragment.
        /// </summary>
        public UInt64 NextVcn
        { get { return Vcn + Length; } }

        /// <summary>
        /// Logical cluster number of next fragment.
        /// </summary>
        public UInt64 NextLcn
        { get { return Lcn + Length; } }
    };
}
