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
        /* Return the location on disk (LCN, Logical Cluster Number) of an item. */
        public UInt64 Lcn
        {
            get
            {
                Fragment Fragment = FirstFragmentInList;
                while ((Fragment != null) && (Fragment.Lcn == Fragment.VIRTUALFRAGMENT))
                {
                    Fragment = Fragment.Next;
                }
                if (Fragment == null)
                    return 0;
                return Fragment.Lcn;
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
        public FragmentList FragmentList { get; set; }

        //HACK: for refactoring
        public Fragment FirstFragmentInList
        {
            get
            {
                if (FragmentList == null)
                    return null;
                return FragmentList._LIST;
            }
        }

        public UInt64 ParentInode;                  /* The Inode number of the parent directory. */

        public ItemStruct ParentDirectory;

        public Boolean Directory;                    /* YES: it's a directory. */
        public Boolean Unmovable;                    /* YES: file can't/couldn't be moved. */
        public Boolean Exclude;                      /* YES: file is not to be defragged/optimized. */
        public Boolean SpaceHog;                     /* YES: file to be moved to end of disk. */
    };
}
