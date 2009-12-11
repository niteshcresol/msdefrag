using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class ItemTree
    {

        /// <summary>
        /// Delete the entire Item tree
        /// </summary>
        /// <param name="Top"></param>
        public static void Delete(ItemStruct Top)
        {
            if (Top == null) return;
            if (Top.Smaller != null)
                Delete(Top.Smaller);
            if (Top.Bigger != null)
                Delete(Top.Bigger);

            if ((Top.ShortPath != null) &&
                ((Top.LongPath == null) ||
                (Top.ShortPath != Top.LongPath)))
            {
                Top.ShortPath = null;
            }

            if ((Top.ShortFilename != null) &&
                ((Top.LongFilename == null) ||
                (Top.ShortFilename != Top.LongFilename)))
            {
                Top.ShortFilename = null;
            }

            Top.LongPath = null;
            Top.LongFilename = null;

            //while (Top.FirstFragmentInList != null)
            //{
            //    Fragment = Top.FragmentList._LIST.Next;

            //    //TODO: ???? What is this for???
            //    Top.FragmentList._LIST = Fragment;
            //}

            Top = null;
        }

        /// <summary>
        /// Return pointer to the last item in the tree (the last file on the volume).
        /// </summary>
        public static ItemStruct TreeBiggest(ItemStruct Top)
        {
	        if (Top == null)
                return null;

	        while (Top.Bigger != null)
                Top = Top.Bigger;

	        return Top;
        }

        /// <summary>
        /// If Direction=0 then return a pointer to the first file on the volume,
        /// if Direction=1 then the last file.
        /// </summary>
        /// <param name="Top"></param>
        /// <param name="Direction"></param>
        /// <returns></returns>
        public static ItemStruct TreeFirst(ItemStruct Top, int Direction)
        {
	        if (Direction == 0)
                return TreeSmallest(Top);

	        return TreeBiggest(Top);
        }

        /// <summary>
        /// Return pointer to the previous item in the tree.
        /// </summary>
        /// <param name="Here"></param>
        /// <returns></returns>
        public static ItemStruct TreePrev(ItemStruct Here)
        {
	        ItemStruct Temp;

	        if (Here == null)
                return Here;

	        if (Here.Smaller != null)
	        {
		        Here = Here.Smaller;

		        while (Here.Bigger != null) 
                    Here = Here.Bigger;

		        return Here;
	        }

	        do
	        {
		        Temp = Here;
		        Here = Here.Parent;
	        } while ((Here != null) && (Here.Smaller == Temp));

	        return Here;
        }

        /// <summary>
        /// Return pointer to the first item in the tree (the first file on the volume).
        /// </summary>
        /// <param name="Top"></param>
        /// <returns></returns>
        public static ItemStruct TreeSmallest(ItemStruct Top)
        {
            if (Top == null)
                return null;

            while (Top.Smaller != null)
                Top = Top.Smaller;

            return Top;
        }

        /// <summary>
        /// Return pointer to the next item in the tree.
        /// </summary>
        /// <param name="Here"></param>
        /// <returns></returns>
        public static ItemStruct TreeNext(ItemStruct Here)
        {
            ItemStruct Temp;

            if (Here == null) return null;

            if (Here.Bigger != null)
            {
                Here = Here.Bigger;

                while (Here.Smaller != null) Here = Here.Smaller;

                return (Here);
            }

            do
            {
                Temp = Here;
                Here = Here.Parent;
            } while ((Here != null) && (Here.Bigger == Temp));

            return Here;
        }

    }
}
