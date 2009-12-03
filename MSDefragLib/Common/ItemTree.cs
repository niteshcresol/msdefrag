using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class ItemTree
    {

        /* Return pointer to the first item in the tree (the first file on the volume). */
        public static ItemStruct TreeSmallest(ItemStruct Top)
        {
            if (Top == null)
                return null;

            while (Top.Smaller != null)
                Top = Top.Smaller;

            return Top;
        }

        /* Return pointer to the next item in the tree. */
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
