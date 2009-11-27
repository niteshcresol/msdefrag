using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class InodeDataStructure
    {
        public UInt64 m_iNode;                          /* The m_iNode number. */
        public UInt64 m_parentInode;                    /* The m_iNode number of the parent directory. */

        public Boolean m_directory;                     /* true: it's a directory. */

        public String m_longFilename;                   /* Long filename. */
        public String m_shortFilename;                  /* Short filename (8.3 DOS). */

        public UInt64 m_totalBytes;                          /* Total number of bytes. */
        public UInt64 m_creationTime;                   /* 1 second = 10000000 */
        public UInt64 m_mftChangeTime;
        public UInt64 m_lastAccessTime;

        public StreamStructure m_streams;               /* List of StreamStruct. */
        public FragmentListStruct m_mftDataFragments;   /* The Fragments of the $MFT::$DATA stream. */

        public UInt64 m_mftDataLength;                   /* Length of the $MFT::$DATA. */

        public FragmentListStruct m_mftBitmapFragments; /* The Fragments of the $MFT::$BITMAP stream. */

        public UInt64 m_mftBitmapLength;                 /* Length of the $MFT::$BITMAP. */
    }
}
