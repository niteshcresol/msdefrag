using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    /// <summary>
    /// List in memory of all the files on disk, sorted by LCN (Logical Cluster Number).
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

        /// <summary>
        /// Return true if the block in the item starting at Offset with Size clusters
        /// is fragmented, otherwise return false.
        /// 
        /// Note: this function does not ask Windows for a fresh list of fragments,
        ///       it only looks at cached information in memory.
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Offset"></param>
        /// <param name="Size"></param>
        /// <returns></returns>
        public Boolean IsFragmented(UInt64 Offset, UInt64 Size)
        {
            UInt64 FragmentBegin = 0;
            UInt64 FragmentEnd = 0;
            UInt64 NextLcn = 0;

            // Walk through all fragments. If a fragment is found where either the begin or the end of 
            // the fragment is inside the block then the file is fragmented and return true.

            foreach (Fragment fragment in FragmentList)
            {
                // Virtual fragments do not occupy space on disk and do not count as fragments.
                if (fragment.IsLogical == false)
                    continue;

                // Treat aligned fragments as a single fragment. Windows will frequently split files
                // in fragments even though they are perfectly aligned on disk, especially system 
                // files and very large files. The defragger treats these files as unfragmented.

                if ((NextLcn != 0) && (fragment.Lcn != NextLcn))
                {
                    // If the fragment is above the block then return false;
                    // the block is not fragmented and we don't have to scan any further.

                    if (FragmentBegin >= Offset + Size)
                        return false;

                    // If the first cluster of the fragment is above the first cluster of the block,
                    // or the last cluster of the fragment is before the last cluster of the block,
                    // then the block is fragmented, return true.

                    if ((FragmentBegin > Offset) || ((FragmentEnd - 1 >= Offset) && (FragmentEnd < Offset + Size)))
                    {
                        return true;
                    }

                    FragmentBegin = FragmentEnd;
                }

                FragmentEnd += fragment.Length;
                NextLcn = fragment.NextLcn;
            }

            // Handle the last fragment.
            if (FragmentBegin >= Offset + Size)
                return false;

            if ((FragmentBegin > Offset) || ((FragmentEnd - 1 >= Offset) && (FragmentEnd < Offset + Size)))
            {
                return true;
            }

            // Return false, the item is not fragmented inside the block.
            return false;
        }

        public String GetPath(Boolean shortPath)
        {
            String path = String.Empty;

            path = shortPath ? ShortFilename : LongFilename;

            if (String.IsNullOrEmpty(path))
            {
                path = shortPath ? LongFilename : ShortFilename;
            }

            if (String.IsNullOrEmpty(path))
            {
                path = String.Empty;
            }

            return path;
        }

        public String GetCompletePath(String MountPoint, Boolean shortPath)
        {
            String path = String.Empty;
            ItemStruct parent = ParentDirectory;

            while (parent != null)
            {
                path = parent.GetPath(shortPath) + "\\" + path;

                parent = parent.ParentDirectory;
            }

	        /* Append all the strings. */
	        path = MountPoint + "\\" + path;

            return path;
        }

        public ItemStruct Parent;              /* Parent item. */
        public ItemStruct Smaller;             /* Next smaller item. */
        public ItemStruct Bigger;              /* Next bigger item. */

        public String LongFilename;                /* Long filename. */
        public String LongPath;                    /* Full path on disk, long filenames. */
        public String ShortFilename;               /* Short filename (8.3 DOS). */
        public String ShortPath;                   /* Full path on disk, short filenames. */

        public UInt64 Bytes;                        /* Total number of bytes. */
        public UInt64 NumClusters;                     /* Total number of clusters. */
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
