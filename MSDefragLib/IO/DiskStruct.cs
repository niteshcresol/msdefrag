using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace MSDefragLib
{
    public class DiskStruct
    {
        public DiskStruct()
        {
            VolumeHandle = IntPtr.Zero;
        }

        public IntPtr VolumeHandle
        {
            get;
            private set;
        }

        private void Open(String path)
        {
            Close();
            VolumeHandle = IO.IOWrapper.OpenVolume(path);
        }

        public Boolean IsOpen
        {
            get
            {
                return VolumeHandle != IntPtr.Zero;
            }
        }

        private String _mountPoint;

        /* Example: "c:" */
        public String MountPoint
        {
            get
            {
                return _mountPoint;
            }
            set
            {
                _mountPoint = value;
                String root = Path.GetPathRoot(value);
                Open(root.Replace(@"\",""));
            }
        }
        
        public String MountPointSlash
        {
            // Example: "c:\"
            get { return MountPoint + @"\"; }
        }
        
        private String VolumeName;          /* Example: "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}" */

        public String VolumeNameSlash
        {
            // Example: "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}\"
            get { return VolumeName + @"\"; }
        }

        public DiskType Type;

        public UInt64 MftLockedClusters;    /* Number of clusters at begin of MFT that cannot be moved. */

        /// <summary>
        /// Read data from this disk starting at the given LCN
        /// </summary>
        /// <param name="lcn"></param>
        /// <param name="buffer">Buffer to copy the data into</param>
        /// <param name="start">Start index inside buffer</param>
        /// <param name="count">Number of bytes to read</param>
        public void ReadFromCluster(UInt64 lcn, Byte[] buffer, int start, int count)
        {
            Overlapped overlapped = IO.OverlappedBuilder.Get(lcn);
            int bytesRead = IO.IOWrapper.Read(VolumeHandle, buffer, start, count, overlapped);
            if (bytesRead != count)
            {
                //String errorMessage = IO.IOWrapper.GetMessage(Marshal.GetLastWin32Error());
                //ShowDebug(2, String.Format("      Error while reading Inode {0:G}: " + errorMessage, RefInode));
                throw new Exception("Could not read the data from disk!");
            }
        }

        public IO.IOWrapper.BitmapData VolumeBitmap
        {
            get
            {
                return IO.IOWrapper.GetVolumeMap(VolumeHandle);
            }
        }

        public void Close()
        {
            if (IsOpen)
                IO.IOWrapper.CloseHandle(VolumeHandle);
            VolumeHandle = IntPtr.Zero;
        }
    };

}
