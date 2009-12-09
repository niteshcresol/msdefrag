using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    /// <summary>
    /// List in memory of all the files on disk, sorted
    /// by LCN (Logical Cluster Number).
    /// </summary>
    public class ItemStruct
    {
        public ItemStruct(FileSystem.Ntfs.Stream stream)
        {
            FragmentList = stream.Fragments;
        }

        /// <summary>
        /// Return the location on disk (LCN, Logical
        /// Cluster Number) of an item.
        /// </summary>
        public UInt64 Lcn
        {
            get
            {
                return FragmentList.Lcn;
            }
        }

        /// <summary>
        /// Return the number of fragments in the item.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public int FragmentCount
        {
            get
            {
                return FragmentList.FragmentCount;
            }
        }



        public ItemStruct Parent;              /* Parent item. */
        public ItemStruct Smaller;             /* Next smaller item. */
        public ItemStruct Bigger;              /* Next bigger item. */

        public String LongFilename;                /* Long filename. */
        public String LongPath;                    /* Full path on disk, long filenames. */
        public String ShortFilename;               /* Short filename (8.3 DOS). */
        public String ShortPath;                   /* Full path on disk, short filenames. */

        public UInt64 Bytes;                        /* Total number of bytes. */
        public UInt64 Clusters;                     /* Total number of clusters. */
        public UInt64 CreationTime;                 /* 1 second = 10000000 */
        public UInt64 MftChangeTime;
        public UInt64 LastAccessTime;

        /* List of fragments. */
        public FragmentList FragmentList { get; private set; }

        public UInt64 ParentInode;                  /* The Inode number of the parent directory. */

        public ItemStruct ParentDirectory;

        public Boolean IsDirectory;                    /* YES: it's a directory. */
        public Boolean Unmovable;                    /* YES: file can't/couldn't be moved. */
        public Boolean Exclude;                      /* YES: file is not to be defragged/optimized. */
        public Boolean SpaceHog;                     /* YES: file to be moved to end of disk. */
    };
}
