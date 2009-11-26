using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace MSDefragLib
{
    /// <summary>
    /// Factory for the overlapped objects we need most often.
    /// </summary>
    public class OverlappedBuilder
    {
        /// <summary>
        /// Get a default overlapped object
        /// </summary>
        /// <returns></returns>
        public static Overlapped Get()
        {
            Overlapped _overlapped = new Overlapped();
            _overlapped.OffsetLow = 0;
            _overlapped.OffsetHigh = 0;
            _overlapped.EventHandleIntPtr = IntPtr.Zero;
            return _overlapped;
        }

        /// <summary>
        /// Get an overlapped object with corresponding to the given
        /// cluster for our I/O calls.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public static Overlapped Get(UInt64 cluster)
        {
            Overlapped _overlapped = new Overlapped();
            _overlapped.OffsetLow = (int)cluster;
            _overlapped.OffsetHigh = (int)(cluster >> 32);
            _overlapped.EventHandleIntPtr = IntPtr.Zero;
            return _overlapped;
        }
    }
}
