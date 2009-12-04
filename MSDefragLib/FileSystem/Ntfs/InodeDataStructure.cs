using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// An inode is the filesystems representation of a file, directory, device,
    /// etc. In NTFS every inode it represented by an MFT FILE record
    /// </summary>
    public class InodeDataStructure
    {
        public UInt64 m_iNode;                          /* The m_iNode number. */
        public UInt64 m_parentInode;                    /* The m_iNode number of the parent directory. */

        /* true: it's a directory. */
        public Boolean IsDirectory
        { get; set; }

        /* Long filename. */
        public String LongFilename
        { get; private set; }

        /* Short filename (8.3 DOS). */
        public String ShortFilename
        { get; private set; }

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

        /// <summary>
        /// Length of $MFT::$DATA, can be less than what is told by the fragments
        /// </summary>
        public UInt64 MftDataLength
        { get; set; }

        /// <summary>
        /// The Fragments of the $MFT::$BITMAP stream.
        /// </summary>
        public FragmentList MftBitmapFragments
        { get; set; }

        /// <summary>
        /// Length of $MFT::$BITMAP, can be less than what is told by the fragments
        /// </summary>
        public UInt64 MftBitmapLength
        { get; set; }

        /// <summary>
        /// Initialize the inode structure
        /// </summary>
        /// <param name="inodeNumber"></param>
        public InodeDataStructure(UInt64 inodeNumber)
        {
            m_iNode = inodeNumber;
            m_parentInode = 5;
            IsDirectory = false;

            IsDirectory = true;

            LongFilename = null;
            ShortFilename = null;
            m_creationTime = 0;
            m_mftChangeTime = 0;
            m_lastAccessTime = 0;
            m_totalBytes = 0;
            Streams = new StreamList();
            MftDataFragments = null;
            MftDataLength = 0;
            MftBitmapFragments = null;
            MftBitmapLength = 0;
        }

        /// <summary>
        /// Save the filename in either the Long or the Short filename. We only
        /// save the first filename, any additional filenames are hard links. They
        /// might be useful for an optimization algorithm that sorts by filename,
        /// but which of the hardlinked names should it sort? So we only store the
        /// first filename.
        /// </summary>
        /// <param name="attribute"></param>
        public void AddName(FileNameAttribute attribute)
        {
            switch (attribute.NameType)
            {
                case NameType.DOS:
                    ShortFilename = ShortFilename ?? attribute.Name;
                    break;
                case NameType.NTFS | NameType.DOS:
                case NameType.NTFS:
                    LongFilename = LongFilename ?? attribute.Name;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
