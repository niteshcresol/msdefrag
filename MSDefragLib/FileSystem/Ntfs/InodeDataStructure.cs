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
        /// <summary>
        /// Initialize the inode structure
        /// </summary>
        /// <param name="inodeNumber"></param>
        public InodeDataStructure(UInt64 inodeNumber)
        {
            Inode = inodeNumber;
            ParentInode = 5;
            IsDirectory = false;

            IsDirectory = true;

            LongFilename = null;
            ShortFilename = null;
            CreationTime = 0;
            MftChangeTime = 0;
            LastAccessTime = 0;
            TotalBytes = 0;
            Streams = new StreamList();
            MftDataFragments = null;
            MftDataLength = 0;
            MftBitmapFragments = null;
            MftBitmapLength = 0;
        }

        /* The Inode number. */
        public UInt64 Inode
        { get; private set; }

        /* The m_iNode number of the parent directory. */
        public UInt64 ParentInode
        { get; set; }

        /* true: it's a directory. */
        public Boolean IsDirectory
        { get; set; }

        /* Long filename. */
        public String LongFilename
        { get; private set; }

        /* Short filename (8.3 DOS). */
        public String ShortFilename
        { get; private set; }

        /* Total number of bytes. */
        public UInt64 TotalBytes
        { get; set; }

        /* 1 second = 10000000 */
        public UInt64 CreationTime
        { get; set; }

        public UInt64 MftChangeTime
        { get; set; }

        public UInt64 LastAccessTime
        { get; set; }

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
                case NameTypes.DOS:
                    ShortFilename = ShortFilename ?? attribute.Name;
                    break;
                case NameTypes.NTFS | NameTypes.DOS:
                case NameTypes.NTFS:
                case NameTypes.POSIX:
                    LongFilename = LongFilename ?? attribute.Name;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
