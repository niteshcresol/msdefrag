using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class DiskStruct
    {
        public IntPtr VolumeHandle
        {
            get;
            set;
        }

        public String MountPoint;           /* Example: "c:" */

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
    };

}
