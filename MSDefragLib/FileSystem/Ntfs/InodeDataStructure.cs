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

        /* true: it's a directory. */
        public Boolean IsDirectory
        { get; set; }

        public String m_longFilename;                   /* Long filename. */
        public String m_shortFilename;                  /* Short filename (8.3 DOS). */

        public UInt64 m_totalBytes;                          /* Total number of bytes. */
        public UInt64 m_creationTime;                   /* 1 second = 10000000 */
        public UInt64 m_mftChangeTime;
        public UInt64 m_lastAccessTime;

        /* List of StreamStruct. */
        public StreamList Streams
        { get; private set; }

        /// <summary>
        /// The Fragments of the $MFT::$DATA stream.
        /// </summary>
        public FragmentList MftDataFragments 
        { get; set; }

        public UInt64 m_mftDataLength;                   /* Length of the $MFT::$DATA. */

        /// <summary>
        /// The Fragments of the $MFT::$BITMAP stream.
        /// </summary>
        public FragmentList MftBitmapFragments
        { get; set; }

        public UInt64 m_mftBitmapLength;                 /* Length of the $MFT::$BITMAP. */

        public InodeDataStructure(UInt64 inodeNumber)
        {
            /* Initialize the InodeData struct. */
            m_iNode = inodeNumber;
            m_parentInode = 5;
            IsDirectory = false;

            IsDirectory = true;

            m_longFilename = null;
            m_shortFilename = null;
            m_creationTime = 0;
            m_mftChangeTime = 0;
            m_lastAccessTime = 0;
            m_totalBytes = 0;
            Streams = new StreamList();
            MftDataFragments = null;
            m_mftDataLength = 0;
            MftBitmapFragments = null;
            m_mftBitmapLength = 0;
        }

        public void AddName(FileNameAttribute fileNameAttribute)
        {
            if (!String.IsNullOrEmpty(fileNameAttribute.Name))
            {
                /* Extract the filename. */
                String p1 = fileNameAttribute.Name;

                /* Save the filename in either the Long or the Short filename. We only
                 * save the first filename, any additional filenames are hard links. They
                 * might be useful for an optimization algorithm that sorts by filename,
                 * but which of the hardlinked names should it sort? So we only store the
                 * first filename.*/
                switch (fileNameAttribute.NameType)
                {
                    case NameType.DOS:
                        if (m_shortFilename == null)
                        {
                            m_shortFilename = p1;
                            //ShowDebug(6, String.Format("    Short filename = '{0:G}'", p1));
                        }
                        break;
                    case NameType.NTFS | NameType.DOS:
                    case NameType.NTFS:
                        if (m_longFilename == null)
                        {
                            m_longFilename = p1;
                            //ShowDebug(6, String.Format("    Long filename = '{0:G}'", p1));
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}
