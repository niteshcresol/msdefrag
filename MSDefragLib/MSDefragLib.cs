/*

The JkDefrag library.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

For the full text of the license see the "License lgpl.txt" file.

Jeroen C. Kessels
Internet Engineer
http://www.kessels.com/

*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSDefragLib;
using Microsoft.Win32;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using MSDefragLib.FileSystem.Ntfs;
using System.Timers;

namespace MSDefragLib
{
    public class MSDefragLib
    {
        public class STARTING_LCN_INPUT_BUFFER
        {
            public UInt64 StartingLcn;
        };

        private Scan m_scanNtfs;

        public MSDefragLib()
        {
            m_scanNtfs = new Scan(this);

            m_scanNtfs.ShowDebugEvent += new Scan.ShowDebugHandler(ScanNtfsEventHandler);
            //ShowDebugEvent += new ShowDebugHandler(ShowDebugEventHandler);
        }

        /*
            All the text strings used by the defragger library.
            Note: The RunJkDefrag() function call has a parameter where you can specify
            a different array. Do not change this default array, simply create a new
            array in your program and specify it as a parameter.
        */
        String []DefaultDebugMsg =
        {
	        /*  0 */   "",
	        /*  1 */   "",
	        /*  2 */   "",
	        /*  3 */   "",
	        /*  4 */   "",
	        /*  5 */   "",
	        /*  6 */   "",
	        /*  7 */   "",
	        /*  8 */   "",
	        /*  9 */   "",
	        /* 10 */   "Getting cluster bitmap: %s",
	        /* 11 */   "Extent: Lcn=%I64u, Vcn=%I64u, NextVcn=%I64u",
	        /* 12 */   "ERROR: could not get volume bitmap: %s",
	        /* 13 */   "Gap found: LCN=%I64d, Size=%I64d",
	        /* 14 */   "Processing '%s'",
	        /* 15 */   "Could not open '%s': %s",
	        /* 16 */   "%I64d clusters at %I64d, %I64d bytes",
	        /* 17 */   "Special file attribute: Compressed",
	        /* 18 */   "Special file attribute: Encrypted",
	        /* 19 */   "Special file attribute: Offline",
	        /* 20 */   "Special file attribute: Read-only",
	        /* 21 */   "Special file attribute: Sparse-file",
	        /* 22 */   "Special file attribute: Temporary",
	        /* 23 */   "Analyzing: %s",
	        /* 24 */   "",
	        /* 25 */   "Cannot move file away because no gap is big enough: %I64d[%I64d]",
	        /* 26 */   "Don't know which file is at the end of the gap: %I64d[%I64d]",
	        /* 27 */   "Enlarging gap %I64d[%I64d] by moving %I64d[%I64d]",
	        /* 28 */   "Skipping gap, cannot fill: %I64d[%I64d]",
	        /* 29 */   "Opening volume '%s' at mountpoint '%s'",
	        /* 30 */   "",
	        /* 31 */   "Volume '%s' at mountpoint '%s' is not mounted.",
	        /* 32 */   "Cannot defragment volume '%s' at mountpoint '%s'",
	        /* 33 */   "MftStartLcn=%I64d, MftZoneStart=%I64d, MftZoneEnd=%I64d, Mft2StartLcn=%I64d, MftValidDataLength=%I64d",
	        /* 34 */   "MftExcludes[%u].Start=%I64d, MftExcludes[%u].End=%I64d",
	        /* 35 */   "",
	        /* 36 */   "Ignoring volume '%s' because it is read-only.",
	        /* 37 */   "Analyzing volume '%s'",
	        /* 38 */   "Finished.",
	        /* 39 */   "Could not get list of volumes: %s",
	        /* 40 */   "Cannot find volume name for mountpoint '%s': %s",
	        /* 41 */   "Cannot enlarge gap at %I64d[%I64d] because of unmovable data.",
	        /* 42 */   "Windows could not move the file, trying alternative method.",
	        /* 43 */   "Cannot process clustermap of '%s': %s",
	        /* 44 */   "Disk is full, cannot defragment.",
	        /* 45 */   "Alternative method failed, leaving file where it is.",
	        /* 46 */   "Extent (virtual): Vcn=%I64u, NextVcn=%I64u",
	        /* 47 */   "Ignoring volume '%s' because of exclude mask '%s'.",
	        /* 48 */   "Vacating %I64u clusters starting at LCN=%I64u",
	        /* 49 */   "Vacated %I64u clusters (until %I64u) from LCN=%I64u",
	        /* 50 */   "Finished vacating %I64u clusters, until LCN=%I64u",
	        /* 51 */   "",
	        /* 52 */   "",
	        /* 53 */   "I am fragmented.",
	        /* 54 */   "I am in MFT reserved space.",
	        /* 55 */   "I am a regular file in zone 1.",
	        /* 56 */   "I am a spacehog in zone 1 or 2.",
	        /* 57 */   "Ignoring volume '%s' because it is not a harddisk."
        };

        public enum CLUSTER_COLORS:int
        {
            COLOREMPTY = 0,
            COLORALLOCATED,
            COLORUNFRAGMENTED,
            COLORUNMOVABLE,
            COLORFRAGMENTED,
            COLORBUSY,
            COLORMFT,
            COLORSPACEHOG,
            COLORBACK,

            COLORMAX
        };

        /// <summary>
        /// Compare a string with a mask, case-insensitive. If it matches then return
        /// true, otherwise false. The mask may contain wildcard characters '?' (any
        /// character) '*' (any characters).
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="Mask"></param>
        /// <returns></returns>
        Boolean MatchMask(String Filename, String Mask)
        {
            Wildcard wildcard = new Wildcard(Mask, RegexOptions.IgnoreCase);

            if (wildcard.IsMatch(Filename)) return true;

            return false;

        }

        /* Search case-insensitive for a substring. */
/*
        char *JKDefragLib::stristr(char *Haystack, char *Needle)
        {
	        register char *p1;
	        register size_t i;

	        if ((Haystack == NULL) || (Needle == NULL)) return(NULL);

	        p1 = Haystack;
	        i = strlen(Needle);

	        while (*p1 != '\0')
	        {
		        if (_strnicmp(p1,Needle,i) == 0) return(p1);

		        p1++;
	        }

	        return(NULL);
        }
*/

        /* Search case-insensitive for a substring. */
/*
        WCHAR *JKDefragLib::stristrW(WCHAR *Haystack, WCHAR *Needle)
        {
	        WCHAR *p1;
	        size_t i;

	        if ((Haystack == NULL) || (Needle == NULL)) return(NULL);

	        p1 = Haystack;
	        i = wcslen(Needle);

	        while (*p1 != 0)
	        {
		        if (_wcsnicmp(p1,Needle,i) == 0) return(p1);

		        p1++;
	        }

	        return(NULL);
        }
*/

        /// <summary>
        /// Return a string with the error message for GetLastError().
        /// </summary>
        /// <param name="ErrorCode"></param>
        /// <returns></returns>
        public static String SystemErrorStr(Int32 ErrorCode)
        {
            return IO.IOWrapper.GetMessage(ErrorCode);
        }

        /* Dump a block of data to standard output, for debugging purposes. */
        public void ShowHex(MSDefragDataStruct Data, Array Buffer, UInt64 Count)
        {
            //JKDefragGui *jkGui = JKDefragGui::getInstance();

	        String s1;
	        String s2;

	        UInt64 i;
	        UInt64 j;

	        for (i = 0; i < Count; i = i + 16)
	        {
                s1 = String.Format("{0:C} {1:X}", i, i);

		        for (j = 0; j < 16; j++)
		        {
			        if (j == 8) s1 += " ";

			        if (j + i >= Count)
			        {
				        s1 += "   ";
			        }
			        else
			        {
                        s2 = String.Format("{0,2:X}", (UInt64)Buffer.GetValue((Int64)(i + j)));

                        s1 += s2;
			        }
		        }

                s1 += " ";

		        for (j = 0; j < 16; j++)
		        {
			        if (j + i >= Count)
			        {
                        s1 += " ";
			        }
			        else
			        {
                        if ((UInt64)Buffer.GetValue((Int64)(i + j)) < 32)
				        {
                            s1 += ".";
				        }
				        else
				        {
                            s2 = String.Format("{0,G}", (UInt64)Buffer.GetValue((Int64)(i + j)));

                            s1 += s2;
				        }
			        }
		        }

        //		jkGui->ShowDebug(2,NULL,L"%s",s1);
	        }
        }

        /*

        Add a string to a string array. If the array is empty then initialize, the
        last item in the array is NULL. If the array is not empty then append the
        new string, realloc() the array.

        */
/*
        WCHAR **JKDefragLib::AddArrayString(WCHAR **Array, WCHAR *NewString)
        {
	        WCHAR **NewArray;
	        int i;

	        / * Sanity check. * /
	        if (NewString == NULL) return(Array);

	        if (Array == NULL)
	        {
		        NewArray = (WCHAR **)malloc(2 * sizeof(WCHAR *));

		        if (NewArray == NULL) return(NULL);

		        NewArray[0] = _wcsdup(NewString);

		        if (NewArray[0] == NULL) return(NULL);

		        NewArray[1] = NULL;

		        return(NewArray);
	        }

	        i = 0;

	        while (Array[i] != NULL) i++;

	        NewArray = (WCHAR **)realloc(Array,(i + 2) * sizeof(WCHAR *));

	        if (NewArray == NULL) return(NULL);

	        NewArray[i] = _wcsdup(NewString);

	        if (NewArray[i] == NULL) return(NULL);

	        NewArray[i+1] = NULL;

	        return(NewArray);
        }
*/
        /* Subfunction of GetShortPath(). */
        void AppendToShortPath(ItemStruct Item, ref String Path)
        {
	        if (Item.ParentDirectory != null) AppendToShortPath(Item.ParentDirectory, ref Path);

            Path += "\\";

	        if (Item.ShortFilename != null)
	        {
                Path += Item.ShortFilename;
	        }
	        else if (Item.LongFilename != null)
	        {
                Path += Item.LongFilename;
	        }
        }

        /*

        Return a string with the full path of an item, constructed from the short names.
        Return NULL if error. The caller must free() the new string.

        */
        String GetShortPath(MSDefragDataStruct Data, ItemStruct Item)
        {
            Debug.Assert(Item != null);

            String Path = Data.Disk.MountPoint;

	        /* Append all the strings. */
	        AppendToShortPath(Item, ref Path);

	        return(Path);
        }

        /* Subfunction of GetLongPath(). */
        void AppendToLongPath(ItemStruct Item, ref String Path)
        {
	        if (Item.ParentDirectory != null)
                AppendToLongPath(Item.ParentDirectory, ref Path);

            Path += "\\";

	        if (Item.LongFilename != null)
	        {
                Path += Item.LongFilename;
	        }
	        else if (Item.ShortFilename != null)
	        {
                Path += Item.ShortFilename;
	        }
        }

        /*

        Return a string with the full path of an item, constructed from the long names.
        Return NULL if error. The caller must free() the new string.

        */
        String GetLongPath(MSDefragDataStruct Data, ItemStruct Item)
        {
	        Debug.Assert(Item != null);

            String Path = Data.Disk.MountPoint;

	        /* Append all the strings. */
	        AppendToLongPath(Item, ref Path);

	        return(Path);
        }

        /* Slow the program down. */
        public void SlowDown()
        {
	        /* Sanity check. */
	        Debug.Assert((Data.Speed > 0) && (Data.Speed <= 100));

	        /*
                Calculate the time we have to sleep so that the wall time is 100% and the
	            actual running time is the "-s" parameter percentage.
            */
            //_ftime64_s(&Time);
            DateTime Time = DateTime.Now;

            Int64 Now = Time.ToFileTime();
            //Now = Time.time * 1000 + Time.millitm;

            if (Now > Data.LastCheckpoint)
	        {
                Data.RunningTime += Now - Data.LastCheckpoint;
	        }

            if (Now < Data.StartTime) Data.StartTime = Now;    /* Should never happen. */

	        /* Sleep. */
            if (Data.RunningTime > 0)
	        {
                Int64 Delay = Data.RunningTime * 100 / Data.Speed - (Now - Data.StartTime);

		        if (Delay > 30000) Delay = 30000;

                if (Delay > 0) Thread.Sleep((Int32)Delay);
	        }

	        /* Save the current wall time, so next time we can calculate the time spent in	the program. */
            //_ftime64_s(&Time);
            Time = DateTime.Now;

            Data.LastCheckpoint = Time.ToFileTime();
            //Data.LastCheckpoint = Time.time * 1000 + Time.millitm;
        }

/*
        / *

        If Direction=0 then return a pointer to the next file on the volume,
        if Direction=1 then the previous file.

        * /
        struct ItemStruct *JKDefragLib::TreeNextPrev(struct ItemStruct *Here, int Direction)
        {
	        if (Direction == 0) return(TreeNext(Here));

	        return(TreePrev(Here));
        }
*/
        /* Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number). */
        public void TreeInsert(ItemStruct newItem)
        {
	        if (newItem == null) return;

            UInt64 NewLcn = newItem.Lcn;

	        /* Locate the place where the record should be inserted. */
            ItemStruct Here = Data.ItemTree;
            ItemStruct Ins = null;

            int Found = 1;

            UInt64 HereLcn;

            while (Here != null)
	        {
		        Ins = Here;
		        Found = 0;

		        HereLcn = Here.Lcn;

		        if (HereLcn > NewLcn)
		        {
			        Found = 1;
			        Here = Here.Smaller;
		        }
		        else
		        {
			        if (HereLcn < NewLcn) Found = -1;

			        Here = Here.Bigger;
		        }
	        }

	        /* Insert the record. */
	        newItem.Parent = Ins;
	        newItem.Smaller = null;
	        newItem.Bigger = null;

	        if (Ins == null)
	        {
		        Data.ItemTree = newItem;
	        }
	        else
	        {
		        if (Found > 0)
		        {
			        Ins.Smaller = newItem;
		        }
		        else
		        {
			        Ins.Bigger = newItem;
		        }
	        }

	        /* If there have been less than 1000 inserts then return. */
	        Data.BalanceCount ++;

	        if (Data.BalanceCount < 1000) return;

	        /*  Balance the tree.
	            It's difficult to explain what exactly happens here. For an excellent
	            tutorial see:
	            http://www.stanford.edu/~blp/avl/libavl.html/Balancing-a-BST.html  */

	        Data.BalanceCount = 0;

            /* Convert the tree into a vine. */
            ItemStruct A = Data.ItemTree;
            ItemStruct C = A;
            ItemStruct B;

            long Count = 0;

	        while (A != null)
	        {
		        /* If A has no Bigger child then move down the tree. */
                if (A.Bigger == null)
		        {
			        Count = Count + 1;
			        C = A;
			        A = A.Smaller;

			        continue;
		        }

		        /* Rotate left at A. */
		        B = A.Bigger;

		        if (Data.ItemTree == A) Data.ItemTree = B;

		        A.Bigger = B.Smaller;

                if (A.Bigger != null) A.Bigger.Parent = A;

		        B.Parent = A.Parent;

                if (B.Parent != null)
		        {
			        if (B.Parent.Smaller == A)
			        {
				        B.Parent.Smaller = B;
			        }
			        else
			        {
				        A.Parent.Bigger = B;
			        }
		        }

		        B.Smaller = A;
		        A.Parent = B;

		        /* Do again. */
		        A = B;
	        }

	        /* Calculate the number of skips. */
            long Skip = 1;

	        while (Skip < Count + 2) Skip = (Skip << 1);

	        Skip = Count + 1 - (Skip >> 1);

	        /* Compress the tree. */
            while (C != null)
	        {
		        if (Skip <= 0) C = C.Parent;

		        A = C;

		        while (A != null)
		        {
			        B = A;
			        A = A.Parent;

			        if (A == null) break;

			        /* Rotate right at A. */
                    if (Data.ItemTree == A) Data.ItemTree = B;

			        A.Smaller = B.Bigger;

			        if (A.Smaller != null) A.Smaller.Parent = A;

			        B.Parent = A.Parent;

                    if (B.Parent != null)
			        {
				        if (B.Parent.Smaller == A)
				        {
					        B.Parent.Smaller = B;
				        }
				        else
				        {
					        B.Parent.Bigger = B;
				        }
			        }

			        A.Parent = B;
			        B.Bigger = A;

			        /* Next item. */
			        A = B.Parent;

			        /* If there were skips then leave if all done. */
			        Skip = Skip - 1;
			        if (Skip == 0) break;
		        }
	        }
        }
/*
        / *

        Detach (unlink) a record from the tree. The record is not freed().
        See: http://www.stanford.edu/~blp/avl/libavl.html/Deleting-from-a-BST.html

        * /
        void JKDefragLib::TreeDetach(struct DefragDataStruct *Data, struct ItemStruct *Item)
        {
	        struct ItemStruct *B;

	        / * Sanity check. * /
	        if ((Data->ItemTree == NULL) || (Item == NULL)) return;

	        if (Item->Bigger == NULL)
	        {
		        / * It is trivial to delete a node with no Bigger child. We replace
		        the pointer leading to the node by it's Smaller child. In
		        other words, we replace the deleted node by its Smaller child. * /
		        if (Item->Parent != NULL)
		        {
			        if (Item->Parent->Smaller == Item)
			        {
				        Item->Parent->Smaller = Item->Smaller;
			        }
			        else
			        {
				        Item->Parent->Bigger = Item->Smaller;
			        }
		        }
		        else
		        {
			        Data->ItemTree = Item->Smaller;
		        }

		        if (Item->Smaller != NULL) Item->Smaller->Parent = Item->Parent;
	        }
	        else if (Item->Bigger->Smaller == NULL)
	        {
		        / * The Bigger child has no Smaller child. In this case, we move Bigger
		        into the node's place, attaching the node's Smaller subtree as the
		        new Smaller. * /
		        if (Item->Parent != NULL)
		        {
			        if (Item->Parent->Smaller == Item)
			        {
				        Item->Parent->Smaller = Item->Bigger;
			        }
			        else
			        {
				        Item->Parent->Bigger = Item->Bigger;
			        }
		        }
		        else
		        {
			        Data->ItemTree = Item->Bigger;
		        }

		        Item->Bigger->Parent = Item->Parent;
		        Item->Bigger->Smaller = Item->Smaller;

		        if (Item->Smaller != NULL) Item->Smaller->Parent = Item->Bigger;
	        }
	        else
	        {
		        / * Replace the node by it's inorder successor, that is, the node with
		        the smallest value greater than the node. We know it exists because
		        otherwise this would be case 1 or case 2, and it cannot have a Smaller
		        value because that would be the node itself. The successor can
		        therefore be detached and can be used to replace the node. * /

		        / * Find the inorder successor. * /
		        B = Item->Bigger;
		        while (B->Smaller != NULL) B = B->Smaller;

		        / * Detach the successor. * /
		        if (B->Parent != NULL)
		        {
			        if (B->Parent->Bigger == B)
			        {
				        B->Parent->Bigger = B->Bigger;
			        }
			        else
			        {
				        B->Parent->Smaller = B->Bigger;
			        }
		        }

		        if (B->Bigger != NULL) B->Bigger->Parent = B->Parent;

		        / * Replace the node with the successor. * /
		        if (Item->Parent != NULL)
		        {
			        if (Item->Parent->Smaller == Item)
			        {
				        Item->Parent->Smaller = B;
			        }
			        else
			        {
				        Item->Parent->Bigger = B;
			        }
		        }
		        else
		        {
			        Data->ItemTree = B;
		        }

		        B->Parent = Item->Parent;
		        B->Smaller = Item->Smaller;

		        if (B->Smaller != NULL) B->Smaller->Parent = B;

		        B->Bigger = Item->Bigger;

		        if (B->Bigger != NULL) B->Bigger->Parent = B;
	        }
        }
*/
        /* Delete the entire ItemTree. */
        public void DeleteItemTree(ItemStruct Top)
        {
	        if (Top == null) return;
            if (Top.Smaller != null) DeleteItemTree(Top.Smaller);
            if (Top.Bigger != null) DeleteItemTree(Top.Bigger);

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
/*
        / *

        Return the LCN of the fragment that contains a cluster at the LCN. If the
        item has no fragment that occupies the LCN then return zero.

        * /
        ULONG64 JKDefragLib::FindFragmentBegin(struct ItemStruct *Item, ULONG64 Lcn)
        {
	        struct FragmentListStruct *Fragment;
	        ULONG64 Vcn;

	        / * Sanity check. * /
	        if ((Item == NULL) || (Lcn == 0)) return(0);

	        / * Walk through all the fragments of the item. If a fragment is found
	        that contains the LCN then return the begin of that fragment. * /
	        Vcn = 0;
	        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
	        {
		        if (Fragment->Lcn != VIRTUALFRAGMENT)
		        {
			        if ((Lcn >= Fragment->Lcn) &&
				        (Lcn < Fragment->Lcn + Fragment->NextVcn - Vcn))
			        {
				        return(Fragment->Lcn);
			        }
		        }

		        Vcn = Fragment->NextVcn;
	        }

	        / * Not found: return zero. * /
	        return(0);
        }

        / *

        Search the list for the item that occupies the cluster at the LCN. Return a
        pointer to the item. If not found then return NULL.

        * /
        struct ItemStruct *JKDefragLib::FindItemAtLcn(struct DefragDataStruct *Data, ULONG64 Lcn)
        {
	        struct ItemStruct *Item;
	        ULONG64 ItemLcn;

	        / * Locate the item by descending the sorted tree in memory. If found then
	        return the item. * /
	        Item = Data->ItemTree;

	        while (Item != NULL)
	        {
		        ItemLcn = GetItemLcn(Item);

		        if (ItemLcn == Lcn) return(Item);

		        if (Lcn < ItemLcn)
		        {
			        Item = Item->Smaller;
		        }
		        else
		        {
			        Item = Item->Bigger;
		        }
	        }

	        / * Walk through all the fragments of all the items in the sorted tree. If a
	        fragment is found that occupies the LCN then return a pointer to the item. * /
	        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
	        {
		        if (FindFragmentBegin(Item,Lcn) != 0) return(Item);
	        }

	        / * LCN not found, return NULL. * /
	        return(NULL);
        }

        / *

        Open the item as a file or as a directory. If the item could not be
        opened then show an error message and return NULL.

        * /
        HANDLE JKDefragLib::OpenItemHandle(struct DefragDataStruct *Data, struct ItemStruct *Item)
        {
	        HANDLE FileHandle;

	        WCHAR ErrorString[BUFSIZ];
	        WCHAR *Path;

	        size_t Length;

	        Length = wcslen(Item->LongPath) + 5;

	        Path = (WCHAR *)malloc(sizeof(WCHAR) * Length);

	        swprintf_s(Path,Length,L"\\\\?\\%s",Item->LongPath);

	        if (Item->Directory == NO)
	        {
		        FileHandle = CreateFileW(Path,FILE_READ_ATTRIBUTES,
			        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
			        NULL,OPEN_EXISTING,FILE_FLAG_NO_BUFFERING,NULL);
	        }
	        else
	        {
		        FileHandle = CreateFileW(Path,GENERIC_READ,
			        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
			        NULL,OPEN_EXISTING,FILE_FLAG_BACKUP_SEMANTICS,NULL);
	        }

	        free(Path);

	        if (FileHandle != INVALID_HANDLE_VALUE) return(FileHandle);

	        / * Show error message: "Could not open '%s': %s" * /
	        SystemErrorStr(GetLastError(),ErrorString,BUFSIZ);

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //	jkGui->ShowDebug(4,NULL,Data->DebugMsg[15],Item->LongPath,ErrorString);

	        return(NULL);
        }

        / *

        Analyze an item (file, directory) and update it's Clusters and Fragments
        in memory. If there was an error then return NO, otherwise return YES.
        Note: Very small files are stored by Windows in the MFT and have no
        clusters (zero) and no fragments (NULL).

        * /
        int JKDefragLib::GetFragments(struct DefragDataStruct *Data, struct ItemStruct *Item, HANDLE FileHandle)
        {
	        STARTING_VCN_INPUT_BUFFER RetrieveParam;

	        struct
	        {
		        DWORD ExtentCount;
		        ULONG64 StartingVcn;
		        struct
		        {
			        ULONG64 NextVcn;
			        ULONG64 Lcn;
		        } Extents[1000];
	        } ExtentData;

	        BY_HANDLE_FILE_INFORMATION FileInformation;
	        ULONG64 Vcn;

	        struct FragmentListStruct *NewFragment;
	        struct FragmentListStruct *LastFragment;

	        DWORD ErrorCode;

	        WCHAR ErrorString[BUFSIZ];

	        int MaxLoop;

	        ULARGE_INTEGER u;

	        DWORD i;
	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Initialize. If the item has an old list of fragments then delete it. * /
	        Item->Clusters = 0;

	        while (Item->Fragments != NULL)
	        {
		        LastFragment = Item->Fragments->Next;

		        free(Item->Fragments);

		        Item->Fragments = LastFragment;
	        }

	        / * Fetch the date/times of the file. * /
	        if ((Item->CreationTime == 0) &&
		        (Item->LastAccessTime == 0) &&
		        (Item->MftChangeTime == 0) &&
		        (GetFileInformationByHandle(FileHandle,&FileInformation) != 0))
	        {
		        u.LowPart = FileInformation.ftCreationTime.dwLowDateTime;
		        u.HighPart = FileInformation.ftCreationTime.dwHighDateTime;

		        Item->CreationTime = u.QuadPart;

		        u.LowPart = FileInformation.ftLastAccessTime.dwLowDateTime;
		        u.HighPart = FileInformation.ftLastAccessTime.dwHighDateTime;

		        Item->LastAccessTime = u.QuadPart;

		        u.LowPart = FileInformation.ftLastWriteTime.dwLowDateTime;
		        u.HighPart = FileInformation.ftLastWriteTime.dwHighDateTime;

		        Item->MftChangeTime = u.QuadPart;
	        }

	        / * Show debug message: "Getting cluster bitmap: %s" * /
        //	jkGui->ShowDebug(4,NULL,Data->DebugMsg[10],Item->LongPath);

	        / * Ask Windows for the clustermap of the item and save it in memory.
	        The buffer that is used to ask Windows for the clustermap has a
	        fixed size, so we may have to loop a couple of times. * /
	        Vcn = 0;
	        MaxLoop = 1000;
	        LastFragment = NULL;

	        do
	        {
		        / * I strongly suspect that the FSCTL_GET_RETRIEVAL_POINTERS system call
		        can sometimes return an empty bitmap and ERROR_MORE_DATA. That's not
		        very nice of Microsoft, because it causes an infinite loop. I've
		        therefore added a loop counter that will limit the loop to 1000
		        iterations. This means the defragger cannot handle files with more
		        than 100000 fragments, though. * /
		        if (MaxLoop <= 0)
		        {
        //			jkGui->ShowDebug(2,NULL,L"FSCTL_GET_RETRIEVAL_POINTERS error: Infinite loop");

			        return(NO);
		        }

		        MaxLoop = MaxLoop - 1;

		        / * Ask Windows for the (next segment of the) clustermap of this file. If error
		        then leave the loop. * /
		        RetrieveParam.StartingVcn.QuadPart = Vcn;

		        ErrorCode = DeviceIoControl(FileHandle,FSCTL_GET_RETRIEVAL_POINTERS,
			        &RetrieveParam,sizeof(RetrieveParam),&ExtentData,sizeof(ExtentData),&w,NULL);

		        if (ErrorCode != 0)
		        {
			        ErrorCode = NO_ERROR;
		        }
		        else
		        {
			        ErrorCode = GetLastError();
		        }

		        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_MORE_DATA)) break;

		        / * Walk through the clustermap, count the total number of clusters, and
		        save all fragments in memory. * /
		        for (i = 0; i < ExtentData.ExtentCount; i++)
		        {
			        / * Show debug message. * /
			        if (ExtentData.Extents[i].Lcn != VIRTUALFRAGMENT)
			        {
				        / * "Extent: Lcn=%I64u, Vcn=%I64u, NextVcn=%I64u" * /
        //				jkGui->ShowDebug(4,NULL,Data->DebugMsg[11],ExtentData.Extents[i].Lcn,Vcn,
        //					ExtentData.Extents[i].NextVcn);
			        }
			        else
			        {
				        / * "Extent (virtual): Vcn=%I64u, NextVcn=%I64u" * /
        //				jkGui->ShowDebug(4,NULL,Data->DebugMsg[46],Vcn,ExtentData.Extents[i].NextVcn);
			        }

			        / * Add the size of the fragment to the total number of clusters.
			        There are two kinds of fragments: real and virtual. The latter do not
			        occupy clusters on disk, but are information used by compressed
			        and sparse files. * /
			        if (ExtentData.Extents[i].Lcn != VIRTUALFRAGMENT)
			        {
				        Item->Clusters = Item->Clusters + ExtentData.Extents[i].NextVcn - Vcn;
			        }

			        / * Add the fragment to the Fragments. * /
			        NewFragment = (struct FragmentListStruct *)malloc(sizeof(struct FragmentListStruct));

			        if (NewFragment != NULL)
			        {
				        NewFragment->Lcn = ExtentData.Extents[i].Lcn;
				        NewFragment->NextVcn = ExtentData.Extents[i].NextVcn;
				        NewFragment->Next = NULL;

				        if (Item->Fragments == NULL)
				        {
					        Item->Fragments = NewFragment;
				        }
				        else
				        {
					        if (LastFragment != NULL) LastFragment->Next = NewFragment;
				        }

				        LastFragment = NewFragment;
			        }

			        / * The Vcn of the next fragment is the NextVcn field in this record. * /
			        Vcn = ExtentData.Extents[i].NextVcn;
		        }

		        / * Loop until we have processed the entire clustermap of the file. * /
	        } while (ErrorCode == ERROR_MORE_DATA);

	        / * If there was an error while reading the clustermap then return NO. * /
	        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_HANDLE_EOF))
	        {
		        / * Show debug message: "Cannot process clustermap of '%s': %s" * /
		        SystemErrorStr(ErrorCode,ErrorString,BUFSIZ);

        //		jkGui->ShowDebug(3,NULL,Data->DebugMsg[43],Item->LongPath,ErrorString);

		        return(NO);
	        }

	        return(YES);
        }
        */

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
        Boolean IsFragmented(ItemStruct Item, UInt64 Offset, UInt64 Size)
        {
	        /*  Walk through all fragments. If a fragment is found where either the
             *  begin or the end of the fragment is inside the block then the file is
             *  fragmented and return YES. */
            UInt64 FragmentBegin = 0;
            UInt64 FragmentEnd = 0;
            UInt64 NextLcn = 0;
	        foreach (Fragment fragment in Item.FragmentList)
	        {
		        /* Virtual fragments do not occupy space on disk and do not count as fragments. */
		        if (fragment.IsLogical)
		        {
			        /* Treat aligned fragments as a single fragment. Windows will frequently
                     * split files in fragments even though they are perfectly aligned on disk,
                     * especially system files and very large files. The defragger treats these
                     * files as unfragmented. */
			        if ((NextLcn != 0) && (fragment.Lcn != NextLcn))
			        {
				        /* If the fragment is above the block then return NO, the block is
                         * not fragmented and we don't have to scan any further. */
				        if (FragmentBegin >= Offset + Size)
                            return false;

				        /* If the first cluster of the fragment is above the first cluster of
                         * the block, or the last cluster of the fragment is before the last
                         * cluster of the block, then the block is fragmented, return YES. */
				        if ((FragmentBegin > Offset) ||
					        ((FragmentEnd - 1 >= Offset) &&
					        (FragmentEnd - 1 < Offset + Size - 1)))
				        {
					        return true;
				        }

				        FragmentBegin = FragmentEnd;
			        }

			        FragmentEnd += fragment.Length;
			        NextLcn = fragment.NextLcn;
		        }
	        }

	        /* Handle the last fragment. */
	        if (FragmentBegin >= Offset + Size)
                return false;

	        if ((FragmentBegin > Offset) ||
		        ((FragmentEnd - 1 >= Offset) &&
		        (FragmentEnd - 1 < Offset + Size - 1)))
	        {
		        return true;
	        }

	        /* Return false, the item is not fragmented inside the block. */
	        return false;
        }

        /// <summary>
        /// Colorize an item (file, directory) on the screen in the proper color
        /// (fragmented, unfragmented, unmovable, empty). If specified then highlight
        /// part of the item. If Undraw=YES then remove the item from the screen.
        /// 
        /// NOTE:
        /// The offset and size of the highlight block is in absolute clusters,
        /// not virtual clusters.
        /// </summary>
        /// 
        /// <param name="Item"></param>
        /// <param name="BusyOffset">Number of first cluster to be highlighted.</param>
        /// <param name="BusySize">Number of clusters to be highlighted.</param>
        /// <param name="UnDraw">true to undraw the file from the screen.</param>
        public void ColorizeItem(ItemStruct Item, UInt64 BusyOffset, UInt64 BusySize, Boolean UnDraw)
        {
	        UInt64 SegmentBegin;
	        UInt64 SegmentEnd;

	        CLUSTER_COLORS Color;

            /* Determine if the item is fragmented. */
            Boolean Fragmented = IsFragmented(Item, 0, Item.Clusters);

	        /* Walk through all the fragments of the file. */
            UInt64 RealVcn = 0;

	        foreach (Fragment fragment in Item.FragmentList)
	        {
		        /* Ignore virtual fragments. They do not occupy space on disk and
                 * do not require colorization. */
		        if (fragment.IsVirtual)
			        continue;

                UInt64 Vcn = fragment.Vcn;

		        /* Walk through all the segments of the file. A segment is usually
                 * the same as a fragment, but if a fragment spans across a boundary
                 * then we must determine the color of the left and right parts
                 * individually. So we pretend the fragment is divided into segments
                 * at the various possible boundaries.*/
		        SegmentBegin = RealVcn;

                UInt64 maxSegment = RealVcn + fragment.Length;

                if (maxSegment > Data.TotalClusters)
                {
                    maxSegment = Data.TotalClusters;
                }

		        while (SegmentBegin < RealVcn + fragment.Length)
		        {
			        SegmentEnd = RealVcn + fragment.Length;

			        /* Determine the color with which to draw this segment. */
			        if (UnDraw == false)
			        {
                        Color = CLUSTER_COLORS.COLORUNFRAGMENTED;

                        if (Item.SpaceHog) Color = CLUSTER_COLORS.COLORSPACEHOG;
                        if (Fragmented) Color = CLUSTER_COLORS.COLORFRAGMENTED;
                        if (Item.Unmovable) Color = CLUSTER_COLORS.COLORUNMOVABLE;
                        if (Item.Exclude) Color = CLUSTER_COLORS.COLORUNMOVABLE;

				        if ((Vcn + SegmentBegin - RealVcn < BusyOffset) &&
					        (Vcn + SegmentEnd - RealVcn > BusyOffset))
				        {
					        SegmentEnd = RealVcn + BusyOffset - Vcn;
				        }

				        if ((Vcn + SegmentBegin - RealVcn >= BusyOffset) &&
					        (Vcn + SegmentBegin - RealVcn < BusyOffset + BusySize))
				        {
					        if (Vcn + SegmentEnd - RealVcn > BusyOffset + BusySize)
					        {
						        SegmentEnd = RealVcn + BusyOffset + BusySize - Vcn;
					        }

                            Color = CLUSTER_COLORS.COLORBUSY;
				        }
			        }
			        else
			        {
                        Color = CLUSTER_COLORS.COLOREMPTY;

				        for (int i = 0; i < 3; i++)
				        {
					        if ((fragment.Lcn + SegmentBegin - RealVcn < Data.MftExcludes[i].Start) &&
						        (fragment.Lcn + SegmentEnd - RealVcn > Data.MftExcludes[i].Start))
					        {
                                SegmentEnd = RealVcn + Data.MftExcludes[i].Start - fragment.Lcn;
					        }

                            if ((fragment.Lcn + SegmentBegin - RealVcn >= Data.MftExcludes[i].Start) &&
                                (fragment.Lcn + SegmentBegin - RealVcn < Data.MftExcludes[i].End))
					        {
                                if (fragment.Lcn + SegmentEnd - RealVcn > Data.MftExcludes[i].End)
						        {
                                    SegmentEnd = RealVcn + Data.MftExcludes[i].End - fragment.Lcn;
						        }

                                Color = CLUSTER_COLORS.COLORMFT;
					        }
				        }
			        }

			        /* Colorize the segment. */
                    DrawCluster(fragment.Lcn + SegmentBegin - RealVcn,
                        fragment.Lcn + SegmentEnd - RealVcn,Color);

			        /* Next segment. */
			        SegmentBegin = SegmentEnd;
		        }

		        /* Next fragment. */
		        RealVcn += fragment.Length;
	        }
        }

        /// <summary>
        /// Show a map on the screen of all the clusters on disk. The map shows
        /// which clusters are free and which are in use.
        ///  
        /// The Data->RedrawScreen flag controls redrawing of the screen. It is set
        /// to "2" (busy) when the subroutine starts. If another thread changes it to
        /// "1" (request) while the subroutine is busy then it will immediately exit
        /// without completing the redraw. When redrawing is completely finished the
        /// flag is set to "0" (no).
        /// </summary>
        /// <param name="Data"></param>
        public void ShowDiskmap()
        {
            ItemStruct Item;
            int Index;
            int IndexMax;
            Boolean InUse = false;
            Boolean PrevInUse = false;

            IO.IOWrapper.BitmapData bitmapData = null;

            Data.RedrawScreen = 2;                       /* Set the flag to "busy". */

            /* Exit if the library is not processing a disk yet. */
            if (!Data.Disk.IsOpen)
            {
                Data.RedrawScreen = 0;                       /* Set the flag to "no". */
                return;
            }

            /* Clear screen. */
            //m_jkGui->ClearScreen(NULL);

            /* Show the map of all the clusters in use. */
            UInt64 Lcn = 0;
            UInt64 ClusterStart = 0;

            PrevInUse = true;

            if (m_clusterData == null)
            {
                m_clusterData = new List<CLUSTER_COLORS>(40000000);

                for (Int32 ii = 0; ii < 40000000; ii++)
                {
                    m_clusterData.Add(CLUSTER_COLORS.COLOREMPTY);
                }
            }

            do
            {
                if (Data.Running != RunningState.RUNNING) break;
                if (Data.RedrawScreen != 2) break;
                if (!Data.Disk.IsOpen) break;

                /* Fetch a block of cluster data. */
//                BitmapParam.StartingLcn.QuadPart = Lcn;

                bitmapData = Data.Disk.VolumeBitmap;

/*
                ErrorCode = DeviceIoControl(Data.Disk.VolumeHandle,FSCTL_GET_VOLUME_BITMAP,
                    BitmapParam,sizeof(BitmapParam),&BitmapData,sizeof(BitmapData),&w,NULL);

*/
/*
                if (bitmapData == null)
                {
                    IOWrapper.GetMessage();
                }
                if (ErrorCode != 0)
                {
                    ErrorCode = NO_ERROR;
                } else {
                    ErrorCode = GetLastError();
                }
                if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_MORE_DATA)) break;

*/
                /* Sanity check. */
                if (Lcn >= bitmapData.StartingLcn + bitmapData.BitmapSize)
                    throw new Exception("Sanity check failed!");
                //break;

                /* Analyze the clusterdata. We resume where the previous block left off. */
                Lcn = bitmapData.StartingLcn;
                Index = 0;
                //Mask = 1;
                IndexMax = bitmapData.Buffer.Length;

                while ((Index < IndexMax) && (Data.Running == RunningState.RUNNING))
                {
                    //InUse = (bitmapData.Buffer[Index] & Mask);
                    InUse = bitmapData.Buffer[Index];

                    /* If at the beginning of the disk then copy the InUse value as our
                    starting value. */
                    if (Lcn == 0) PrevInUse = InUse;

                    /* At the beginning and end of an Exclude draw the cluster. */
                    if ((Lcn == Data.MftExcludes[0].Start) || (Lcn == Data.MftExcludes[0].End) ||
                        (Lcn == Data.MftExcludes[1].Start) || (Lcn == Data.MftExcludes[1].End) ||
                        (Lcn == Data.MftExcludes[2].Start) || (Lcn == Data.MftExcludes[2].End))
                    {
                        if ((Lcn == Data.MftExcludes[0].End) ||
                            (Lcn == Data.MftExcludes[1].End) ||
                            (Lcn == Data.MftExcludes[2].End))
                        {
                            DrawCluster(ClusterStart,Lcn,CLUSTER_COLORS.COLORUNMOVABLE);
                        }
                        else if (PrevInUse == false)
                        {
                            DrawCluster(ClusterStart,Lcn,CLUSTER_COLORS.COLOREMPTY);
                        }
                        else
                        {
                            DrawCluster(ClusterStart,Lcn,CLUSTER_COLORS.COLORALLOCATED);
                        }

                        InUse = true;
                        PrevInUse = true;
                        ClusterStart = Lcn;
                    }

                    if ((PrevInUse == false) && (InUse != false))
                    {          /* Free */
                        DrawCluster(ClusterStart, Lcn, CLUSTER_COLORS.COLOREMPTY);
                        ClusterStart = Lcn;
                    }

                    if ((PrevInUse != false) && (InUse == false))
                    {          /* In use */
                        DrawCluster(ClusterStart, Lcn, CLUSTER_COLORS.COLORALLOCATED);
                        ClusterStart = Lcn;
                    }

                    PrevInUse = InUse;

                    Index++;
                    Lcn++;
                }

            } while (Lcn < bitmapData.StartingLcn + bitmapData.BitmapSize);

            if ((Lcn > 0) && (Data.RedrawScreen == 2))
            {
                if (PrevInUse == false)
                {          /* Free */
                    DrawCluster(ClusterStart, Lcn, CLUSTER_COLORS.COLOREMPTY);
                }

                if (PrevInUse != false)
                {          /* In use */
                    DrawCluster(ClusterStart, Lcn, CLUSTER_COLORS.COLORALLOCATED);
                }
            }

            /* Show the MFT zones. */
            for (int i = 0; i < 3; i++)
            {
                if (Data.RedrawScreen != 2) break;
                if (Data.MftExcludes[i].Start <= 0) continue;

                DrawCluster(Data.MftExcludes[i].Start, Data.MftExcludes[i].End, CLUSTER_COLORS.COLORMFT);
            }

            /* Colorize all the files on the screen.
                Note: the "$BadClus" file on NTFS disks maps the entire disk, so we have to
                ignore it. */
            for (Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
            {
                if (Data.Running != RunningState.RUNNING) break;
                if (Data.RedrawScreen != 2) break;

                if ((Item.LongFilename != null) &&
                    ((Item.LongFilename.CompareTo("$BadClus") == 0) ||
                     (Item.LongFilename.CompareTo("$BadClus:$Bad:$DATA") == 0)))
                {
                    continue;
                }

                ColorizeItem(Item, 0, 0, false);
            }

            /* Set the flag to "no". */
            if (Data.RedrawScreen == 2) Data.RedrawScreen = 0;
        }

/*
        / *

        Look for a gap, a block of empty clusters on the volume.
        MinimumLcn: Start scanning for gaps at this location. If there is a gap
        at this location then return it. Zero is the begin of the disk.
        MaximumLcn: Stop scanning for gaps at this location. Zero is the end of
        the disk.
        MinimumSize: The gap must have at least this many contiguous free clusters.
        Zero will match any gap, so will return the first gap at or above
        MinimumLcn.
        MustFit: if YES then only return a gap that is bigger/equal than the
        MinimumSize. If NO then return a gap bigger/equal than MinimumSize,
        or if no such gap is found return the largest gap on the volume (above
        MinimumLcn).
        FindHighestGap: if NO then return the lowest gap that is bigger/equal
        than the MinimumSize. If YES then return the highest gap.
        Return YES if succes, NO if no gap was found or an error occurred.
        The routine asks Windows for the cluster bitmap every time. It would be
        faster to cache the bitmap in memory, but that would cause more fails
        because of stale information.

        * /
        int JKDefragLib::FindGap(struct DefragDataStruct *Data,
						         ULONG64 MinimumLcn,          / * Gap must be at or above this LCN. * /
						         ULONG64 MaximumLcn,          / * Gap must be below this LCN. * /
						         ULONG64 MinimumSize,         / * Gap must be at least this big. * /
						         int MustFit,                 / * YES: gap must be at least MinimumSize. * /
						         int FindHighestGap,          / * YES: return the last gap that fits. * /
						         ULONG64 *BeginLcn,           / * Result, LCN of begin of cluster. * /
						         ULONG64 *EndLcn,             / * Result, LCN of end of cluster. * /
						         BOOL IgnoreMftExcludes)
        {
	        STARTING_LCN_INPUT_BUFFER BitmapParam;

	        struct
	        {
		        ULONG64 StartingLcn;
		        ULONG64 BitmapSize;

		        BYTE Buffer[65536];               / * Most efficient if binary multiple. * /
	        } BitmapData;

	        ULONG64 Lcn;
	        ULONG64 ClusterStart;
	        ULONG64 HighestBeginLcn;
	        ULONG64 HighestEndLcn;
	        ULONG64 LargestBeginLcn;
	        ULONG64 LargestEndLcn;

	        int Index;
	        int IndexMax;

	        BYTE Mask;

	        int InUse;
	        int PrevInUse;

	        DWORD ErrorCode;

	        WCHAR s1[BUFSIZ];

	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Sanity check. * /
	        if (MinimumLcn >= Data->TotalClusters) return(NO);

	        / * Main loop to walk through the entire clustermap. * /
	        Lcn = MinimumLcn;
	        ClusterStart = 0;
	        PrevInUse = 1;
	        HighestBeginLcn = 0;
	        HighestEndLcn = 0;
	        LargestBeginLcn = 0;
	        LargestEndLcn = 0;

	        do
	        {
		        / * Fetch a block of cluster data. If error then return NO. * /
		        BitmapParam.StartingLcn.QuadPart = Lcn;
		        ErrorCode = DeviceIoControl(Data->Disk.VolumeHandle,FSCTL_GET_VOLUME_BITMAP,
			        &BitmapParam,sizeof(BitmapParam),&BitmapData,sizeof(BitmapData),&w,NULL);

		        if (ErrorCode != 0)
		        {
			        ErrorCode = NO_ERROR;
		        }
		        else
		        {
			        ErrorCode = GetLastError();
		        }

		        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_MORE_DATA))
		        {
			        / * Show debug message: "ERROR: could not get volume bitmap: %s" * /
			        SystemErrorStr(GetLastError(),s1,BUFSIZ);

        //			jkGui->ShowDebug(1,NULL,Data->DebugMsg[12],s1);

			        return(NO);
		        }

		        / * Sanity check. * /
		        if (Lcn >= BitmapData.StartingLcn + BitmapData.BitmapSize) return(NO);
		        if (MaximumLcn == 0) MaximumLcn = BitmapData.StartingLcn + BitmapData.BitmapSize;

		        / * Analyze the clusterdata. We resume where the previous block left
		        off. If a cluster is found that matches the criteria then return
		        it's LCN (Logical Cluster Number). * /
		        Lcn = BitmapData.StartingLcn;
		        Index = 0;
		        Mask = 1;

		        IndexMax = sizeof(BitmapData.Buffer);

		        if (BitmapData.BitmapSize / 8 < IndexMax) IndexMax = (int)(BitmapData.BitmapSize / 8);

		        while ((Index < IndexMax) && (Lcn < MaximumLcn))
		        {
			        if (Lcn >= MinimumLcn)
			        {
				        InUse = (BitmapData.Buffer[Index] & Mask);

				        if (((Lcn >= Data->MftExcludes[0].Start) && (Lcn < Data->MftExcludes[0].End)) ||
					        ((Lcn >= Data->MftExcludes[1].Start) && (Lcn < Data->MftExcludes[1].End)) ||
					        ((Lcn >= Data->MftExcludes[2].Start) && (Lcn < Data->MftExcludes[2].End)))
				        {
					        if (IgnoreMftExcludes == FALSE) InUse = 1;
				        }

				        if ((PrevInUse == 0) && (InUse != 0))
				        {
					        / * Show debug message: "Gap found: LCN=%I64d, Size=%I64d" * /
        //					jkGui->ShowDebug(6,NULL,Data->DebugMsg[13],ClusterStart,Lcn - ClusterStart);

					        / * If the gap is bigger/equal than the mimimum size then return it,
					        or remember it, depending on the FindHighestGap parameter. * /
					        if ((ClusterStart >= MinimumLcn) &&
						        (Lcn - ClusterStart >= MinimumSize))
					        {
						        if (FindHighestGap == NO)
						        {
							        if (BeginLcn != NULL) *BeginLcn = ClusterStart;

							        if (EndLcn != NULL) *EndLcn = Lcn;

							        return(YES);
						        }

						        HighestBeginLcn = ClusterStart;
						        HighestEndLcn = Lcn;
					        }

					        / * Remember the largest gap on the volume. * /
					        if ((LargestBeginLcn == 0) ||
						        (LargestEndLcn - LargestBeginLcn < Lcn - ClusterStart))
					        {
						        LargestBeginLcn = ClusterStart;
						        LargestEndLcn = Lcn;
					        }
				        }

				        if ((PrevInUse != 0) && (InUse == 0)) ClusterStart = Lcn;

				        PrevInUse = InUse;
			        }

			        if (Mask == 128)
			        {
				        Mask = 1;
				        Index = Index + 1;
			        }
			        else
			        {
				        Mask = Mask << 1;
			        }

			        Lcn = Lcn + 1;
		        }
	        } while ((ErrorCode == ERROR_MORE_DATA) &&
		        (Lcn < BitmapData.StartingLcn + BitmapData.BitmapSize) &&
		        (Lcn < MaximumLcn));

	        / * Process the last gap. * /
	        if (PrevInUse == 0)
	        {
		        / * Show debug message: "Gap found: LCN=%I64d, Size=%I64d" * /
        //		jkGui->ShowDebug(6,NULL,Data->DebugMsg[13],ClusterStart,Lcn - ClusterStart);

		        if ((ClusterStart >= MinimumLcn) && (Lcn - ClusterStart >= MinimumSize))
		        {
			        if (FindHighestGap == NO)
			        {
				        if (BeginLcn != NULL) *BeginLcn = ClusterStart;
				        if (EndLcn != NULL) *EndLcn = Lcn;

				        return(YES);
			        }

			        HighestBeginLcn = ClusterStart;
			        HighestEndLcn = Lcn;
		        }

		        / * Remember the largest gap on the volume. * /
		        if ((LargestBeginLcn == 0) ||
			        (LargestEndLcn - LargestBeginLcn < Lcn - ClusterStart))
		        {
			        LargestBeginLcn = ClusterStart;
			        LargestEndLcn = Lcn;
		        }
	        }

	        / * If the FindHighestGap flag is YES then return the highest gap we have found. * /
	        if ((FindHighestGap == YES) && (HighestBeginLcn != 0))
	        {
		        if (BeginLcn != NULL) *BeginLcn = HighestBeginLcn;
		        if (EndLcn != NULL) *EndLcn = HighestEndLcn;

		        return(YES);
	        }

	        / * If the MustFit flag is NO then return the largest gap we have found. * /
	        if ((MustFit == NO) && (LargestBeginLcn != 0))
	        {
		        if (BeginLcn != NULL) *BeginLcn = LargestBeginLcn;
		        if (EndLcn != NULL) *EndLcn = LargestEndLcn;

		        return(YES);
	        }

	        / * No gap found, return NO. * /
	        return(NO);
        }
*/
        /*

        Calculate the begin of the 3 zones.
        Unmovable files pose an interesting problem. Suppose an unmovable file is in
        zone 1, then the calculation for the beginning of zone 2 must count that file.
        But that changes the beginning of zone 2. Some unmovable files may now suddenly
        be in another zone. So we have to recalculate, which causes another border
        change, and again, and again....
        Note: the program only knows if a file is unmovable after it has tried to move a
        file. So we have to recalculate the beginning of the zones every time we encounter
        an unmovable file.

        */
        void CalculateZones()
        {
	        ItemStruct Item;

            UInt64[] SizeOfMovableFiles/*[3]*/ = new UInt64[3];
	        UInt64[] SizeOfUnmovableFragments/*[3]*/ = new UInt64[3];
	        UInt64[] ZoneEnd/*[3]*/ = new UInt64[3];
	        UInt64[] OldZoneEnd/*[3]*/ = new UInt64[3];
	        UInt64 RealVcn;

	        int Iterate;
	        int i;

	        /* Calculate the number of clusters in movable items for every zone. */
            for (int Zone = 0; Zone <= 2; Zone++)
            {
                SizeOfMovableFiles[Zone] = new UInt64();
                SizeOfMovableFiles[Zone] = 0;
            }

            for (Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
	        {
		        if (Item.Unmovable == true) continue;
		        if (Item.Exclude == true) continue;
                if ((Item.Directory == true) && (Data.CannotMoveDirs > 20)) continue;

		        int Zone = 1;
		        if (Item.SpaceHog == true) Zone = 2;
		        if (Item.Directory == true) Zone = 0;

		        SizeOfMovableFiles[Zone] = SizeOfMovableFiles[Zone] + Item.Clusters;
	        }

	        /* Iterate until the calculation does not change anymore, max 10 times. */
	        for (int Zone = 0; Zone <= 2; Zone++)
                SizeOfUnmovableFragments[Zone] = 0;

	        for (int Zone = 0; Zone <= 2; Zone++) 
                OldZoneEnd[Zone] = 0;

	        for (Iterate = 1; Iterate <= 10; Iterate++)
	        {
		        /* Calculate the end of the zones. */
		        ZoneEnd[0] = SizeOfMovableFiles[0] + SizeOfUnmovableFragments[0] +
                    (UInt64)(Data.TotalClusters * Data.FreeSpace / 100.0);

		        ZoneEnd[1] = ZoneEnd[0] + SizeOfMovableFiles[1] + SizeOfUnmovableFragments[1] +
                    (UInt64)(Data.TotalClusters * Data.FreeSpace / 100.0);

		        ZoneEnd[2] = ZoneEnd[1] + SizeOfMovableFiles[2] + SizeOfUnmovableFragments[2];

		        /* Exit if there was no change. */
		        if ((OldZoneEnd[0] == ZoneEnd[0]) &&
			        (OldZoneEnd[1] == ZoneEnd[1]) &&
			        (OldZoneEnd[2] == ZoneEnd[2])) break;

		        for (int Zone = 0; Zone <= 2; Zone++)
                    OldZoneEnd[Zone] = ZoneEnd[Zone];

		        /* Show debug info. */
        		ShowDebug(4, String.Format("Zone calculation, iteration {0:G}: 0 - {0:G} - {0:G} - {0:G}",
                    Iterate, ZoneEnd[0], ZoneEnd[1], ZoneEnd[2]));

                /* Reset the SizeOfUnmovableFragments array. We are going to (re)calculate these numbers
		        based on the just calculates ZoneEnd's. */
		        for (int Zone = 0; Zone <= 2; Zone++) 
                    SizeOfUnmovableFragments[Zone] = 0;

		        /* The MFT reserved areas are counted as unmovable data. */
		        for (i = 0; i < 3; i++)
		        {
                    if (Data.MftExcludes[i].Start < ZoneEnd[0])
			        {
                        SizeOfUnmovableFragments[0] = SizeOfUnmovableFragments[0] + Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
			        }
                    else if (Data.MftExcludes[i].Start < ZoneEnd[1])
			        {
                        SizeOfUnmovableFragments[1] = SizeOfUnmovableFragments[1] + Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
			        }
                    else if (Data.MftExcludes[i].Start < ZoneEnd[2])
			        {
                        SizeOfUnmovableFragments[2] = SizeOfUnmovableFragments[2] + Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
			        }
		        }

		        /* Walk through all items and count the unmovable fragments. Ignore unmovable fragments
		        in the MFT zones, we have already counted the zones. */
                for (Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
		        {
			        if ((Item.Unmovable == false) &&
				        (Item.Exclude == false) &&
                        ((Item.Directory == false) || (Data.CannotMoveDirs <= 20))) continue;

			        RealVcn = 0;

			        foreach (Fragment fragment in Item.FragmentList)
			        {
				        if (fragment.IsLogical)
				        {
                            if (((fragment.Lcn < Data.MftExcludes[0].Start) || (fragment.Lcn >= Data.MftExcludes[0].End)) &&
                                ((fragment.Lcn < Data.MftExcludes[1].Start) || (fragment.Lcn >= Data.MftExcludes[1].End)) &&
                                ((fragment.Lcn < Data.MftExcludes[2].Start) || (fragment.Lcn >= Data.MftExcludes[2].End)))
					        {
						        if (fragment.Lcn < ZoneEnd[0])
						        {
							        SizeOfUnmovableFragments[0] = SizeOfUnmovableFragments[0] + fragment.Length;
						        }
						        else if (fragment.Lcn < ZoneEnd[1])
						        {
							        SizeOfUnmovableFragments[1] = SizeOfUnmovableFragments[1] + fragment.Length;
						        }
						        else if (fragment.Lcn < ZoneEnd[2])
						        {
							        SizeOfUnmovableFragments[2] = SizeOfUnmovableFragments[2] + fragment.Length;
						        }
					        }

					        RealVcn += fragment.Length;
				        }
			        }
		        }
	        }

	        /* Calculated the begin of the zones. */
            Data.Zones[0] = 0;

            for (i = 1; i <= 3; i++) Data.Zones[i] = ZoneEnd[i - 1];
        }
/*
        / *

        Subfunction for MoveItem(), see below. Move (part of) an item to a new
        location on disk. Return errorcode from DeviceIoControl().
        The file is moved in a single FSCTL_MOVE_FILE call. If the file has
        fragments then Windows will join them up.
        Note: the offset and size of the block is in absolute clusters, not
        virtual clusters.

        * /
        DWORD JKDefragLib::MoveItem1(struct DefragDataStruct *Data,
							         HANDLE FileHandle,
        struct ItemStruct *Item,
	        ULONG64 NewLcn,                   / * Where to move to. * /
	        ULONG64 Offset,                   / * Number of first cluster to be moved. * /
	        ULONG64 Size)                     / * Number of clusters to be moved. * /
        {
	        MOVE_FILE_DATA MoveParams;

	        struct FragmentListStruct *Fragment;

	        ULONG64 Vcn;
	        ULONG64 RealVcn;
	        ULONG64 Lcn;

	        DWORD ErrorCode;
	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Find the first fragment that contains clusters inside the block, so we
	        can translate the absolute cluster number of the block into the virtual
	        cluster number used by Windows. * /
	        Vcn = 0;
	        RealVcn = 0;

	        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
	        {
		        if (Fragment->Lcn != VIRTUALFRAGMENT)
		        {
			        if (RealVcn + Fragment->NextVcn - Vcn - 1 >= Offset) break;

			        RealVcn = RealVcn + Fragment->NextVcn - Vcn;
		        }

		        Vcn = Fragment->NextVcn;
	        }

	        / * Setup the parameters for the move. * /
	        MoveParams.FileHandle = FileHandle;
	        MoveParams.StartingLcn.QuadPart = NewLcn;
	        MoveParams.StartingVcn.QuadPart = Vcn + (Offset - RealVcn);
	        MoveParams.ClusterCount = (DWORD)(Size);

	        if (Fragment == NULL)
	        {
		        Lcn = 0;
	        }
	        else
	        {
		        Lcn = Fragment->Lcn + (Offset - RealVcn);
	        }

	        / * Show progress message. * /
        //	jkGui->ShowMove(Item,MoveParams.ClusterCount,Lcn,NewLcn,MoveParams.StartingVcn.QuadPart);

	        / * Draw the item and the destination clusters on the screen in the BUSY	color. * /
	        ColorizeItem(Data,Item,MoveParams.StartingVcn.QuadPart,MoveParams.ClusterCount,NO);

        //	jkGui->DrawCluster(Data,NewLcn,NewLcn + Size,JKDefragStruct::COLORBUSY);

	        / * Call Windows to perform the move. * /
	        ErrorCode = DeviceIoControl(Data->Disk.VolumeHandle,FSCTL_MOVE_FILE,&MoveParams,
		        sizeof(MoveParams),NULL,0,&w,NULL);

	        if (ErrorCode != 0)
	        {
		        ErrorCode = NO_ERROR;
	        }
	        else
	        {
		        ErrorCode = GetLastError();
	        }

	        / * Update the PhaseDone counter for the progress bar. * /
	        Data->PhaseDone = Data->PhaseDone + MoveParams.ClusterCount;

	        / * Undraw the destination clusters on the screen. * /
        //	jkGui->DrawCluster(Data,NewLcn,NewLcn + Size,JKDefragStruct::COLOREMPTY);

	        return(ErrorCode);
        }

        / *

        Subfunction for MoveItem(), see below. Move (part of) an item to a new
        location on disk. Return errorcode from DeviceIoControl().
        Move the item one fragment at a time, a FSCTL_MOVE_FILE call per fragment.
        The fragments will be lined up on disk and the defragger will treat the
        item as unfragmented.
        Note: the offset and size of the block is in absolute clusters, not
        virtual clusters.

        * /
        DWORD JKDefragLib::MoveItem2(struct DefragDataStruct *Data,
							         HANDLE FileHandle,
        struct ItemStruct *Item,
	        ULONG64 NewLcn,                / * Where to move to. * /
	        ULONG64 Offset,                / * Number of first cluster to be moved. * /
	        ULONG64 Size)                  / * Number of clusters to be moved. * /
        {
	        MOVE_FILE_DATA MoveParams;

	        struct FragmentListStruct *Fragment;

	        ULONG64 Vcn;
	        ULONG64 RealVcn;
	        ULONG64 FromLcn;

	        DWORD ErrorCode;
	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Walk through the fragments of the item and move them one by one to the new location. * /
	        ErrorCode = NO_ERROR;
	        Vcn = 0;
	        RealVcn = 0;

	        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
	        {
		        if (*Data->Running != RUNNING) break;

		        if (Fragment->Lcn != VIRTUALFRAGMENT)
		        {
			        if (RealVcn >= Offset + Size) break;

			        if (RealVcn + Fragment->NextVcn - Vcn - 1 >= Offset)
			        {
				        / * Setup the parameters for the move. If the block that we want to move
				        begins somewhere in the middle of a fragment then we have to setup
				        slightly differently than when the fragment is at or after the begin
				        of the block. * /
				        MoveParams.FileHandle = FileHandle;

				        if (RealVcn < Offset)
				        {
					        / * The fragment starts before the Offset and overlaps. Move the
					        part of the fragment from the Offset until the end of the
					        fragment or the block. * /
					        MoveParams.StartingLcn.QuadPart = NewLcn;
					        MoveParams.StartingVcn.QuadPart = Vcn + (Offset - RealVcn);

					        if (Size < (Fragment->NextVcn - Vcn) - (Offset - RealVcn))
					        {
						        MoveParams.ClusterCount = (DWORD)Size;
					        }
					        else
					        {
						        MoveParams.ClusterCount = (DWORD)((Fragment->NextVcn - Vcn) - (Offset - RealVcn));
					        }

					        FromLcn = Fragment->Lcn + (Offset - RealVcn);
				        }
				        else
				        {
					        / * The fragment starts at or after the Offset. Move the part of
					        the fragment inside the block (up until Offset+Size). * /
					        MoveParams.StartingLcn.QuadPart = NewLcn + RealVcn - Offset;
					        MoveParams.StartingVcn.QuadPart = Vcn;

					        if (Fragment->NextVcn - Vcn < Offset + Size - RealVcn)
					        {
						        MoveParams.ClusterCount = (DWORD)(Fragment->NextVcn - Vcn);
					        }
					        else
					        {
						        MoveParams.ClusterCount = (DWORD)(Offset + Size - RealVcn);
					        }
					        FromLcn = Fragment->Lcn;
				        }

				        / * Show progress message. * /
        //				jkGui->ShowMove(Item,MoveParams.ClusterCount,FromLcn,MoveParams.StartingLcn.QuadPart,
        //					MoveParams.StartingVcn.QuadPart);

				        / * Draw the item and the destination clusters on the screen in the BUSY	color. * /
				        //				if (*Data->RedrawScreen == 0) {
				        ColorizeItem(Data,Item,MoveParams.StartingVcn.QuadPart,MoveParams.ClusterCount,NO);
				        //				} else {
				        //					m_jkGui->ShowDiskmap(Data);
				        //				}

        //				jkGui->DrawCluster(Data,MoveParams.StartingLcn.QuadPart,
        //					MoveParams.StartingLcn.QuadPart + MoveParams.ClusterCount,JKDefragStruct::COLORBUSY);

				        / * Call Windows to perform the move. * /
				        ErrorCode = DeviceIoControl(Data->Disk.VolumeHandle,FSCTL_MOVE_FILE,&MoveParams,
					        sizeof(MoveParams),NULL,0,&w,NULL);

				        if (ErrorCode != 0)
				        {
					        ErrorCode = NO_ERROR;
				        }
				        else
				        {
					        ErrorCode = GetLastError();
				        }

				        / * Update the PhaseDone counter for the progress bar. * /
				        Data->PhaseDone = Data->PhaseDone + MoveParams.ClusterCount;

				        / * Undraw the destination clusters on the screen. * /
        //				jkGui->DrawCluster(Data,MoveParams.StartingLcn.QuadPart,
        //					MoveParams.StartingLcn.QuadPart + MoveParams.ClusterCount,JKDefragStruct::COLOREMPTY);

				        / * If there was an error then exit. * /
				        if (ErrorCode != NO_ERROR) return(ErrorCode);
			        }

			        RealVcn = RealVcn + Fragment->NextVcn - Vcn;
		        }

		        / * Next fragment. * /
		        Vcn = Fragment->NextVcn;
	        }

	        return(ErrorCode);
        }

        / *

        Subfunction for MoveItem(), see below. Move (part of) an item to a new
        location on disk. Return YES if success, NO if failure.
        Strategy 0: move the block in a single FSCTL_MOVE_FILE call. If the block
        has fragments then Windows will join them up.
        Strategy 1: move the block one fragment at a time. The fragments will be
        lined up on disk and the defragger will treat them as unfragmented.
        Note: the offset and size of the block is in absolute clusters, not
        virtual clusters.

        * /
        int JKDefragLib::MoveItem3(struct DefragDataStruct *Data,
        struct ItemStruct *Item,
	        HANDLE FileHandle,
	        ULONG64 NewLcn,          / * Where to move to. * /
	        ULONG64 Offset,          / * Number of first cluster to be moved. * /
	        ULONG64 Size,            / * Number of clusters to be moved. * /
	        int Strategy)            / * 0: move in one part, 1: move individual fragments. * /
        {
	        DWORD ErrorCode;

	        WCHAR ErrorString[BUFSIZ];

	        int Result;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Slow the program down if so selected. * /
	        SlowDown(Data);

	        / * Move the item, either in a single block or fragment by fragment. * /
	        if (Strategy == 0)
	        {
		        ErrorCode = MoveItem1(Data,FileHandle,Item,NewLcn,Offset,Size);
	        }
	        else
	        {
		        ErrorCode = MoveItem2(Data,FileHandle,Item,NewLcn,Offset,Size);
	        }

	        / * If there was an error then fetch the errormessage and save it. * /
	        if (ErrorCode != NO_ERROR) SystemErrorStr(ErrorCode,ErrorString,BUFSIZ);

	        / * Fetch the new fragment map of the item and refresh the screen. * /
	        ColorizeItem(Data,Item,0,0,YES);

	        TreeDetach(Data,Item);

	        Result = GetFragments(Data,Item,FileHandle);

	        TreeInsert(Data,Item);

	        //		if (*Data->RedrawScreen == 0) {
	        ColorizeItem(Data,Item,0,0,NO);
	        //		} else {
	        //			m_jkGui->ShowDiskmap(Data);
	        //		}

	        / * If Windows reported an error while moving the item then show the
	        errormessage and return NO. * /
	        if (ErrorCode != NO_ERROR)
	        {
        //		jkGui->ShowDebug(3,Item,ErrorString);

		        return(NO);
	        }

	        / * If there was an error analyzing the item then return NO. * /
	        if (Result == NO) return(NO);

	        return(YES);
        }

        / *

        Subfunction for MoveItem(), see below. Move the item with strategy 0.
        If this results in fragmentation then try again using strategy 1.
        Return YES if success, NO if failed to move without fragmenting the
        item.
        Note: The Windows defragmentation API does not report an error if it only
        moves part of the file and has fragmented the file. This can for example
        happen when part of the file is locked and cannot be moved, or when (part
        of) the gap was previously in use by another file but has not yet been
        released by the NTFS checkpoint system.
        Note: the offset and size of the block is in absolute clusters, not
        virtual clusters.

        * /
        int JKDefragLib::MoveItem4(struct DefragDataStruct *Data,
        struct ItemStruct *Item,
	        HANDLE FileHandle,
	        ULONG64 NewLcn,                                   / * Where to move to. * /
	        ULONG64 Offset,                / * Number of first cluster to be moved. * /
	        ULONG64 Size,                       / * Number of clusters to be moved. * /
	        int Direction)                          / * 0: move up, 1: move down. * /
        {
	        ULONG64 OldLcn;
	        ULONG64 ClusterStart;
	        ULONG64 ClusterEnd;

	        int Result;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Remember the current position on disk of the item. * /
	        OldLcn = GetItemLcn(Item);

	        / * Move the Item to the requested LCN. If error then return NO. * /
	        Result = MoveItem3(Data,Item,FileHandle,NewLcn,Offset,Size,0);

	        if (Result == NO) return(NO);
	        if (*Data->Running != RUNNING) return(NO);

	        / * If the block is not fragmented then return YES. * /
	        if (IsFragmented(Item,Offset,Size) == NO) return(YES);

	        / * Show debug message: "Windows could not move the file, trying alternative method." * /
        //	jkGui->ShowDebug(3,Item,Data->DebugMsg[42]);

	        / * Find another gap on disk for the item. * /
	        if (Direction == 0)
	        {
		        ClusterStart = OldLcn + Item->Clusters;

		        if ((ClusterStart + Item->Clusters >= NewLcn) &&
			        (ClusterStart < NewLcn + Item->Clusters))
		        {
			        ClusterStart = NewLcn + Item->Clusters;
		        }

		        Result = FindGap(Data,ClusterStart,0,Size,YES,NO,&ClusterStart,&ClusterEnd,FALSE);
	        }
	        else
	        {
		        Result = FindGap(Data,Data->Zones[1],OldLcn,Size,YES,YES,&ClusterStart,&ClusterEnd,FALSE);
	        }

	        if (Result == NO) return(NO);

	        / * Add the size of the item to the width of the progress bar, we have discovered
	        that we have more work to do. * /
	        Data->PhaseTodo = Data->PhaseTodo + Size;

	        / * Move the item to the other gap using strategy 1. * /
	        if (Direction == 0)
	        {
		        Result = MoveItem3(Data,Item,FileHandle,ClusterStart,Offset,Size,1);
	        }
	        else
	        {
		        Result = MoveItem3(Data,Item,FileHandle,ClusterEnd - Size,Offset,Size,1);
	        }

	        if (Result == NO) return(NO);

	        / * If the block is still fragmented then return NO. * /
	        if (IsFragmented(Item,Offset,Size) == YES)
	        {
		        / * Show debug message: "Alternative method failed, leaving file where it is." * /
        //		jkGui->ShowDebug(3,Item,Data->DebugMsg[45]);

		        return(NO);
	        }

        //	jkGui->ShowDebug(3,Item,L"");

	        / * Add the size of the item to the width of the progress bar, we have more work to do. * /
	        Data->PhaseTodo = Data->PhaseTodo + Size;

	        / * Strategy 1 has helped. Move the Item again to where we want it, but
	        this time use strategy 1. * /
	        Result = MoveItem3(Data,Item,FileHandle,NewLcn,Offset,Size,1);

	        return(Result);
        }

        / *

        Move (part of) an item to a new location on disk. Moving the Item will
        automatically defragment it. If unsuccesful then set the Unmovable
        flag of the item and return NO, otherwise return YES.
        Note: the item will move to a different location in the tree.
        Note: the offset and size of the block is in absolute clusters, not
        virtual clusters.

        * /
        int JKDefragLib::MoveItem(struct DefragDataStruct *Data,
        struct ItemStruct *Item,
	        ULONG64 NewLcn,                                   / * Where to move to. * /
	        ULONG64 Offset,                / * Number of first cluster to be moved. * /
	        ULONG64 Size,                       / * Number of clusters to be moved. * /
	        int Direction)                          / * 0: move up, 1: move down. * /
        {
	        HANDLE FileHandle;

	        ULONG64 ClustersTodo;
	        ULONG64 ClustersDone;

	        int Result;

	        / * If the Item is Unmovable, Excluded, or has zero size then we cannot move it. * /
	        if (Item->Unmovable == YES) return(NO);
	        if (Item->Exclude == YES) return(NO);
	        if (Item->Clusters == 0) return(NO);

	        / * Directories cannot be moved on FAT volumes. This is a known Windows limitation
	        and not a bug in JkDefrag. But JkDefrag will still try, to allow for possible
	        circumstances where the Windows defragmentation API can move them after all.
	        To speed up things we count the number of directories that could not be moved,
	        and when it reaches 20 we ignore all directories from then on. * /
	        if ((Item->Directory == YES) && (Data->CannotMoveDirs > 20))
	        {
		        Item->Unmovable = YES;

		        ColorizeItem(Data,Item,0,0,NO);

		        return(NO);
	        }

	        / * Open a filehandle for the item and call the subfunctions (see above) to
	        move the file. If success then return YES. * /
	        ClustersDone = 0;
	        Result = YES;

	        while ((ClustersDone < Size) && (*Data->Running == RUNNING))
	        {
		        ClustersTodo = Size - ClustersDone;

		        if (Data->BytesPerCluster > 0)
		        {
			        if (ClustersTodo > 1073741824 / Data->BytesPerCluster)
			        {
				        ClustersTodo = 1073741824 / Data->BytesPerCluster;
			        }
		        }
		        else
		        {
			        if (ClustersTodo > 262144) ClustersTodo = 262144;
		        }

		        FileHandle = OpenItemHandle(Data,Item);

		        Result = NO;

		        if (FileHandle == NULL) break;

		        Result = MoveItem4(Data,Item,FileHandle,NewLcn+ClustersDone,Offset+ClustersDone,
			        ClustersTodo,Direction);

		        if (Result == NO) break;

		        ClustersDone = ClustersDone + ClustersTodo;

		        FlushFileBuffers(FileHandle);            / * Is this useful? Can't hurt. * /
		        CloseHandle(FileHandle);
	        }

	        if (Result == YES)
	        {
		        if (Item->Directory == YES) Data->CannotMoveDirs = 0;

		        return(YES);
	        }

	        / * If error then set the Unmovable flag, colorize the item on the screen, recalculate
	        the begin of the zone's, and return NO. * /
	        Item->Unmovable = YES;

	        if (Item->Directory == YES) Data->CannotMoveDirs++;

	        ColorizeItem(Data,Item,0,0,NO);
	        CalculateZones(Data);

	        return(NO);
        }

        / *

        Look in the ItemTree and return the highest file above the gap that fits inside
        the gap (cluster start - cluster end). Return a pointer to the item, or NULL if
        no file could be found.
        Direction=0      Search for files below the gap.
        Direction=1      Search for files above the gap.
        Zone=0           Only search the directories.
        Zone=1           Only search the regular files.
        Zone=2           Only search the SpaceHogs.
        Zone=3           Search all items.

        * /
        struct ItemStruct *JKDefragLib::FindHighestItem(struct DefragDataStruct *Data,
	        ULONG64 ClusterStart,
	        ULONG64 ClusterEnd,
	        int Direction,
	        int Zone)
        {
	        struct ItemStruct *Item;

	        ULONG64 ItemLcn;

	        int FileZone;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * "Looking for highest-fit %I64d[%I64d]" * /
        //	jkGui->ShowDebug(5,NULL,L"Looking for highest-fit %I64d[%I64d]",
        //		ClusterStart,ClusterEnd - ClusterStart);

	        / * Walk backwards through all the items on disk and select the first
	        file that fits inside the free block. If we find an exact match then
	        immediately return it. * /
	        for (Item = TreeFirst(Data->ItemTree,Direction);
		        Item != NULL;
		        Item = TreeNextPrev(Item,Direction))
	        {
		        ItemLcn = GetItemLcn(Item);

		        if (ItemLcn == 0) continue;

		        if (Direction == 1)
		        {
			        if (ItemLcn < ClusterEnd) return(NULL);
		        }
		        else
		        {
			        if (ItemLcn > ClusterStart) return(NULL);
		        }

		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;

		        if (Zone != 3)
		        {
			        FileZone = 1;

			        if (Item->SpaceHog == YES) FileZone = 2;
			        if (Item->Directory == YES) FileZone = 0;
			        if (Zone != FileZone) continue;
		        }

		        if (Item->Clusters > ClusterEnd - ClusterStart) continue;

		        return(Item);
	        }

	        return(NULL);
        }

        / *

        Find the highest item on disk that fits inside the gap (cluster start - cluster
        end), and combined with other items will perfectly fill the gap. Return NULL if
        no perfect fit could be found. The subroutine will limit it's running time to 0.5
        seconds.
        Direction=0      Search for files below the gap.
        Direction=1      Search for files above the gap.
        Zone=0           Only search the directories.
        Zone=1           Only search the regular files.
        Zone=2           Only search the SpaceHogs.
        Zone=3           Search all items.

        * /
        struct ItemStruct *JKDefragLib::FindBestItem(struct DefragDataStruct *Data,
	        ULONG64 ClusterStart,
	        ULONG64 ClusterEnd,
	        int Direction,
	        int Zone)
        {
	        struct ItemStruct *Item;
	        struct ItemStruct *FirstItem;

	        ULONG64 ItemLcn;
	        ULONG64 GapSize;
	        ULONG64 TotalItemsSize;

	        int FileZone;

	        struct __timeb64 Time;

	        LONG64 MaxTime;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //	jkGui->ShowDebug(5,NULL,L"Looking for perfect fit %I64d[%I64d]",
        //		ClusterStart,ClusterEnd - ClusterStart);

	        / * Walk backwards through all the items on disk and select the first item that
	        fits inside the free block, and combined with other items will fill the gap
	        perfectly. If we find an exact match then immediately return it. * /

	        _ftime64_s(&Time);

	        MaxTime = Time.time * 1000 + Time.millitm + 500;
	        FirstItem = NULL;
	        GapSize = ClusterEnd - ClusterStart;
	        TotalItemsSize = 0;

	        for (Item = TreeFirst(Data->ItemTree,Direction);
		        Item != NULL;
		        Item = TreeNextPrev(Item,Direction))
	        {
		        / * If we have passed the top of the gap then.... * /
		        ItemLcn = GetItemLcn(Item);

		        if (ItemLcn == 0) continue;

		        if (((Direction == 1) && (ItemLcn < ClusterEnd)) ||
			        ((Direction == 0) && (ItemLcn > ClusterEnd)))
		        {
			        / * If we did not find an item that fits inside the gap then exit. * /
			        if (FirstItem == NULL) break;

			        / * Exit if the total size of all the items is less than the size of the gap.
			        We know that we can never find a perfect fit. * /
			        if (TotalItemsSize < ClusterEnd - ClusterStart)
			        {
        //				jkGui->ShowDebug(5,NULL,L"No perfect fit found, the total size of all the items above the gap is less than the size of the gap.");

				        return(NULL);
			        }

			        / * Exit if the running time is more than 0.5 seconds. * /
			        _ftime64_s(&Time);

			        if (Time.time * 1000 + Time.millitm > MaxTime)
			        {
        //				jkGui->ShowDebug(5,NULL,L"No perfect fit found, out of time.");

				        return(NULL);
			        }

			        / * Rewind and try again. The item that we have found previously fits in the
			        gap, but it does not combine with other items to perfectly fill the gap. * /
			        Item = FirstItem;
			        FirstItem = NULL;
			        GapSize = ClusterEnd - ClusterStart;
			        TotalItemsSize = 0;

			        continue;
		        }

		        / * Ignore all unsuitable items. * /
		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;

		        if (Zone != 3)
		        {
			        FileZone = 1;

			        if (Item->SpaceHog == YES) FileZone = 2;
			        if (Item->Directory == YES) FileZone = 0;
			        if (Zone != FileZone) continue;
		        }

		        if (Item->Clusters < ClusterEnd - ClusterStart)
		        {
			        TotalItemsSize = TotalItemsSize + Item->Clusters;
		        }

		        if (Item->Clusters > GapSize) continue;

		        / * Exit if this item perfectly fills the gap, or if we have found a combination
		        with a previous item that perfectly fills the gap. * /
		        if (Item->Clusters == GapSize)
		        {
        //			jkGui->ShowDebug(5,NULL,L"Perfect fit found.");

			        if (FirstItem != NULL) return(FirstItem);

			        return(Item);
		        }

		        / * We have found an item that fit's inside the gap, but does not perfectly fill
		        the gap. We are now looking to fill a smaller gap. * /
		        GapSize = GapSize - Item->Clusters;

		        / * Remember the first item that fits inside the gap. * /
		        if (FirstItem == NULL) FirstItem = Item;
	        }

        //	jkGui->ShowDebug(5,NULL,L"No perfect fit found, all items above the gap are bigger than the gap.");

	        return(NULL);
        }
*/

        /* Update some numbers in the DefragData. */
        void CallShowStatus(int Phase, int Zone)
        {
	        STARTING_LCN_INPUT_BUFFER BitmapParam = new STARTING_LCN_INPUT_BUFFER();

	        int Index;
	        int IndexMax;

	        Int64 Count;
	        Int64 Factor;
	        Int64 Sum;

	        /* Count the number of free gaps on the disk. */
            Data.CountGaps = 0;
            Data.CountFreeClusters = 0;
            Data.BiggestGap = 0;
            Data.CountGapsLess16 = 0;
            Data.CountClustersLess16 = 0;

            UInt64 Lcn = 0;
            UInt64 ClusterStart = 0;
            Boolean PrevInUse = true;

            IO.IOWrapper.BitmapData bitmapData = null;

	        do
	        {
		        /* Fetch a block of cluster data. */
		        BitmapParam.StartingLcn = Lcn;

                bitmapData = Data.Disk.VolumeBitmap;

                if (bitmapData.Buffer == null)
                {
                    break;
                }

		        Lcn = bitmapData.StartingLcn;

		        Index = 0;

		        IndexMax = bitmapData.Buffer.Count;

                Boolean InUse = false;
		        while (Index < IndexMax)
		        {
                    InUse = bitmapData.Buffer[Index];

                    if (((Lcn >= Data.MftExcludes[0].Start) && (Lcn < Data.MftExcludes[0].End)) ||
                        ((Lcn >= Data.MftExcludes[1].Start) && (Lcn < Data.MftExcludes[1].End)) ||
                        ((Lcn >= Data.MftExcludes[2].Start) && (Lcn < Data.MftExcludes[2].End)))
                    {
					    InUse = true;
			        }

			        if ((PrevInUse == false) && (InUse != false))
			        {
                        Data.CountGaps ++;
                        Data.CountFreeClusters += Lcn - ClusterStart;

                        if (Data.BiggestGap < Lcn - ClusterStart) Data.BiggestGap = Lcn - ClusterStart;

				        if (Lcn - ClusterStart < 16)
				        {
                            Data.CountGapsLess16++;
                            Data.CountClustersLess16 += Lcn - ClusterStart;
				        }
			        }

			        if ((PrevInUse != false) && (InUse == false)) ClusterStart = Lcn;

			        PrevInUse = InUse;

    		        Index++;
			        Lcn++;
		        }

	        } while ((bitmapData.Buffer != null) && (Lcn < bitmapData.StartingLcn + bitmapData.BitmapSize));

	        if (PrevInUse == false)
	        {
                Data.CountGaps ++;
                Data.CountFreeClusters += Lcn - ClusterStart;

                if (Data.BiggestGap < Lcn - ClusterStart) Data.BiggestGap = Lcn - ClusterStart;

		        if (Lcn - ClusterStart < 16)
		        {
                    Data.CountGapsLess16 ++;
                    Data.CountClustersLess16 += Lcn - ClusterStart;
		        }
	        }

	        /* Walk through all files and update the counters. */
            Data.CountDirectories = 0;
            Data.CountAllFiles = 0;
            Data.CountFragmentedItems = 0;
            Data.CountAllBytes = 0;
            Data.CountFragmentedBytes = 0;
            Data.CountAllClusters = 0;
            Data.CountFragmentedClusters = 0;

            for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
	        {
		        if ((Item.LongFilename != null) &&
			        (Item.LongFilename.Equals("$BadClus") ||
			         Item.LongFilename.Equals("$BadClus:$Bad:$DATA")))
		        {
			        continue;
		        }

                Data.CountAllBytes += Item.Bytes;
                Data.CountAllClusters += Item.Clusters;

		        if (Item.Directory == true)
		        {
                    Data.CountDirectories++;
		        }
		        else
		        {
                    Data.CountAllFiles++;
		        }

                if (Item.FragmentCount > 1)
		        {
                    Data.CountFragmentedItems++;
                    Data.CountFragmentedBytes += Item.Bytes;
                    Data.CountFragmentedClusters += Item.Clusters;
		        }
	        }

	        /* Calculate the average distance between the end of any file to the begin of
	        any other file. After reading a file the harddisk heads will have to move to
	        the beginning of another file. The number is a measure of how fast files can
	        be accessed.

	        For example these 3 files:
	        File 1 begin = 107
	        File 1 end = 312
	        File 2 begin = 595
	        File 2 end = 645
	        File 3 begin = 917
	        File 3 end = 923

	        File 1 end - File 2 begin = 283
	        File 1 end - File 3 begin = 605
	        File 2 end - File 1 begin = 538
	        File 2 end - File 3 begin = 272
	        File 3 end - File 1 begin = 816
	        File 3 end - File 2 begin = 328
	        --> Average distance from end to begin = 473.6666

	        The formula used is:
	        N = number of files
	        Bn = Begin of file n
	        En = End of file n
	        Average = ( (1-N)*(B1+E1) + (3-N)*(B2+E2) + (5-N)*(B3+E3) + .... + (2*N-1-N)*(BN+EN) ) / ( N * (N-1) )

	        For the above example:
	        Average = ( (1-3)*(107+312) + (3-3)*(595+645) + 5-3)*(917+923) ) / ( 3 * (3-1) ) = 473.6666

	        */
	        Count = 0;

            for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
	        {
		        if ((Item.LongFilename == "$BadClus") ||
			        (Item.LongFilename == "$BadClus:$Bad:$DATA"))
		        {
			        continue;
		        }

		        if (Item.Clusters == 0) continue;

		        Count++;
	        }

	        if (Count > 1)
	        {
		        Factor = 1 - Count;
		        Sum = 0;

                for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
		        {
                    if ((Item.LongFilename == "$BadClus") ||
                        (Item.LongFilename == "$BadClus:$Bad:$DATA"))
                    {
                        continue;
                    }

			        if (Item.Clusters == 0) continue;

			        Sum += Factor * (Int64)((Item.Lcn * 2 + Item.Clusters));

			        Factor = Factor + 2;
		        }

                Data.AverageDistance = Sum / (double)(Count * (Count - 1));
	        }
	        else
	        {
                Data.AverageDistance = 0;
	        }

            Data.Phase = (UInt16)Phase;
            Data.Zone = (UInt16)Zone;
            Data.PhaseDone = 0;
            Data.PhaseTodo = 0;

        //	jkGui->ShowStatus(Data);
        }

/*
        / * For debugging only: compare the data with the output from the
        FSCTL_GET_RETRIEVAL_POINTERS function call.
        Note: Reparse points will usually be flagged as different. A reparse point is
        a symbolic link. The CreateFile call will resolve the symbolic link and retrieve
        the info from the real item, but the MFT contains the info from the symbolic
        link. * /
        void JKDefragLib::CompareItems(struct DefragDataStruct *Data, struct ItemStruct *Item)
        {
	        HANDLE FileHandle;

	        ULONG64   Clusters;                         / * Total number of clusters. * /

	        STARTING_VCN_INPUT_BUFFER RetrieveParam;

	        struct
	        {
		        DWORD ExtentCount;

		        ULONG64 StartingVcn;

		        struct
		        {
			        ULONG64 NextVcn;
			        ULONG64 Lcn;
		        } Extents[1000];
	        } ExtentData;

	        BY_HANDLE_FILE_INFORMATION FileInformation;

	        ULONG64 Vcn;

	        struct FragmentListStruct *Fragment;
	        struct FragmentListStruct *LastFragment;

	        DWORD ErrorCode;

	        WCHAR ErrorString[BUFSIZ];

	        int MaxLoop;

	        ULARGE_INTEGER u;

	        DWORD i;
	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //	jkGui->ShowDebug(0,NULL,L"%I64u %s",GetItemLcn(Item),Item->LongFilename);

	        if (Item->Directory == NO)
	        {
		        FileHandle = CreateFileW(Item->LongPath,FILE_READ_ATTRIBUTES,
			        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
			        NULL,OPEN_EXISTING,FILE_FLAG_NO_BUFFERING,NULL);
	        }
	        else
	        {
		        FileHandle = CreateFileW(Item->LongPath,GENERIC_READ,
			        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
			        NULL,OPEN_EXISTING,FILE_FLAG_BACKUP_SEMANTICS,NULL);
	        }

	        if (FileHandle == INVALID_HANDLE_VALUE)
	        {
		        SystemErrorStr(GetLastError(),ErrorString,BUFSIZ);

        //		jkGui->ShowDebug(0,NULL,L"  Could not open: %s",ErrorString);

		        return;
	        }

	        / * Fetch the date/times of the file. * /
	        if (GetFileInformationByHandle(FileHandle,&FileInformation) != 0)
	        {
		        u.LowPart = FileInformation.ftCreationTime.dwLowDateTime;
		        u.HighPart = FileInformation.ftCreationTime.dwHighDateTime;

		        if (Item->CreationTime != u.QuadPart)
		        {
        //			jkGui->ShowDebug(0,NULL,L"  Different CreationTime %I64u <> %I64u = %I64u",
        //				Item->CreationTime,u.QuadPart,Item->CreationTime - u.QuadPart);
		        }

		        u.LowPart = FileInformation.ftLastAccessTime.dwLowDateTime;
		        u.HighPart = FileInformation.ftLastAccessTime.dwHighDateTime;

		        if (Item->LastAccessTime != u.QuadPart)
		        {
        //			jkGui->ShowDebug(0,NULL,L"  Different LastAccessTime %I64u <> %I64u = %I64u",
        //				Item->LastAccessTime,u.QuadPart,Item->LastAccessTime - u.QuadPart);
		        }
	        }

/ *
        #ifdef jk
	        Vcn = 0;
	        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next) {
		        if (Fragment->Lcn != VIRTUALFRAGMENT) {
			        Data->ShowDebug(0,NULL,L"  Extent 1: Lcn=%I64u, Vcn=%I64u, NextVcn=%I64u",
				        Fragment->Lcn,Vcn,Fragment->NextVcn);
		        } else {
			        Data->ShowDebug(0,NULL,L"  Extent 1 (virtual): Vcn=%I64u, NextVcn=%I64u",
				        Vcn,Fragment->NextVcn);
		        }
		        Vcn = Fragment->NextVcn;
	        }
        #endif
* /

	        / * Ask Windows for the clustermap of the item and save it in memory.
	        The buffer that is used to ask Windows for the clustermap has a
	        fixed size, so we may have to loop a couple of times. * /
	        Fragment = Item->Fragments;
	        Clusters = 0;
	        Vcn = 0;
	        MaxLoop = 1000;
	        LastFragment = NULL;

	        do {
		        / * I strongly suspect that the FSCTL_GET_RETRIEVAL_POINTERS system call
		        can sometimes return an empty bitmap and ERROR_MORE_DATA. That's not
		        very nice of Microsoft, because it causes an infinite loop. I've
		        therefore added a loop counter that will limit the loop to 1000
		        iterations. This means the defragger cannot handle files with more
		        than 100000 fragments, though. * /
		        if (MaxLoop <= 0)
		        {
        //			jkGui->ShowDebug(0,NULL,L"  FSCTL_GET_RETRIEVAL_POINTERS error: Infinite loop");

			        return;
		        }

		        MaxLoop = MaxLoop - 1;

		        / * Ask Windows for the (next segment of the) clustermap of this file. If error
		        then leave the loop. * /
		        RetrieveParam.StartingVcn.QuadPart = Vcn;

		        ErrorCode = DeviceIoControl(FileHandle,FSCTL_GET_RETRIEVAL_POINTERS,
			        &RetrieveParam,sizeof(RetrieveParam),&ExtentData,sizeof(ExtentData),&w,NULL);

		        if (ErrorCode != 0)
		        {
			        ErrorCode = NO_ERROR;
		        }
		        else
		        {
			        ErrorCode = GetLastError();
		        }

		        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_MORE_DATA)) break;

		        / * Walk through the clustermap, count the total number of clusters, and
		        save all fragments in memory. * /
		        for (i = 0; i < ExtentData.ExtentCount; i++)
		        {
			        / * Show debug message. * /
/ *
        #ifdef jk
			        if (ExtentData.Extents[i].Lcn != VIRTUALFRAGMENT) {
				        Data->ShowDebug(0,NULL,L"  Extent 2: Lcn=%I64u, Vcn=%I64u, NextVcn=%I64u",
					        ExtentData.Extents[i].Lcn,Vcn,ExtentData.Extents[i].NextVcn);
			        } else {
				        Data->ShowDebug(0,NULL,L"  Extent 2 (virtual): Vcn=%I64u, NextVcn=%I64u",
					        Vcn,ExtentData.Extents[i].NextVcn);
			        }
        #endif
* /

			        / * Add the size of the fragment to the total number of clusters.
			        There are two kinds of fragments: real and virtual. The latter do not
			        occupy clusters on disk, but are information used by compressed
			        and sparse files. * /
			        if (ExtentData.Extents[i].Lcn != VIRTUALFRAGMENT)
			        {
				        Clusters = Clusters + ExtentData.Extents[i].NextVcn - Vcn;
			        }

			        / * Compare the fragment. * /
			        if (Fragment == NULL)
			        {
        //				jkGui->ShowDebug(0,NULL,L"  Extra fragment in FSCTL_GET_RETRIEVAL_POINTERS");
			        }
			        else
			        {
				        if (Fragment->Lcn != ExtentData.Extents[i].Lcn)
				        {
        // 					jkGui->ShowDebug(0,NULL,L"  Different LCN in fragment: %I64u <> %I64u",
        // 						Fragment->Lcn,ExtentData.Extents[i].Lcn);
				        }

				        if (Fragment->NextVcn != ExtentData.Extents[i].NextVcn)
				        {
        // 					jkGui->ShowDebug(0,NULL,L"  Different NextVcn in fragment: %I64u <> %I64u",
        // 						Fragment->NextVcn,ExtentData.Extents[i].NextVcn);
				        }

				        Fragment = Fragment->Next;
			        }

			        / * The Vcn of the next fragment is the NextVcn field in this record. * /
			        Vcn = ExtentData.Extents[i].NextVcn;
		        }

		        / * Loop until we have processed the entire clustermap of the file. * /
	        } while (ErrorCode == ERROR_MORE_DATA);

	        / * If there was an error while reading the clustermap then return NO. * /
	        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_HANDLE_EOF))
	        {
		        SystemErrorStr(ErrorCode,ErrorString,BUFSIZ);

        //		jkGui->ShowDebug(0,Item,L"  Error while processing clustermap: %s",ErrorString);

		        return;
	        }

	        if (Fragment != NULL)
	        {
        //		jkGui->ShowDebug(0,NULL,L"  Extra fragment from MFT");
	        }

	        if (Item->Clusters != Clusters)
	        {
        // 		jkGui->ShowDebug(0,NULL,L"  Different cluster count: %I64u <> %I64u",
        // 			Item->Clusters,Clusters);
	        }
        }

        / * Scan all files in a directory and all it's subdirectories (recursive)
        and store the information in a tree in memory for later use by the
        optimizer. * /
        void JKDefragLib::ScanDir(struct DefragDataStruct *Data, WCHAR *Mask, struct ItemStruct *ParentDirectory)
        {
	        struct ItemStruct *Item;

	        struct FragmentListStruct *Fragment;

	        HANDLE FindHandle;

	        WIN32_FIND_DATAW FindFileData;

	        WCHAR *RootPath;
	        WCHAR *TempPath;

	        HANDLE FileHandle;

	        ULONG64 SystemTime;

	        SYSTEMTIME Time1;

	        FILETIME Time2;

	        ULARGE_INTEGER Time3;

	        int Result;

	        size_t Length;

	        WCHAR *p1;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Slow the program down to the percentage that was specified on the
	        command line. * /
	        SlowDown(Data);

	        / * Determine the rootpath (base path of the directory) by stripping
	        everything after the last backslash in the Mask. The FindFirstFile()
	        system call only processes wildcards in the last section (i.e. after
	        the last backslash). * /
	        RootPath = _wcsdup(Mask);

	        if (RootPath == NULL) return;

	        p1 = wcsrchr(RootPath,'\\');

	        if (p1 != NULL) *p1 = 0;

	        / * Show debug message: "Analyzing: %s". * /
        //	jkGui->ShowDebug(3,NULL,Data->DebugMsg[23],Mask);

	        / * Fetch the current time in the ULONG64 format (1 second = 10000000). * /
	        GetSystemTime(&Time1);

	        if (SystemTimeToFileTime(&Time1,&Time2) == FALSE)
	        {
		        SystemTime = 0;
	        }
	        else
	        {
		        Time3.LowPart = Time2.dwLowDateTime;
		        Time3.HighPart = Time2.dwHighDateTime;

		        SystemTime = Time3.QuadPart;
	        }

	        / * Walk through all the files. If nothing found then exit.
	        Note: I am using FindFirstFileW() instead of _findfirst() because the latter
	        will crash (exit program) on files with badly formed dates. * /
	        FindHandle = FindFirstFileW(Mask,&FindFileData);

	        if (FindHandle == INVALID_HANDLE_VALUE)
	        {
		        free(RootPath);
		        return;
	        }

	        Item = NULL;

	        do 
	        {
		        if (*Data->Running != RUNNING) break;

		        if (wcscmp(FindFileData.cFileName,L".") == 0) continue;
		        if (wcscmp(FindFileData.cFileName,L"..") == 0) continue;

		        / * Ignore reparse-points, a directory where a volume is mounted
		        with the MOUNTVOL command. * /
		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
		        {
			        continue;
		        }

		        / * Cleanup old item. * /
		        if (Item != NULL)
		        {
			        if (Item->ShortPath != NULL) free(Item->ShortPath);
			        if (Item->ShortFilename != NULL) free(Item->ShortFilename);
			        if (Item->LongPath != NULL) free(Item->LongPath);
			        if (Item->LongFilename != NULL) free(Item->LongFilename);

			        while (Item->Fragments != NULL)
			        {
				        Fragment = Item->Fragments->Next;

				        free(Item->Fragments);

				        Item->Fragments = Fragment;
			        }

			        free(Item);

			        Item = NULL;
		        }

		        / * Create new item. * /
		        Item = (struct ItemStruct *)malloc(sizeof(struct ItemStruct));

		        if (Item == NULL) break;

		        Item->ShortPath = NULL;
		        Item->ShortFilename = NULL;
		        Item->LongPath = NULL;
		        Item->LongFilename = NULL;
		        Item->Fragments = NULL;

		        Length = wcslen(RootPath) + wcslen(FindFileData.cFileName) + 2;

		        Item->LongPath = (WCHAR *)malloc(sizeof(WCHAR) * Length);

		        if (Item->LongPath == NULL) break;

		        swprintf_s(Item->LongPath,Length,L"%s\\%s",RootPath,FindFileData.cFileName);

		        Item->LongFilename = _wcsdup(FindFileData.cFileName);

		        if (Item->LongFilename == NULL) break;

		        Length = wcslen(RootPath) + wcslen(FindFileData.cAlternateFileName) + 2;

		        Item->ShortPath = (WCHAR *)malloc(sizeof(WCHAR) * Length);

		        if (Item->ShortPath == NULL) break;

		        swprintf_s(Item->ShortPath,Length,L"%s\\%s",RootPath,FindFileData.cAlternateFileName);

		        Item->ShortFilename = _wcsdup(FindFileData.cAlternateFileName);

		        if (Item->ShortFilename == NULL) break;

		        Item->Bytes = FindFileData.nFileSizeHigh * ((ULONG64)MAXDWORD + 1) +
			        FindFileData.nFileSizeLow;

		        Item->Clusters = 0;
		        Item->CreationTime = 0;
		        Item->LastAccessTime = 0;
		        Item->MftChangeTime = 0;
		        Item->ParentDirectory = ParentDirectory;
		        Item->Directory = NO;

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
		        {
			        Item->Directory = YES;
		        }
		        Item->Unmovable = NO;
		        Item->Exclude = NO;
		        Item->SpaceHog = NO;

		        / * Analyze the item: Clusters and Fragments, and the CreationTime, LastAccessTime,
		        and MftChangeTime. If the item could not be opened then ignore the item. * /
		        FileHandle = OpenItemHandle(Data,Item);

		        if (FileHandle == NULL) continue;

		        Result = GetFragments(Data,Item,FileHandle);

		        CloseHandle(FileHandle);

		        if (Result == NO) continue;

		        / * Increment counters. * /
		        Data->CountAllFiles = Data->CountAllFiles + 1;
		        Data->CountAllBytes = Data->CountAllBytes + Item->Bytes;
		        Data->CountAllClusters = Data->CountAllClusters + Item->Clusters;

		        if (IsFragmented(Item,0,Item->Clusters) == YES)
		        {
			        Data->CountFragmentedItems = Data->CountFragmentedItems + 1;
			        Data->CountFragmentedBytes = Data->CountFragmentedBytes + Item->Bytes;
			        Data->CountFragmentedClusters = Data->CountFragmentedClusters + Item->Clusters;
		        }

		        Data->PhaseDone = Data->PhaseDone + Item->Clusters;

		        / * Show progress message. * /
        //		jkGui->ShowAnalyze(Data,Item);

		        / * If it's a directory then iterate subdirectories. * /
		        if (Item->Directory == YES)
		        {
			        Data->CountDirectories = Data->CountDirectories + 1;

			        Length = wcslen(RootPath) + wcslen(FindFileData.cFileName) + 4;

			        TempPath = (WCHAR *)malloc(sizeof(WCHAR) * Length);

			        if (TempPath != NULL)
			        {
				        swprintf_s(TempPath,Length,L"%s\\%s\\*",RootPath,FindFileData.cFileName);
				        ScanDir(Data,TempPath,Item);
				        free(TempPath);
			        }
		        }

		        / * Ignore the item if it has no clusters or no LCN. Very small
		        files are stored in the MFT and are reported by Windows as
		        having zero clusters and no fragments. * /
		        if ((Item->Clusters == 0) || (Item->Fragments == NULL)) continue;

		        / * Draw the item on the screen. * /
		        //		if (*Data->RedrawScreen == 0) {
		        ColorizeItem(Data,Item,0,0,NO);
		        //		} else {
		        //			m_jkGui->ShowDiskmap(Data);
		        //		}

		        / * Show debug info about the file. * /
		        / * Show debug message: "%I64d clusters at %I64d, %I64d bytes" * /
        //		jkGui->ShowDebug(4,Item,Data->DebugMsg[16],Item->Clusters,GetItemLcn(Item),Item->Bytes);

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_COMPRESSED) != 0)
		        {
			        / * Show debug message: "Special file attribute: Compressed" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[17]);
		        }

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_ENCRYPTED) != 0)
		        {
			        / * Show debug message: "Special file attribute: Encrypted" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[18]);
		        }

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_OFFLINE) != 0)
		        {
			        / * Show debug message: "Special file attribute: Offline" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[19]);
		        }

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_READONLY) != 0)
		        {
			        / * Show debug message: "Special file attribute: Read-only" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[20]);
		        }

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_SPARSE_FILE) != 0)
		        {
			        / * Show debug message: "Special file attribute: Sparse-file" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[21]);
		        }

		        if ((FindFileData.dwFileAttributes & FILE_ATTRIBUTE_TEMPORARY) != 0)
		        {
			        / * Show debug message: "Special file attribute: Temporary" * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[22]);
		        }

		        / * Save some memory if short and long filename are the same. * /
		        if ((Item->LongFilename != NULL) &&
			        (Item->ShortFilename != NULL) &&
			        (_wcsicmp(Item->LongFilename,Item->ShortFilename) == 0))
		        {
			        free(Item->ShortFilename);
			        Item->ShortFilename = Item->LongFilename;
		        }

		        if ((Item->LongFilename == NULL) && (Item->ShortFilename != NULL)) Item->LongFilename = Item->ShortFilename;
		        if ((Item->LongFilename != NULL) && (Item->ShortFilename == NULL)) Item->ShortFilename = Item->LongFilename;

		        if ((Item->LongPath != NULL) &&
			        (Item->ShortPath != NULL) &&
			        (_wcsicmp(Item->LongPath,Item->ShortPath) == 0))
		        {
			        free(Item->ShortPath);
			        Item->ShortPath = Item->LongPath;
		        }

		        if ((Item->LongPath == NULL) && (Item->ShortPath != NULL)) Item->LongPath = Item->ShortPath;
		        if ((Item->LongPath != NULL) && (Item->ShortPath == NULL)) Item->ShortPath = Item->LongPath;

		        / * Add the item to the ItemTree in memory. * /
		        TreeInsert(Data,Item);
		        Item = NULL;

	        } while (FindNextFileW(FindHandle,&FindFileData) != 0);

	        FindClose(FindHandle);

	        / * Cleanup. * /
	        free(RootPath);

	        if (Item != NULL)
	        {
		        if (Item->ShortPath != NULL) free(Item->ShortPath);
		        if (Item->ShortFilename != NULL) free(Item->ShortFilename);
		        if (Item->LongPath != NULL) free(Item->LongPath);
		        if (Item->LongFilename != NULL) free(Item->LongFilename);

		        while (Item->Fragments != NULL)
		        {
			        Fragment = Item->Fragments->Next;

			        free(Item->Fragments);

			        Item->Fragments = Fragment;
		        }

		        free(Item);
	        }
        }
*/
        /* Scan all files in a volume and store the information in a tree in memory for later use by the optimizer. */
        void AnalyzeVolume()
        {
	        Boolean Result;
	        int i;

	        CallShowStatus(1,-1);             /* "Phase 1: Analyze" */

	        /* Fetch the current time in the ULONG64 format (1 second = 10000000). */
            DateTime Time1 = DateTime.Now;
            Int64 Time2 = Time1.ToFileTime();
            Int64 Time3 = Time2;
            Int64 SystemTime = Time3;

	        /* Scan NTFS disks. */

//            ScanNtfs ntfs = new ScanNtfs(this);

//            ntfs.AnalyzeNtfsVolume();

//            return;

            Result = m_scanNtfs.AnalyzeNtfsVolume();

	        /* Scan FAT disks. */
//	        if ((Result == false) && (*Data->Running == RUNNING)) Result = jkScanFat->AnalyzeFatVolume(Data);

/*
	        / * Scan all other filesystems. * /
	        if ((Result == FALSE) && (*Data->Running == RUNNING))
	        {
        //		jkGui->ShowDebug(0,NULL,L"This is not a FAT or NTFS disk, using the slow scanner.");

		        / * Setup the width of the progress bar. * /
		        Data->PhaseTodo = Data->TotalClusters - Data->CountFreeClusters;

		        for (i = 0; i < 3; i++)
		        {
			        Data->PhaseTodo = Data->PhaseTodo - (Data->MftExcludes[i].End - Data->MftExcludes[i].Start);
		        }

		        / * Scan all the files. * /
		        ScanDir(Data,Data->IncludeMask,NULL);
	        }

*/
	        /* Update the diskmap with the CLUSTER_COLORS. */
            Data.PhaseDone = Data.PhaseTodo;

            DrawCluster(0, Data.TotalClusters, CLUSTER_COLORS.COLOREMPTY);

	        /* Setup the progress counter and the file/dir counters. */
            Data.PhaseDone = 0;
            Data.PhaseTodo = 0;

            for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
	        {
                Data.PhaseTodo++;
	        }

        //	jkGui->ShowAnalyze(NULL,NULL);

	        /* Walk through all the items one by one. */
            for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
	        {
                if (Data.Running != RunningState.RUNNING) break;

		        /* If requested then redraw the diskmap. */
                if (Data.RedrawScreen == 1) ShowDiskmap();

		        /* Construct the full path's of the item. The MFT contains only the filename, plus
		        a pointer to the directory. We have to construct the full paths's by joining
		        all the names of the directories, and the name of the file. */
                if (Item.LongPath == null) Item.LongPath = GetLongPath(Data, Item);
                if (Item.ShortPath == null) Item.ShortPath = GetShortPath(Data, Item);

		        /* Save some memory if the short and long paths are the same. */
		        if ((Item.LongPath != null) &&
			        (Item.ShortPath != null) &&
			        (Item.LongPath != Item.ShortPath) &&
			        (Item.LongPath.CompareTo(Item.ShortPath)) == 0)
		        {
			        Item.ShortPath = Item.LongPath;
		        }

                if ((Item.LongPath == null) && (Item.ShortPath != null)) Item.LongPath = Item.ShortPath;
                if ((Item.LongPath != null) && (Item.ShortPath == null)) Item.ShortPath = Item.LongPath;

		        /* For debugging only: compare the data with the output from the
		        FSCTL_GET_RETRIEVAL_POINTERS function call. */
		        /*
		        CompareItems(Data,Item);
		        */

		        /* Apply the Mask and set the Exclude flag of all items that do not match. */
                if ((MatchMask(Item.LongPath, Data.IncludeMask) == false) &&
                    (MatchMask(Item.ShortPath, Data.IncludeMask) == false))
		        {
			        Item.Exclude = true;

			        ColorizeItem(Item,0,0,false);
		        }

		        /* Determine if the item is to be excluded by comparing it's name with the Exclude masks. */
                if ((Item.Exclude == false) && (Data.Excludes != null))
		        {
                    for (i = 0; Data.Excludes[i] != null; i++)
			        {
                        if ((MatchMask(Item.LongPath, Data.Excludes[i]) == true) ||
                            (MatchMask(Item.ShortPath, Data.Excludes[i]) == true))
				        {
					        Item.Exclude = true;

					        ColorizeItem(Item,0,0,false);

					        break;
				        }
			        }
		        }

		        /* Exclude my own logfile. */
		        if ((Item.Exclude == false) &&
			        (Item.LongFilename != null) &&
                    ((Item.LongFilename == "jkdefrag.log") ||
                    (Item.LongFilename == "jkdefragcmd.log") ||
                    (Item.LongFilename == "jkdefragscreensaver.log")))
		        {
			        Item.Exclude = true;
			        ColorizeItem(Item,0,0,false);
		        }

		        /* The item is a SpaceHog if it's larger than 50 megabytes, or last access time
		        is more than 30 days ago, or if it's filename matches a SpaceHog mask. */
		        if ((Item.Exclude == false) && (Item.Directory == false))
		        {
                    if ((Data.UseDefaultSpaceHogs == true) && (Item.Bytes > 50 * 1024 * 1024))
			        {
				        Item.SpaceHog = true;
			        }
                    else if ((Data.UseDefaultSpaceHogs == true) &&
                        (Data.UseLastAccessTime == true) &&
				        ((Int64)(Item.LastAccessTime + (Int64)(30 * 24 * 60 * 60) * 10000000) < SystemTime))
			        {
				        Item.SpaceHog = true;
			        }
                    else if (Data.SpaceHogs != null)
			        {
                        for (i = 0; Data.SpaceHogs[i] != null; i++)
				        {
                            if ((MatchMask(Item.LongPath, Data.SpaceHogs[i]) == true) ||
                                (MatchMask(Item.ShortPath, Data.SpaceHogs[i]) == true))
					        {
						        Item.SpaceHog = true;

						        break;
					        }
				        }
			        }

			        if (Item.SpaceHog == true) ColorizeItem(Item,0,0,false);
		        }

		        /* Special exception for "http://www.safeboot.com/". */
		        if (MatchMask(Item.LongPath,"*\\safeboot.fs") == true)
                    Item.Unmovable = true;

		        /* Special exception for Acronis OS Selector. */
		        if (MatchMask(Item.LongPath,"?:\\bootwiz.sys") == true)
                    Item.Unmovable = true;
		        if (MatchMask(Item.LongPath,"*\\BOOTWIZ\\*") == true)
                    Item.Unmovable = true;

		        /* Special exception for DriveCrypt by "http://www.securstar.com/". */
		        if (MatchMask(Item.LongPath,"?:\\BootAuth?.sys") == true)
                    Item.Unmovable = true;

		        /* Special exception for Symantec GoBack. */
		        if (MatchMask(Item.LongPath,"*\\Gobackio.bin") == true)
                    Item.Unmovable = true;

		        /* The $BadClus file maps the entire disk and is always unmovable. */
		        if ((Item.LongFilename == "$BadClus") ||
			        (Item.LongFilename == "$BadClus:$Bad:$DATA"))
		        {
			        Item.Unmovable = true;
		        }

		        /* Update the progress percentage. */
                Data.PhaseDone++;

        		if (Data.PhaseDone % 100 == 0) ShowDebug(1, "Phase: " + Data.PhaseDone + " / " + Data.PhaseTodo);
	        }

	        /* Force the percentage to 100%. */
            Data.PhaseDone = Data.PhaseTodo;

            DrawCluster(0,0,0);

	        /* Calculate the begin of the zone's. */
	        CalculateZones();

	        /* Call the ShowAnalyze() callback one last time. */
        //	jkGui->ShowAnalyze(Data,NULL);
        }
/*
        / * Move items to their zone. This will:
        - Defragment all fragmented files
        - Move regular files out of the directory zone.
        - Move SpaceHogs out of the directory- and regular zones.
        - Move items out of the MFT reserved zones
        * /
        void JKDefragLib::Fixup(struct DefragDataStruct *Data)
        {
	        struct ItemStruct *Item;
	        struct ItemStruct *NextItem;

	        ULONG64 ItemLcn;
	        ULONG64 GapBegin[3];
	        ULONG64 GapEnd[3];

	        int FileZone;

	        WIN32_FILE_ATTRIBUTE_DATA Attributes;

	        ULONG64 FileTime;

	        FILETIME SystemTime1;

	        ULONG64 SystemTime;
	        ULONG64 LastCalcTime;

	        int Result;

	        ULARGE_INTEGER u;

	        int MoveMe;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        CallShowStatus(Data,8,-1);               / * "Phase 3: Fixup" * /

	        / * Initialize: fetch the current time. * /
	        GetSystemTimeAsFileTime(&SystemTime1);

	        u.LowPart = SystemTime1.dwLowDateTime;
	        u.HighPart = SystemTime1.dwHighDateTime;

	        SystemTime = u.QuadPart;

	        / * Initialize the width of the progress bar: the total number of clusters
	        of all the items. * /
	        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
	        {
		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;
		        if (Item->Clusters == 0) continue;

		        Data->PhaseTodo = Data->PhaseTodo + Item->Clusters;
	        }

	        LastCalcTime = SystemTime;

	        / * Exit if nothing to do. * /
	        if (Data->PhaseTodo == 0) return;

	        / * Walk through all files and move the files that need to be moved. * /
	        for (FileZone = 0; FileZone < 3; FileZone++)
	        {
		        GapBegin[FileZone] = 0;
		        GapEnd[FileZone] = 0;
	        }

	        NextItem = TreeSmallest(Data->ItemTree);

	        while ((NextItem != NULL) && (*Data->Running == RUNNING))
	        {
		        / * The loop will change the position of the item in the tree, so we have
		        to determine the next item before executing the loop. * /
		        Item = NextItem;

		        NextItem = TreeNext(Item);

		        / * Ignore items that are unmovable or excluded. * /
		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;
		        if (Item->Clusters == 0) continue;

		        / * Ignore items that do not need to be moved. * /
		        FileZone = 1;

		        if (Item->SpaceHog == YES) FileZone = 2;
		        if (Item->Directory == YES) FileZone = 0;

		        ItemLcn = GetItemLcn(Item);

		        MoveMe = NO;

		        if (IsFragmented(Item,0,Item->Clusters) == YES)
		        {
			        / * "I am fragmented." * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[53]);

			        MoveMe = YES;
		        }

		        if ((MoveMe == NO) &&
			        (((ItemLcn >= Data->MftExcludes[0].Start) && (ItemLcn < Data->MftExcludes[0].End)) ||
			        ((ItemLcn >= Data->MftExcludes[1].Start) && (ItemLcn < Data->MftExcludes[1].End)) ||
			        ((ItemLcn >= Data->MftExcludes[2].Start) && (ItemLcn < Data->MftExcludes[2].End))) &&
			        ((Data->Disk.Type != NTFS) || (MatchMask(Item->LongPath,L"?:\\$MFT") != YES)))
		        {
			        / * "I am in MFT reserved space." * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[54]);

			        MoveMe = YES;
		        }

		        if ((FileZone == 1) && (ItemLcn < Data->Zones[1]) && (MoveMe == NO))
		        {
			        / * "I am a regular file in zone 1." * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[55]);

			        MoveMe = YES;
		        }

		        if ((FileZone == 2) && (ItemLcn < Data->Zones[2]) && (MoveMe == NO))
		        {
			        / * "I am a spacehog in zone 1 or 2." * /
        //			jkGui->ShowDebug(4,Item,Data->DebugMsg[56]);

			        MoveMe = YES;
		        }

		        if (MoveMe == NO)
		        {
			        Data->PhaseDone = Data->PhaseDone + Item->Clusters;

			        continue;
		        }

		        / * Ignore files that have been modified less than 15 minutes ago. * /
		        if (Item->Directory == NO)
		        {
			        Result = GetFileAttributesExW(Item->LongPath,GetFileExInfoStandard,&Attributes);

			        if (Result != 0)
			        {
				        u.LowPart = Attributes.ftLastWriteTime.dwLowDateTime;
				        u.HighPart = Attributes.ftLastWriteTime.dwHighDateTime;

				        FileTime = u.QuadPart;

				        if (FileTime + 15 * 60 * (ULONG64)10000000 > SystemTime)
				        {
					        Data->PhaseDone = Data->PhaseDone + Item->Clusters;

					        continue;
				        }
			        }
		        }

		        / * If the file does not fit in the current gap then find another gap. * /
		        if (Item->Clusters > GapEnd[FileZone] - GapBegin[FileZone])
		        {
			        Result = FindGap(Data,Data->Zones[FileZone],0,Item->Clusters,YES,NO,&GapBegin[FileZone],
				        &GapEnd[FileZone],FALSE);

			        if (Result == NO)
			        {
				        / * Show debug message: "Cannot move item away because no gap is big enough: %I64d[%lu]" * /
        //				jkGui->ShowDebug(2,Item,Data->DebugMsg[25],GetItemLcn(Item),Item->Clusters);

				        GapEnd[FileZone] = GapBegin[FileZone];         / * Force re-scan of gap. * /

				        Data->PhaseDone = Data->PhaseDone + Item->Clusters;

				        continue;
			        }
		        }

		        / * Move the item. * /
		        Result = MoveItem(Data,Item,GapBegin[FileZone],0,Item->Clusters,0);

		        if (Result == YES)
		        {
			        GapBegin[FileZone] = GapBegin[FileZone] + Item->Clusters;
		        }
		        else
		        {
			        GapEnd[FileZone] = GapBegin[FileZone];         / * Force re-scan of gap. * /
		        }

		        / * Get new system time. * /
		        GetSystemTimeAsFileTime(&SystemTime1);

		        u.LowPart = SystemTime1.dwLowDateTime;
		        u.HighPart = SystemTime1.dwHighDateTime;

		        SystemTime = u.QuadPart;
	        }
        }

        / * Defragment all the fragmented files. * /
        void JKDefragLib::Defragment(struct DefragDataStruct *Data)
        {
	        struct ItemStruct *Item;
	        struct ItemStruct *NextItem;

	        ULONG64 GapBegin;
	        ULONG64 GapEnd;
	        ULONG64 ClustersDone;
	        ULONG64 Clusters;

	        struct FragmentListStruct *Fragment;

	        ULONG64 Vcn;
	        ULONG64 RealVcn;

	        HANDLE FileHandle;

	        int FileZone;
	        int Result;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        CallShowStatus(Data,2,-1);               / * "Phase 2: Defragment" * /

	        / * Setup the width of the progress bar: the number of clusters in all
	        fragmented files. * /
	        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
	        {
		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;
		        if (Item->Clusters == 0) continue;

		        if (IsFragmented(Item,0,Item->Clusters) == NO) continue;

		        Data->PhaseTodo = Data->PhaseTodo + Item->Clusters;
	        }

	        / * Exit if nothing to do. * /
	        if (Data->PhaseTodo == 0) return;

	        / * Walk through all files and defrag. * /
	        NextItem = TreeSmallest(Data->ItemTree);

	        while ((NextItem != NULL) && (*Data->Running == RUNNING))
	        {
		        / * The loop may change the position of the item in the tree, so we have
		        to determine and remember the next item now. * /
		        Item = NextItem;

		        NextItem = TreeNext(Item);

		        / * Ignore if the Item cannot be moved, or is Excluded, or is not fragmented. * /
		        if (Item->Unmovable == YES) continue;
		        if (Item->Exclude == YES) continue;
		        if (Item->Clusters == 0) continue;

		        if (IsFragmented(Item,0,Item->Clusters) == NO) continue;

		        / * Find a gap that is large enough to hold the item, or the largest gap
		        on the volume. If the disk is full then show a message and exit. * /
		        FileZone = 1;

		        if (Item->SpaceHog == YES) FileZone = 2;
		        if (Item->Directory == YES) FileZone = 0;

		        Result = FindGap(Data,Data->Zones[FileZone],0,Item->Clusters,NO,NO,&GapBegin,&GapEnd,FALSE);

		        if (Result == NO)
		        {
			        / * Try finding a gap again, this time including the free area. * /
			        Result = FindGap(Data,0,0,Item->Clusters,NO,NO,&GapBegin,&GapEnd,FALSE);

			        if (Result == NO)
			        {
				        / * Show debug message: "Disk is full, cannot defragment." * /
        //				jkGui->ShowDebug(2,Item,Data->DebugMsg[44]);

				        return;
			        }
		        }

		        / * If the gap is big enough to hold the entire item then move the file
		        in a single go, and loop. * /
		        if (GapEnd - GapBegin >= Item->Clusters)
		        {
			        MoveItem(Data,Item,GapBegin,0,Item->Clusters,0);

			        continue;
		        }

		        / * Open a filehandle for the item. If error then set the Unmovable flag,
		        colorize the item on the screen, and loop. * /
		        FileHandle = OpenItemHandle(Data,Item);

		        if (FileHandle == NULL)
		        {
			        Item->Unmovable = YES;

			        ColorizeItem(Data,Item,0,0,NO);

			        continue;
		        }

		        / * Move the file in parts, each time selecting the biggest gap
		        available. * /
		        ClustersDone = 0;

		        do
		        {
			        Clusters = GapEnd - GapBegin;

			        if (Clusters > Item->Clusters - ClustersDone)
			        {
				        Clusters = Item->Clusters - ClustersDone;
			        }

			        / * Make sure that the gap is bigger than the first fragment of the
			        block that we're about to move. If not then the result would be
			        more fragments, not less. * /
			        Vcn = 0;
			        RealVcn = 0;

			        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
			        {
				        if (Fragment->Lcn != VIRTUALFRAGMENT)
				        {
					        if (RealVcn >= ClustersDone)
					        {
						        if (Clusters > Fragment->NextVcn - Vcn) break;

						        ClustersDone = RealVcn + Fragment->NextVcn - Vcn;

						        Data->PhaseDone = Data->PhaseDone + Fragment->NextVcn - Vcn;
					        }

					        RealVcn = RealVcn + Fragment->NextVcn - Vcn;
				        }

				        Vcn = Fragment->NextVcn;
			        }

			        if (ClustersDone >= Item->Clusters) break;

			        / * Move the segment. * /
			        Result = MoveItem4(Data,Item,FileHandle,GapBegin,ClustersDone,Clusters,0);

			        / * Next segment. * /
			        ClustersDone = ClustersDone + Clusters;

			        / * Find a gap large enough to hold the remainder, or the largest gap
			        on the volume. * /
			        if (ClustersDone < Item->Clusters)
			        {
				        Result = FindGap(Data,Data->Zones[FileZone],0,Item->Clusters - ClustersDone,
					        NO,NO,&GapBegin,&GapEnd,FALSE);

				        if (Result == NO) break;
			        }

		        } while ((ClustersDone < Item->Clusters) && (*Data->Running == RUNNING));

		        / * Close the item. * /
		        FlushFileBuffers(FileHandle);            / * Is this useful? Can't hurt. * /
		        CloseHandle(FileHandle);
	        }
        }

        / * Fill all the gaps at the beginning of the disk with fragments from the files above. * /
        void JKDefragLib::ForcedFill(struct DefragDataStruct *Data)
        {
	        ULONG64 GapBegin;
	        ULONG64 GapEnd;

	        struct ItemStruct *Item;
	        struct FragmentListStruct *Fragment;
	        struct ItemStruct *HighestItem;

	        ULONG64 MaxLcn;
	        ULONG64 HighestLcn;
	        ULONG64 HighestVcn;
	        ULONG64 HighestSize;
	        ULONG64 Clusters;
	        ULONG64 Vcn;
	        ULONG64 RealVcn;

	        int Result;

	        CallShowStatus(Data,3,-1);            / * "Phase 3: ForcedFill" * /

	        / * Walk through all the gaps. * /
	        GapBegin = 0;
	        MaxLcn = Data->TotalClusters;

	        while (*Data->Running == RUNNING)
	        {
		        / * Find the next gap. If there are no more gaps then exit. * /
		        Result = FindGap(Data,GapBegin,0,0,YES,NO,&GapBegin,&GapEnd,FALSE);

		        if (Result == NO) break;

		        / * Find the item with the highest fragment on disk. * /
		        HighestItem = NULL;
		        HighestLcn = 0;
		        HighestVcn = 0;
		        HighestSize = 0;

		        for (Item = TreeBiggest(Data->ItemTree); Item != NULL; Item = TreePrev(Item))
		        {
			        if (Item->Unmovable == YES) continue;
			        if (Item->Exclude == YES) continue;
			        if (Item->Clusters == 0) continue;

			        Vcn = 0;
			        RealVcn = 0;

			        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
			        {
				        if (Fragment->Lcn != VIRTUALFRAGMENT)
				        {
					        if ((Fragment->Lcn > HighestLcn) && (Fragment->Lcn < MaxLcn))
					        {
						        HighestItem = Item;
						        HighestLcn = Fragment->Lcn;
						        HighestVcn = RealVcn;
						        HighestSize = Fragment->NextVcn - Vcn;
					        }

					        RealVcn = RealVcn + Fragment->NextVcn - Vcn;
				        }

				        Vcn = Fragment->NextVcn;
			        }
		        }

		        if (HighestItem == NULL) break;

		        / * If the highest fragment is before the gap then exit, we're finished. * /
		        if (HighestLcn <= GapBegin) break;

		        / * Move as much of the item into the gap as possible. * /
		        Clusters = GapEnd - GapBegin;

		        if (Clusters > HighestSize) Clusters = HighestSize;

		        Result = MoveItem(Data,HighestItem,GapBegin,HighestVcn + HighestSize - Clusters, Clusters,0);

		        GapBegin = GapBegin + Clusters;
		        MaxLcn = HighestLcn + HighestSize - Clusters;
	        }
        }

        / * Vacate an area by moving files upward. If there are unmovable files at the Lcn then
        skip them. Then move files upward until the gap is bigger than Clusters, or when we
        encounter an unmovable file. * /
        void JKDefragLib::Vacate(struct DefragDataStruct *Data, ULONG64 Lcn, ULONG64 Clusters, BOOL IgnoreMftExcludes)
        {
	        ULONG64 TestGapBegin;
	        ULONG64 TestGapEnd;
	        ULONG64 MoveGapBegin;
	        ULONG64 MoveGapEnd;

	        struct ItemStruct *Item;
	        struct FragmentListStruct *Fragment;

	        ULONG64 Vcn;
	        ULONG64 RealVcn;

	        struct ItemStruct *BiggerItem;

	        ULONG64 BiggerBegin;
	        ULONG64 BiggerEnd;
	        ULONG64 BiggerRealVcn;
	        ULONG64 MoveTo;
	        ULONG64 DoneUntil;

	        int Result;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //	jkGui->ShowDebug(5,NULL,L"Vacating %I64u clusters starting at LCN=%I64u",Clusters,Lcn);

	        / * Sanity check. * /
	        if (Lcn >= Data->TotalClusters)
	        {
        //		jkGui->ShowDebug(1,NULL,L"Error: trying to vacate an area beyond the end of the disk.");

		        return;
	        }

	        / * Determine the point to above which we will be moving the data. We want at least the
	        end of the zone if everything was perfectly optimized, so data will not be moved
	        again and again. * /
	        MoveTo = Lcn + Clusters;

	        if (Data->Zone == 0) MoveTo = Data->Zones[1];
	        if (Data->Zone == 1) MoveTo = Data->Zones[2];

	        if (Data->Zone == 2)
	        {
		        / * Zone 2: end of disk minus all the free space. * /
		        MoveTo = Data->TotalClusters - Data->CountFreeClusters +
			        (ULONG64)(Data->TotalClusters * 2.0 * Data->FreeSpace / 100.0);
	        }

	        if (MoveTo < Lcn + Clusters) MoveTo = Lcn + Clusters;

        //	jkGui->ShowDebug(5,NULL,L"MoveTo = %I64u",MoveTo);

	        / * Loop forever. * /
	        MoveGapBegin = 0;
	        MoveGapEnd = 0;
	        DoneUntil = Lcn;

	        while (*Data->Running == RUNNING)
	        {
		        / * Find the first movable data fragment at or above the DoneUntil Lcn. If there is nothing
		        then return, we have reached the end of the disk. * /
		        BiggerItem = NULL;
		        BiggerBegin = 0;

		        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
		        {
			        if ((Item->Unmovable == YES) || (Item->Exclude == YES) || (Item->Clusters == 0))
			        {
				        continue;
			        }

			        Vcn = 0;
			        RealVcn = 0;

			        for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
			        {
				        if (Fragment->Lcn != VIRTUALFRAGMENT)
				        {
					        if ((Fragment->Lcn >= DoneUntil) &&
						        ((BiggerBegin > Fragment->Lcn) || (BiggerItem == NULL)))
					        {
						        BiggerItem = Item;
						        BiggerBegin = Fragment->Lcn;
						        BiggerEnd = Fragment->Lcn + Fragment->NextVcn - Vcn;
						        BiggerRealVcn = RealVcn;

						        if (BiggerBegin == Lcn) break;
					        }

					        RealVcn = RealVcn + Fragment->NextVcn - Vcn;
				        }

				        Vcn = Fragment->NextVcn;
			        }

			        if ((BiggerBegin != 0) && (BiggerBegin == Lcn)) break;
		        }

		        if (BiggerItem == NULL)
		        {
        //			jkGui->ShowDebug(5,NULL,L"No data found above LCN=%I64u",Lcn);

			        return;
		        }

        //		jkGui->ShowDebug(5,NULL,L"Data found at LCN=%I64u, %s",BiggerBegin,BiggerItem->LongPath);

		        / * Find the first gap above the Lcn. * /
		        Result = FindGap(Data,Lcn,0,0,YES,NO,&TestGapBegin,&TestGapEnd,IgnoreMftExcludes);

		        if (Result == NO)
		        {
        //			jkGui->ShowDebug(5,NULL,L"No gaps found above LCN=%I64u",Lcn);

			        return;
		        }

		        / * Exit if the end of the first gap is below the first movable item, the gap cannot
		        be enlarged. * /
		        if (TestGapEnd < BiggerBegin)
		        {
        // 			jkGui->ShowDebug(5,NULL,L"Cannot enlarge the gap from %I64u to %I64u (%I64u clusters) any further.",
        // 				TestGapBegin,TestGapEnd,TestGapEnd - TestGapBegin);

			        return;
		        }

		        / * Exit if the first movable item is at the end of the gap and the gap is big enough,
		        no need to enlarge any further. * /
		        if ((TestGapEnd == BiggerBegin) && (TestGapEnd - TestGapBegin >= Clusters))
		        {
        // 			jkGui->ShowDebug(5,NULL,L"Finished vacating, the gap from %I64u to %I64u (%I64u clusters) is now bigger than %I64u clusters.",
        // 				TestGapBegin,TestGapEnd,TestGapEnd - TestGapBegin,Clusters);

			        return;
		        }

		        / * Exit if we have moved the item before. We don't want a worm. * /
		        if (Lcn >= MoveTo)
		        {
        //			jkGui->ShowDebug(5,NULL,L"Stopping vacate because of possible worm.");

			        return;
		        }

		        / * Determine where we want to move the fragment to. Maybe the previously used
		        gap is big enough, otherwise we have to locate another gap. * /
		        if (BiggerEnd - BiggerBegin >= MoveGapEnd - MoveGapBegin)
		        {
			        Result = NO;

			        / * First try to find a gap above the MoveTo point. * /
			        if ((MoveTo < Data->TotalClusters) && (MoveTo >= BiggerEnd))
			        {
        //				jkGui->ShowDebug(5,NULL,L"Finding gap above MoveTo=%I64u",MoveTo);

				        Result = FindGap(Data,MoveTo,0,BiggerEnd - BiggerBegin,YES,NO,&MoveGapBegin,&MoveGapEnd,FALSE);
			        }

			        / * If no gap was found then try to find a gap as high on disk as possible, but
			        above the item. * /
			        if (Result == NO)
			        {
        //				jkGui->ShowDebug(5,NULL,L"Finding gap from end of disk above BiggerEnd=%I64u",BiggerEnd);

				        Result = FindGap(Data,BiggerEnd,0,BiggerEnd - BiggerBegin,YES,YES,&MoveGapBegin,
					        &MoveGapEnd,FALSE);
			        }

			        / * If no gap was found then exit, we cannot move the item. * /
			        if (Result == NO)
			        {
        //				jkGui->ShowDebug(5,NULL,L"No gap found.");

				        return;
			        }
		        }

		        / * Move the fragment to the gap. * /
		        Result = MoveItem(Data,BiggerItem,MoveGapBegin,BiggerRealVcn,BiggerEnd - BiggerBegin,0);

		        if (Result == YES)
		        {
			        if (MoveGapBegin < MoveTo) MoveTo = MoveGapBegin;

			        MoveGapBegin = MoveGapBegin + BiggerEnd - BiggerBegin;
		        }
		        else
		        {
			        MoveGapEnd = MoveGapBegin;         / * Force re-scan of gap. * /
		        }

		        / * Adjust the DoneUntil Lcn. We don't want an infinite loop. * /
		        DoneUntil = BiggerEnd;
	        }
        }

        / * Compare two items.
        SortField=0    Filename
        SortField=1    Filesize, smallest first
        SortField=2    Date/Time LastAccess, oldest first
        SortField=3    Date/Time LastChange, oldest first
        SortField=4    Date/Time Creation, oldest first
        Return values:
        -1   Item1 is smaller than Item2
        0    Equal
        1    Item1 is bigger than Item2
        * /
        int JKDefragLib::CompareItems(struct ItemStruct *Item1, struct ItemStruct *Item2, int SortField)
        {
	        int Result;

	        / * If one of the items is NULL then the other item is bigger. * /
	        if (Item1 == NULL) return(-1);
	        if (Item2 == NULL) return(1);

	        / * Return zero if the items are exactly the same. * /
	        if (Item1 == Item2) return(0);

	        / * Compare the SortField of the items and return 1 or -1 if they are not equal. * /
	        if (SortField == 0)
	        {
		        if ((Item1->LongPath == NULL) && (Item2->LongPath == NULL)) return(0);
		        if (Item1->LongPath == NULL) return(-1);
		        if (Item2->LongPath == NULL) return(1);

		        Result = _wcsicmp(Item1->LongPath,Item2->LongPath);

		        if (Result != 0) return(Result);
	        }

	        if (SortField == 1)
	        {
		        if (Item1->Bytes < Item2->Bytes) return(-1);
		        if (Item1->Bytes > Item2->Bytes) return(1);
	        }

	        if (SortField == 2)
	        {
		        if (Item1->LastAccessTime > Item2->LastAccessTime) return(-1);
		        if (Item1->LastAccessTime < Item2->LastAccessTime) return(1);
	        }

	        if (SortField == 3)
	        {
		        if (Item1->MftChangeTime < Item2->MftChangeTime) return(-1);
		        if (Item1->MftChangeTime > Item2->MftChangeTime) return(1);
	        }

	        if (SortField == 4)
	        {
		        if (Item1->CreationTime < Item2->CreationTime) return(-1);
		        if (Item1->CreationTime > Item2->CreationTime) return(1);
	        }

	        / * The SortField of the items is equal, so we must compare all the other fields
	        to see if they are really equal. * /
	        if ((Item1->LongPath != NULL) && (Item2->LongPath != NULL))
	        {
		        if (Item1->LongPath == NULL) return(-1);
		        if (Item2->LongPath == NULL) return(1);

		        Result = _wcsicmp(Item1->LongPath,Item2->LongPath);

		        if (Result != 0) return(Result);
	        }

	        if (Item1->Bytes < Item2->Bytes) return(-1);
	        if (Item1->Bytes > Item2->Bytes) return(1);
	        if (Item1->LastAccessTime < Item2->LastAccessTime) return(-1);
	        if (Item1->LastAccessTime > Item2->LastAccessTime) return(1);
	        if (Item1->MftChangeTime < Item2->MftChangeTime) return(-1);
	        if (Item1->MftChangeTime > Item2->MftChangeTime) return(1);
	        if (Item1->CreationTime < Item2->CreationTime) return(-1);
	        if (Item1->CreationTime > Item2->CreationTime) return(1);

	        / * As a last resort compare the location on harddisk. * /
	        if (GetItemLcn(Item1) < GetItemLcn(Item2)) return(-1);
	        if (GetItemLcn(Item1) > GetItemLcn(Item2)) return(1);

	        return(0);
        }

        / * Optimize the volume by moving all the files into a sorted order.
        SortField=0    Filename
        SortField=1    Filesize
        SortField=2    Date/Time LastAccess
        SortField=3    Date/Time LastChange
        SortField=4    Date/Time Creation
        * /
        void JKDefragLib::OptimizeSort(struct DefragDataStruct *Data, int SortField)
        {
	        struct ItemStruct *Item;
	        struct ItemStruct *PreviousItem;
	        struct ItemStruct *TempItem;

	        ULONG64 Lcn;
	        ULONG64 VacatedUntil;
	        ULONG64 PhaseTemp;
	        ULONG64 GapBegin;
	        ULONG64 GapEnd;
	        ULONG64 Clusters;
	        ULONG64 ClustersDone;
	        ULONG64 MinimumVacate;

	        int Result;
	        int FileZone;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Sanity check. * /
	        if (Data->ItemTree == NULL) return;

	        / * Process all the zones. * /
	        VacatedUntil = 0;
	        MinimumVacate = Data->TotalClusters / 200;

	        for (Data->Zone = 0; Data->Zone < 3; Data->Zone++)
	        {
		        CallShowStatus(Data,4,Data->Zone);            / * "Zone N: Sort" * /

		        / * Start at the begin of the zone and move all the items there, one by one
		        in the requested sorting order, making room as we go. * /
		        PreviousItem = NULL;

		        Lcn = Data->Zones[Data->Zone];

		        GapBegin = 0;
		        GapEnd = 0;

		        while (*Data->Running == RUNNING)
		        {
			        / * Find the next item that we want to place. * /
			        Item = NULL;
			        PhaseTemp = 0;

			        for (TempItem = TreeSmallest(Data->ItemTree); TempItem != NULL; TempItem = TreeNext(TempItem))
			        {
				        if (TempItem->Unmovable == YES) continue;
				        if (TempItem->Exclude == YES) continue;
				        if (TempItem->Clusters == 0) continue;

				        FileZone = 1;

				        if (TempItem->SpaceHog == YES) FileZone = 2;
				        if (TempItem->Directory == YES) FileZone = 0;
				        if (FileZone != Data->Zone) continue;

				        if ((PreviousItem != NULL) &&
					        (CompareItems(PreviousItem,TempItem,SortField) >= 0))
				        {
					        continue;
				        }

				        PhaseTemp = PhaseTemp + TempItem->Clusters;

				        if ((Item != NULL) && (CompareItems(TempItem,Item,SortField) >= 0)) continue;

				        Item = TempItem;
			        }

			        if (Item == NULL)
			        {
        //				jkGui->ShowDebug(2,NULL,L"Finished sorting zone %u.",Data->Zone+1);

				        break;
			        }

			        PreviousItem = Item;
			        Data->PhaseTodo = Data->PhaseDone + PhaseTemp;

			        / * If the item is already at the Lcn then skip. * /
			        if (GetItemLcn(Item) == Lcn)
			        {
				        Lcn = Lcn + Item->Clusters;

				        continue;
			        }

			        / * Move the item to the Lcn. If the gap at Lcn is not big enough then fragment
			        the file into whatever gaps are available. * /
			        ClustersDone = 0;

			        while ((*Data->Running == RUNNING) &&
				        (ClustersDone < Item->Clusters) &&
				        (Item->Unmovable == NO))
			        {
				        if (ClustersDone > 0)
				        {
        // 					jkGui->ShowDebug(5,NULL,L"Item partially placed, %I64u clusters more to do",
        // 						Item->Clusters - ClustersDone);
				        }

				        / * Call the Vacate() function to make a gap at Lcn big enough to hold the item.
				        The Vacate() function may not be able to move whatever is now at the Lcn, so
				        after calling it we have to locate the first gap after the Lcn. * /
				        if (GapBegin + Item->Clusters - ClustersDone + 16 > GapEnd)
				        {
					        Vacate(Data,Lcn,Item->Clusters - ClustersDone + MinimumVacate,FALSE);

					        Result = FindGap(Data,Lcn,0,0,YES,NO,&GapBegin,&GapEnd,FALSE);

					        if (Result == NO) return;              / * No gaps found, exit. * /
				        }

				        / * If the gap is not big enough to hold the entire item then calculate how much
				        of the item will fit in the gap. * /
				        Clusters = Item->Clusters - ClustersDone;

				        if (Clusters > GapEnd - GapBegin)
				        {
					        Clusters = GapEnd - GapBegin;

					        / * It looks like a partial move only succeeds if the number of clusters is a
					        multiple of 8. * /
					        Clusters = Clusters - (Clusters % 8);

					        if (Clusters == 0)
					        {
						        Lcn = GapEnd;
						        continue;
					        }
				        }

				        / * Move the item to the gap. * /
				        Result = MoveItem(Data,Item,GapBegin,ClustersDone,Clusters,0);

				        if (Result == YES)
				        {
					        GapBegin = GapBegin + Clusters;
				        }
				        else
				        {
					        Result = FindGap(Data,GapBegin,0,0,YES,NO,&GapBegin,&GapEnd,FALSE);
					        if (Result == NO) return;              / * No gaps found, exit. * /
				        }

				        Lcn = GapBegin;
				        ClustersDone = ClustersDone + Clusters;
			        }
		        }
	        }
        }

        / *

        Move the MFT to the beginning of the harddisk.
        - The Microsoft defragmentation api only supports moving the MFT on Vista.
        - What to do if there is unmovable data at the beginning of the disk? I have
        chosen to wrap the MFT around that data. The fragments will be aligned, so
        the performance loss is minimal, and still faster than placing the MFT
        higher on the disk.

        * /
        void JKDefragLib::MoveMftToBeginOfDisk(struct DefragDataStruct *Data)
        {
	        struct ItemStruct *Item;

	        ULONG64 Lcn;
	        ULONG64 GapBegin;
	        ULONG64 GapEnd;
	        ULONG64 Clusters;
	        ULONG64 ClustersDone;

	        int Result;

	        OSVERSIONINFO OsVersion;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //	jkGui->ShowDebug(2,NULL,L"Moving the MFT to the beginning of the volume.");

	        / * Exit if this is not an NTFS disk. * /
	        if (Data->Disk.Type != NTFS)
	        {
        //		jkGui->ShowDebug(5,NULL,L"Cannot move the MFT because this is not an NTFS disk.");

		        return;
	        }

	        / * The Microsoft defragmentation api only supports moving the MFT on Vista. * /
	        ZeroMemory(&OsVersion,sizeof(OSVERSIONINFO));

	        OsVersion.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

	        if ((GetVersionEx(&OsVersion) != 0) && (OsVersion.dwMajorVersion < 6))
	        {
        //		jkGui->ShowDebug(5,NULL,L"Cannot move the MFT because it is not supported by this version of Windows.");

		        return;
	        }

	        / * Locate the Item for the MFT. If not found then exit. * /
	        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
	        {
		        if (MatchMask(Item->LongPath,L"?:\\$MFT") == YES) break;
	        }

	        if (Item == NULL)
	        {
        //		jkGui->ShowDebug(5,NULL,L"Cannot move the MFT because I cannot find it.");

		        return;
	        }

	        / * Exit if the MFT is at the beginning of the volume (inside zone 0) and is not
	        fragmented. * /
/ *
        #ifdef jk
	        if ((Item->Fragments != NULL) &&
		        (Item->Fragments->NextVcn == Data->Disk.MftLockedClusters) &&
		        (Item->Fragments->Next != NULL) &&
		        (Item->Fragments->Next->Lcn < Data->Zones[1]) &&
		        (IsFragmented(Item,Data->Disk.MftLockedClusters,Item->Clusters - Data->Disk.MftLockedClusters) == NO)) {
			        m_jkGui->ShowDebug(5,NULL,L"No need to move the MFT because it's already at the beginning of the volume and it's data part is not fragmented.");
			        return;
	        }
        #endif
* /

	        Lcn = 0;
	        GapBegin = 0;
	        GapEnd = 0;
	        ClustersDone = Data->Disk.MftLockedClusters;

	        while ((*Data->Running == RUNNING) && (ClustersDone < Item->Clusters))
	        {
		        if (ClustersDone > Data->Disk.MftLockedClusters)
		        {
        // 			jkGui->ShowDebug(5,NULL,L"Partially placed, %I64u clusters more to do",
        // 				Item->Clusters - ClustersDone);
		        }

		        / * Call the Vacate() function to make a gap at Lcn big enough to hold the MFT.
		        The Vacate() function may not be able to move whatever is now at the Lcn, so
		        after calling it we have to locate the first gap after the Lcn. * /
		        if (GapBegin + Item->Clusters - ClustersDone + 16 > GapEnd)
		        {
			        Vacate(Data,Lcn,Item->Clusters - ClustersDone,TRUE);

			        Result = FindGap(Data,Lcn,0,0,YES,NO,&GapBegin,&GapEnd,TRUE);

			        if (Result == NO) return;              / * No gaps found, exit. * /
		        }

		        / * If the gap is not big enough to hold the entire MFT then calculate how much
		        will fit in the gap. * /
		        Clusters = Item->Clusters - ClustersDone;

		        if (Clusters > GapEnd - GapBegin)
		        {
			        Clusters = GapEnd - GapBegin;
			        / * It looks like a partial move only succeeds if the number of clusters is a
			        multiple of 8. * /
			        Clusters = Clusters - (Clusters % 8);

			        if (Clusters == 0)
			        {
				        Lcn = GapEnd;

				        continue;
			        }
		        }

		        / * Move the MFT to the gap. * /
		        Result = MoveItem(Data,Item,GapBegin,ClustersDone,Clusters,0);

		        if (Result == YES)
		        {
			        GapBegin = GapBegin + Clusters;
		        }
		        else
		        {
			        Result = FindGap(Data,GapBegin,0,0,YES,NO,&GapBegin,&GapEnd,TRUE);

			        if (Result == NO) return;              / * No gaps found, exit. * /
		        }

		        Lcn = GapBegin;
		        ClustersDone = ClustersDone + Clusters;
	        }

	        / * Make the MFT unmovable. We don't want it moved again by any other subroutine. * /
	        Item->Unmovable = YES;

	        ColorizeItem(Data,Item,0,0,NO);
	        CalculateZones(Data);

	        / * Note: The MftExcludes do not change by moving the MFT. * /
        }

        / * Optimize the harddisk by filling gaps with files from above. * /
        void JKDefragLib::OptimizeVolume(struct DefragDataStruct *Data)
        {
	        int Zone;

	        struct ItemStruct *Item;

	        ULONG64 GapBegin;
	        ULONG64 GapEnd;

	        int Result;
	        int Retry;
	        int PerfectFit;

	        ULONG64 PhaseTemp;

	        int FileZone;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        / * Sanity check. * /
	        if (Data->ItemTree == NULL) return;

	        / * Process all the zones. * /
	        for (Zone = 0; Zone < 3; Zone++)
	        {
		        CallShowStatus(Data,5,Zone);            / * "Zone N: Fast Optimize" * /

		        / * Walk through all the gaps. * /
		        GapBegin = Data->Zones[Zone];
		        Retry = 0;

		        while (*Data->Running == RUNNING)
		        {
			        / * Find the next gap. * /
			        Result = FindGap(Data,GapBegin,0,0,YES,NO,&GapBegin,&GapEnd,FALSE);

			        if (Result == NO) break;

			        / * Update the progress counter: the number of clusters in all the files
			        above the gap. Exit if there are no more files. * /
			        PhaseTemp = 0;

			        for (Item = TreeBiggest(Data->ItemTree); Item != NULL; Item = TreePrev(Item))
			        {
				        if (GetItemLcn(Item) < GapEnd) break;
				        if (Item->Unmovable == YES) continue;
				        if (Item->Exclude == YES) continue;

				        FileZone = 1;

				        if (Item->SpaceHog == YES) FileZone = 2;
				        if (Item->Directory == YES) FileZone = 0;
				        if (FileZone != Zone) continue;

				        PhaseTemp = PhaseTemp + Item->Clusters;
			        }

			        Data->PhaseTodo = Data->PhaseDone + PhaseTemp;
			        if (PhaseTemp == 0) break;

			        / * Loop until the gap is filled. First look for combinations of files that perfectly
			        fill the gap. If no combination can be found, or if there are less files than
			        the gap is big, then fill with the highest file(s) that fit in the gap. * /
			        PerfectFit = YES;
			        if (GapEnd - GapBegin > PhaseTemp) PerfectFit = NO;

			        while ((GapBegin < GapEnd) && (Retry < 5) && (*Data->Running == RUNNING))
			        {
				        / * Find the Item that is the best fit for the gap. If nothing found (no files
				        fit the gap) then exit the loop. * /
				        if (PerfectFit == YES)
				        {
					        Item = FindBestItem(Data,GapBegin,GapEnd,1,Zone);

					        if (Item == NULL)
					        {
						        PerfectFit = NO;

						        Item = FindHighestItem(Data,GapBegin,GapEnd,1,Zone);
					        }
				        }
				        else
				        {
					        Item = FindHighestItem(Data,GapBegin,GapEnd,1,Zone);
				        }

				        if (Item == NULL) break;

				        / * Move the item. * /
				        Result = MoveItem(Data,Item,GapBegin,0,Item->Clusters,0);

				        if (Result == YES)
				        {
					        GapBegin = GapBegin + Item->Clusters;
					        Retry = 0;
				        }
				        else
				        {
					        GapEnd = GapBegin;   / * Force re-scan of gap. * /
					        Retry = Retry + 1;
				        }
			        }

			        / * If the gap could not be filled then skip. * /
			        if (GapBegin < GapEnd)
			        {
				        / * Show debug message: "Skipping gap, cannot fill: %I64d[%I64d]" * /
        //				jkGui->ShowDebug(5,NULL,Data->DebugMsg[28],GapBegin,GapEnd - GapBegin);

				        GapBegin = GapEnd;
				        Retry = 0;
			        }
		        }
	        }
        }

        / * Optimize the harddisk by moving the selected items up. * /
        void JKDefragLib::OptimizeUp(struct DefragDataStruct *Data)
        {
	        struct ItemStruct *Item;

	        ULONG64 GapBegin;
	        ULONG64 GapEnd;

	        int Result;
	        int Retry;
	        int PerfectFit;

	        ULONG64 PhaseTemp;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        CallShowStatus(Data,6,-1);            / * "Phase 3: Move Up" * /

	        / * Setup the progress counter: the total number of clusters in all files. * /
	        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
	        {
		        Data->PhaseTodo = Data->PhaseTodo + Item->Clusters;
	        }

	        / * Exit if nothing to do. * /
	        if (Data->ItemTree == NULL) return;

	        / * Walk through all the gaps. * /
	        GapEnd = Data->TotalClusters;
	        Retry = 0;

	        while (*Data->Running == RUNNING)
	        {
		        / * Find the previous gap. * /
		        Result = FindGap(Data,Data->Zones[1],GapEnd,0,YES,YES,&GapBegin,&GapEnd,FALSE);

		        if (Result == NO) break;

		        / * Update the progress counter: the number of clusters in all the files
		        below the gap. * /
		        PhaseTemp = 0;

		        for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
		        {
			        if (Item->Unmovable == YES) continue;
			        if (Item->Exclude == YES) continue;
			        if (GetItemLcn(Item) >= GapEnd) break;

			        PhaseTemp = PhaseTemp + Item->Clusters;
		        }

		        Data->PhaseTodo = Data->PhaseDone + PhaseTemp;
		        if (PhaseTemp == 0) break;

		        / * Loop until the gap is filled. First look for combinations of files that perfectly
		        fill the gap. If no combination can be found, or if there are less files than
		        the gap is big, then fill with the highest file(s) that fit in the gap. * /
		        PerfectFit = YES;
		        if (GapEnd - GapBegin > PhaseTemp) PerfectFit = NO;

		        while ((GapBegin < GapEnd) && (Retry < 5) && (*Data->Running == RUNNING))
		        {
			        / * Find the Item that is the best fit for the gap. If nothing found (no files
			        fit the gap) then exit the loop. * /
			        if (PerfectFit == YES)
			        {
				        Item = FindBestItem(Data,GapBegin,GapEnd,0,3);

				        if (Item == NULL)
				        {
					        PerfectFit = NO;
					        Item = FindHighestItem(Data,GapBegin,GapEnd,0,3);
				        }
			        }
			        else
			        {
				        Item = FindHighestItem(Data,GapBegin,GapEnd,0,3);
			        }

			        if (Item == NULL) break;

			        / * Move the item. * /
			        Result = MoveItem(Data,Item,GapEnd - Item->Clusters,0,Item->Clusters,1);

			        if (Result == YES)
			        {
				        GapEnd = GapEnd - Item->Clusters;
				        Retry = 0;
			        }
			        else
			        {
				        GapBegin = GapEnd;   / * Force re-scan of gap. * /
				        Retry = Retry + 1;
			        }
		        }

		        / * If the gap could not be filled then skip. * /
		        if (GapBegin < GapEnd)
		        {
			        / * Show debug message: "Skipping gap, cannot fill: %I64d[%I64d]" * /
        //			jkGui->ShowDebug(5,NULL,Data->DebugMsg[28],GapBegin,GapEnd - GapBegin);

			        GapEnd = GapBegin;
			        Retry = 0;
		        }
	        }
        }
*/
        /// <summary>
        /// Run the defragmenter. Input is the name of a disk, mountpoint, directory, or file,
        /// and may contain wildcards '*' and '?'.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Mode"></param>
        void DefragOnePath(String Path, UInt16 Mode)
        {
            #region Initialize Variables

            int i;

	        /*  Initialize the data. Some items are inherited from the caller and are not initialized. */
	        Data.Phase = 0;

            Data.Disk = new Disk();

            Data.ItemTree = null;

            Data.BalanceCount = 0;

            Data.MftExcludes = new List<ExcludesStruct>();

            Data.MftExcludes.Add(new ExcludesStruct());
            Data.MftExcludes.Add(new ExcludesStruct());
            Data.MftExcludes.Add(new ExcludesStruct());

            Data.MftExcludes[0].Start = 0;
            Data.MftExcludes[0].End = 0;
            Data.MftExcludes[1].Start = 0;
            Data.MftExcludes[1].End = 0;
            Data.MftExcludes[2].Start = 0;
            Data.MftExcludes[2].End = 0;

            Data.TotalClusters = 0;
            Data.BytesPerCluster = 0;

            for (i = 0; i < 3; i++) Data.Zones[i] = 0;

            Data.CannotMoveDirs = 0;
            Data.CountDirectories = 0;
            Data.CountAllFiles = 0;
            Data.CountFragmentedItems = 0;
            Data.CountAllBytes = 0;
            Data.CountFragmentedBytes = 0;
            Data.CountAllClusters = 0;
            Data.CountFragmentedClusters = 0;
            Data.CountFreeClusters = 0;
            Data.CountGaps = 0;
            Data.BiggestGap = 0;
            Data.CountGapsLess16 = 0;
            Data.CountClustersLess16 = 0;
            Data.PhaseTodo = 0;
            Data.PhaseDone = 0;

            DateTime Time = System.DateTime.Now;

            Data.LastCheckpoint = Data.StartTime;
            Data.RunningTime = 0;

            #endregion

            #region Excludes

            /* Compare the item with the Exclude masks. If a mask matches then return,
             * ignoring the item. */
            if (Data.Excludes != null)
	        {
                String matchedExclude = null;

                foreach (String exclude in Data.Excludes)
                {
                    if (MatchMask(Path,exclude) == true) break;

                    if ((exclude.Equals("*")) &&
                        (exclude.Length <= 3) &&
                        (exclude[0].ToString().ToLower().Equals(Path[0].ToString().ToLower())))
                    {
                        matchedExclude = exclude;

                        break;
                    }
                }

                if (matchedExclude != null)
                {
                    /* Show debug message: "Ignoring volume '%s' because of exclude mask '%s'." */
                    //Data->ShowDebug(0, NULL, Data->DebugMsg[47], Path, Data->Excludes[i]);

                    return;
                }
            }

            #endregion

            /* Clear the screen and show "Processing '%s'" message. */
            //jkGui->ClearScreen(Data->DebugMsg[14],Path);

	        /* Try to change our permissions so we can access special files and directories
             * such as "C:\System Volume Information". If this does not succeed then quietly
             * continue, we'll just have to do with whatever permissions we have.
             * SE_BACKUP_NAME = Backup and Restore Privileges.*/
            IO.IOWrapper.ElevatePermissions();

            Data.Disk.MountPoint = Path;

            #region Old Code

            /*
                        if ((OpenProcessToken(GetCurrentProcess(),TOKEN_ADJUST_PRIVILEGES|TOKEN_QUERY,
                            ref ProcessTokenHandle) != 0) &&
                            (LookupPrivilegeValue(null,SE_BACKUP_NAME,ref TakeOwnershipValue) != 0))
                        {
                            TokenPrivileges.PrivilegeCount = 1;
                            TokenPrivileges.Luid = TakeOwnershipValue;
                            TokenPrivileges.Attributes = SE_PRIVILEGE_ENABLED;

                            if (AdjustTokenPrivileges(ProcessTokenHandle,0,ref TokenPrivileges,
                                Marshal.SizeOf(TokenPrivileges), 0, 0) == 0)
                            {
            //                    Data->ShowDebug(3, NULL," Info: could not elevate to SeBackupPrivilege.");
                            }
                        }
                        else
                        {
            //                Data->ShowDebug(3, NULL, "Info: could not elevate to SeBackupPrivilege.");
                        }
            */
            /* Try finding the MountPoint by treating the input path as a path to
                something on the disk. If this does not succeed then use the Path as
                a literal MountPoint name. */
            //            Data.Disk.MountPoint = Path;

            //            if (Data.Disk.MountPoint == null) return;

            //            Result = GetVolumePathNameW(Path, Data.Disk.MountPoint, Data.Disk.MountPoint.Length);

            //            if (Result == 0) Data.Disk.MountPoint = Path;

            //            /* Make two versions of the MountPoint, one with a trailing backslash and one
            //                without. */

            //            Data.Disk.MountPoint = Data.Disk.MountPoint.TrimEnd("\\\0".ToCharArray());

            //            Data.Disk.MountPointSlash = Data.Disk.MountPoint + "\\";

            //            Data.Disk.VolumeNameSlash = "";

            //            Data.Disk.VolumeNameSlash = Data.Disk.VolumeNameSlash.PadLeft(260,' ');

            //            /* Determine the name of the volume (something like
            //                "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}\"). */
            //            Result = GetVolumeNameForVolumeMountPointW(Data.Disk.MountPointSlash, Data.Disk.VolumeNameSlash, 260);

            //            Data.Disk.VolumeNameSlash = Data.Disk.VolumeNameSlash.TrimEnd(" \0".ToCharArray());

            //            if (Result == 0)
            //            {
            //                if (Data.Disk.MountPointSlash.Length > 52 - 1 - 4)
            //                {
            //                    /* "Cannot find volume name for mountpoint '%s': %s" */
            ////                    SystemErrorStr(GetLastError(),s1,BUFSIZ);
            ////                    Data->ShowDebug(0,NULL,Data->DebugMsg[40],Data->Disk.MountPointSlash,s1);
            //                    Data.Disk.MountPoint = "";
            //                    Data.Disk.MountPointSlash = "";

            //                    return;
            //                }

            //                Data.Disk.VolumeNameSlash = Data.Disk.MountPointSlash + "\\\\.\\";
            //            }

            //            /* Make a copy of the VolumeName without the trailing backslash. */
            //            Data.Disk.VolumeName = Data.Disk.VolumeNameSlash.TrimEnd("\\\0".ToCharArray());

            //            /* Exit if the disk is hybernated (if "?/hiberfil.sys" exists and does not begin
            //                with 4 zero bytes). */
            //            String hibernateFileName = Data.Disk.MountPointSlash + "\\hiberfil.sys";

            //            FileStream fileStream = null;

            //            try
            //            {
            //                byte []buffer={0,0,0,0};
            //                int res = 0;

            //                fileStream = new FileStream(@hibernateFileName, FileMode.Open);

            //                fileStream.Read(buffer, 0, 4);

            //                for(int ii=0; ii<4; ii++)
            //                {

            //                    res += buffer[ii];
            //                }

            //                if (res != 0)
            //                {
            ////                    Data->ShowDebug(0, NULL, "Will not process this disk, it contains hibernated data.");

            //                    Data.Disk.MountPoint = "";
            //                    Data.Disk.MountPointSlash = "";

            //                    fileStream.Close();

            //                    return;
            //                }
            //            }
            //            catch (System.IO.IOException e)
            //            {
            //                // Data->ShowDebug(0, NULL, "Could not read from hibernate file.");
            //            }
            //            finally
            //            {
            //                if (fileStream != null)
            //                {
            //                    fileStream.Close();
            //                }
            //            }
            #endregion

            BitArray bitmap = Data.Disk.VolumeBitmap.Buffer;

            #region Old Code

            /* Show debug message: "Opening volume '%s' at mountpoint '%s'" */
            // Data->ShowDebug(0,NULL,Data->DebugMsg[29],Data->Disk.VolumeName,Data->Disk.MountPoint);

            //            /* Open the VolumeHandle. If error then leave. */
            //            SECURITY_ATTRIBUTES secAttributes = new SECURITY_ATTRIBUTES();
            //            IntPtr hTemplateFile = new IntPtr();

            //            Data.Disk.VolumeHandle = CreateFileW(Data.Disk.VolumeName,GENERIC_READ,
            //                FILE_SHARE_READ | FILE_SHARE_WRITE, ref secAttributes, OPEN_EXISTING, 0, hTemplateFile);

            //            if (Data.Disk.VolumeHandle == INVALID_HANDLE_VALUE)
            //            {
            //                SystemErrorStr(GetLastError(),s1,BUFSIZ);

            //                Data->ShowDebug(1,NULL,"Cannot open volume '%s' at mountpoint '%s': %s",
            //                    Data->Disk.VolumeName,Data->Disk.MountPoint,s1);

            //                Data.Disk.MountPoint = String.Empty;
            //                Data.Disk.MountPointSlash = String.Empty;

            //                return;
            //            }
            /* Determine the maximum LCN (maximum cluster number). A single call to
                FSCTL_GET_VOLUME_BITMAP is enough, we don't have to walk through the
                entire bitmap.
                It's a pity we have to do it in this roundabout manner, because
                there is no system call that reports the total number of clusters
                in a volume. GetDiskFreeSpace() does, but is limited to 2Gb volumes,
                GetDiskFreeSpaceEx() reports in bytes, not clusters, _getdiskfree()
                requires a drive letter so cannot be used on unmounted volumes or
                volumes that are mounted on a directory, and FSCTL_GET_NTFS_VOLUME_DATA
                only works for NTFS volumes. */

            //            BitmapParam.StartingLcn.QuadPart = 0;

            //            BitmapData bitmapData = new BitmapData();
            //            OVERLAPPED ov = new OVERLAPPED();
            //            UInt64 dataCount = new UInt64();

            //            ErrorCode = DeviceIoControl(Data.Disk.VolumeHandle, FSCTL_GET_VOLUME_BITMAP,
            //                BitmapParam, Marshal.SizeOf(BitmapParam), bitmapData, Marshal.SizeOf(bitmapData), dataCount, ref ov);

            //            if (ErrorCode != 0)
            //            {
            //                ErrorCode = 0;
            //}
            //else
            //{
            //    ErrorCode = GetLastError(); // Marshal.GetLastWin32Error();
            //}

            //            if ((ErrorCode != 0) && (ErrorCode != ERROR_MORE_DATA))
            //            {
            //                /* Show debug message: "Cannot defragment volume '%s' at mountpoint '%s'" */
            ////                Data->ShowDebug(0,NULL,Data->DebugMsg[32],Data->Disk.VolumeName,Data->Disk.MountPoint);

            //                CloseHandle(Data.Disk.VolumeHandle);

            //                Data.Disk.MountPoint = String.Empty;
            //                Data.Disk.MountPointSlash = String.Empty;

            //                return;
            //            }

            #endregion

            Data.TotalClusters = (UInt64)bitmap.Count/*bitmap.StartingLcn + bitmap.BitmapSize*/;

            IO.IOWrapper.NTFS_VOLUME_DATA_BUFFER ntfsData = Data.Disk.NtfsVolumeData;

            Data.BytesPerCluster = ntfsData.BytesPerCluster;

            Data.MftExcludes[0].Start = ntfsData.MftStartLcn;
            Data.MftExcludes[0].End = ntfsData.MftStartLcn + (UInt64)(ntfsData.MftValidDataLength / ntfsData.BytesPerCluster);

            Data.MftExcludes[1].Start = ntfsData.MftZoneStart;
            Data.MftExcludes[1].End = ntfsData.MftZoneEnd;

            Data.MftExcludes[2].Start = ntfsData.Mft2StartLcn;
            Data.MftExcludes[2].End = ntfsData.Mft2StartLcn + (UInt64)(ntfsData.MftValidDataLength / ntfsData.BytesPerCluster);

            #region old code
#if AAA


            /* Determine the number of bytes per cluster.
                Again I have to do this in a roundabout manner. As far as I know there is
                no system call that returns the number of bytes per cluster, so first I have
                to get the total size of the disk and then divide by the number of clusters.
            */

            ErrorCode = GetDiskFreeSpaceExW(Path,(PULARGE_INTEGER)&FreeBytesToCaller,
                (PULARGE_INTEGER)&TotalBytes,(PULARGE_INTEGER)&FreeBytes);

            if (ErrorCode != 0) Data->BytesPerCluster = TotalBytes / Data->TotalClusters;

            /* Setup the list of clusters that cannot be used. The Master File
                Table cannot be moved and cannot be used by files. All this is
                only necessary for NTFS volumes. */
            ErrorCode = DeviceIoControl(Data->Disk.VolumeHandle,FSCTL_GET_NTFS_VOLUME_DATA,
                NULL,0,&NtfsData,sizeof(NtfsData),&w,NULL);

            if (ErrorCode != 0)
            {
                /* Note: NtfsData.TotalClusters.QuadPart should be exactly the same
                as the Data->TotalClusters that was determined in the previous block. */

                Data->BytesPerCluster = NtfsData.BytesPerCluster;

                Data->MftExcludes[0].Start = NtfsData.MftStartLcn.QuadPart;
                Data->MftExcludes[0].End = NtfsData.MftStartLcn.QuadPart +
                    NtfsData.MftValidDataLength.QuadPart / NtfsData.BytesPerCluster;
                Data->MftExcludes[1].Start = NtfsData.MftZoneStart.QuadPart;
                Data->MftExcludes[1].End = NtfsData.MftZoneEnd.QuadPart;
                Data->MftExcludes[2].Start = NtfsData.Mft2StartLcn.QuadPart;
                Data->MftExcludes[2].End = NtfsData.Mft2StartLcn.QuadPart +
                NtfsData.MftValidDataLength.QuadPart / NtfsData.BytesPerCluster;

                /* Show debug message: "MftStartLcn=%I64d, MftZoneStart=%I64d, MftZoneEnd=%I64d, Mft2StartLcn=%I64d, MftValidDataLength=%I64d" */
                Data->ShowDebug(3,NULL,Data->DebugMsg[33],
                    NtfsData.MftStartLcn.QuadPart,NtfsData.MftZoneStart.QuadPart,
                    NtfsData.MftZoneEnd.QuadPart,NtfsData.Mft2StartLcn.QuadPart,
                    NtfsData.MftValidDataLength.QuadPart / NtfsData.BytesPerCluster);

                /* Show debug message: "MftExcludes[%u].Start=%I64d, MftExcludes[%u].End=%I64d" */
                Data->ShowDebug(3,NULL,Data->DebugMsg[34],0,Data->MftExcludes[0].Start,0,Data->MftExcludes[0].End);
                Data->ShowDebug(3,NULL,Data->DebugMsg[34],1,Data->MftExcludes[1].Start,1,Data->MftExcludes[1].End);
                Data->ShowDebug(3,NULL,Data->DebugMsg[34],2,Data->MftExcludes[2].Start,2,Data->MftExcludes[2].End);
            }

#endif
            #endregion

            /* Fixup the input mask.
             *  - If the length is 2 or 3 characters then rewrite into "c:\*".
             *  - If it does not contain a wildcard then append '*'.  */
            Data.IncludeMask = Path;

            if ((Path.Length == 2) || (Path.Length == 3))
            {
                Data.IncludeMask = Path.ToLower()[0] + ":\\*";
            }
            else
                if (Path.IndexOf('*') < 0)
                {
                    Data.IncludeMask = Path + "*";
                }

            ShowDebug(0, "Input mask: " + Data.IncludeMask);

            /* Defragment and optimize. */
            ShowDiskmap();

            if (Data.Running == RunningState.RUNNING)
            {
                AnalyzeVolume();

/*                switch (Mode)
                {
                    case 1:
                        //                        Defragment(Data);
                        break;
                    case 2:
                    case 3:
                    //                        Defragment(Data);

                    //                        Fixup(Data);
                    //                        OptimizeVolume(Data);
                    //                        Fixup(Data);     / * Again, in case of new zone startpoint. * /
                    default:
                        break;
                }
 */
            }

            #region TO BE INCLUDED

            //if ((*Data->Running == RUNNING) && (Mode == 1))
            //{
            //    Defragment(Data);
            //}

            //if ((*Data->Running == RUNNING) && ((Mode == 2) || (Mode == 3)))
            //{
            //    Defragment(Data);

            //    if (*Data->Running == RUNNING) Fixup(Data);
            //    if (*Data->Running == RUNNING) OptimizeVolume(Data);
            //    if (*Data->Running == RUNNING) Fixup(Data);     /* Again, in case of new zone startpoint. */
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 4))
            //{
            //    ForcedFill(Data);
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 5))
            //{
            //    OptimizeUp(Data);
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 6))
            //{
            //    OptimizeSort(Data,0);                        /* Filename */
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 7))
            //{
            //    OptimizeSort(Data,1);                        /* Filesize */
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 8))
            //{
            //    OptimizeSort(Data,2);                     /* Last access */
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 9))
            //{
            //    OptimizeSort(Data,3);                     /* Last change */
            //}

            //if ((*Data->Running == RUNNING) && (Mode == 10))
            //{
            //    OptimizeSort(Data,4);                        /* Creation */
            //}

            ///*
            //if ((*Data->Running == RUNNING) && (Mode == 11))
            //{
            //    MoveMftToBeginOfDisk(Data);
            //}
            //*/

            //CallShowStatus(Data,7,-1);                     /* "Finished." */

            ///* Close the volume handles. */
            //if ((Data->Disk.VolumeHandle != NULL) &&
            //    (Data->Disk.VolumeHandle != INVALID_HANDLE_VALUE))
            //{
            //    CloseHandle(Data->Disk.VolumeHandle);
            //}

            ///* Cleanup. */
            //DeleteItemTree(Data->ItemTree);

            //if (Data->Disk.MountPoint != NULL) free(Data->Disk.MountPoint);
            //if (Data->Disk.MountPointSlash != NULL) free(Data->Disk.MountPointSlash);

            #endregion

            Data.Disk.Close();
        }
/*
        / * Subfunction for DefragAllDisks(). It will ignore removable disks, and
        will iterate for disks that are mounted on a subdirectory of another
        disk (instead of being mounted on a drive). * /
        void JKDefragLib::DefragMountpoints(struct DefragDataStruct *Data, WCHAR *MountPoint, int Mode)
        {
	        WCHAR VolumeNameSlash[BUFSIZ];
	        WCHAR VolumeName[BUFSIZ];

	        int DriveType;

	        DWORD FileSystemFlags;

	        HANDLE FindMountpointHandle;

	        WCHAR RootPath[MAX_PATH + BUFSIZ];
	        WCHAR *FullRootPath;

	        HANDLE VolumeHandle;

	        int Result;

	        size_t Length;

	        DWORD ErrorCode;

	        WCHAR s1[BUFSIZ];
	        WCHAR *p1;

	        DWORD w;

        //	JKDefragGui *jkGui = JKDefragGui::getInstance();

	        if (*Data->Running != RUNNING) return;

	        / * Clear the screen and show message "Analyzing volume '%s'" * /
        //	jkGui->ClearScreen(Data->DebugMsg[37],MountPoint);

	        / * Return if this is not a fixed disk. * /
	        DriveType = GetDriveTypeW(MountPoint);

	        if (DriveType != DRIVE_FIXED)
	        {
		        if (DriveType == DRIVE_UNKNOWN)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because the drive type cannot be determined.",MountPoint);
		        }

		        if (DriveType == DRIVE_NO_ROOT_DIR)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because there is no volume mounted.",MountPoint);
		        }

		        if (DriveType == DRIVE_REMOVABLE)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because it has removable media.",MountPoint);
		        }

		        if (DriveType == DRIVE_REMOTE)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because it is a remote (network) drive.",MountPoint);
		        }

		        if (DriveType == DRIVE_CDROM)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because it is a CD-ROM drive.",MountPoint);
		        }

		        if (DriveType == DRIVE_RAMDISK)
		        {
        //			jkGui->ClearScreen(L"Ignoring volume '%s' because it is a RAM disk.",MountPoint);
		        }

		        return;
	        }

	        / * Determine the name of the volume, something like
	        "\\?\Volume{08439462-3004-11da-bbca-806d6172696f}\". * /
	        Result = GetVolumeNameForVolumeMountPointW(MountPoint,VolumeNameSlash,BUFSIZ);

	        if (Result == 0)
	        {
		        ErrorCode = GetLastError();

		        if (ErrorCode == 3)
		        {
			        / * "Ignoring volume '%s' because it is not a harddisk." * /
        //			jkGui->ShowDebug(0,NULL,Data->DebugMsg[57],MountPoint);
		        }
		        else
		        {
			        / * "Cannot find volume name for mountpoint: %s" * /
			        SystemErrorStr(ErrorCode,s1,BUFSIZ);

        //			jkGui->ShowDebug(0,NULL,Data->DebugMsg[40],MountPoint,s1);
		        }

		        return;
	        }

	        / * Return if the disk is read-only. * /
	        GetVolumeInformationW(VolumeNameSlash,NULL,0,NULL,NULL,&FileSystemFlags,NULL,0);

	        if ((FileSystemFlags & FILE_READ_ONLY_VOLUME) != 0)
	        {
		        / * Clear the screen and show message "Ignoring disk '%s' because it is read-only." * /
        //		jkGui->ClearScreen(Data->DebugMsg[36],MountPoint);

		        return;
	        }

	        / * If the volume is not mounted then leave. Unmounted volumes can be
	        defragmented, but the system administrator probably has unmounted
	        the volume because he wants it untouched. * /
	        wcscpy_s(VolumeName,BUFSIZ,VolumeNameSlash);

	        p1 = wcschr(VolumeName,0);

	        if (p1 != VolumeName)
	        {
		        p1--;
		        if (*p1 == '\\') *p1 = 0;
	        }

	        VolumeHandle = CreateFileW(VolumeName,GENERIC_READ,
		        FILE_SHARE_READ | FILE_SHARE_WRITE,NULL,OPEN_EXISTING,0,NULL);

	        if (VolumeHandle == INVALID_HANDLE_VALUE)
	        {
		        SystemErrorStr(GetLastError(),s1,BUFSIZ);

        // 		jkGui->ShowDebug(1,NULL,L"Cannot open volume '%s' at mountpoint '%s': %s",
        // 			VolumeName,MountPoint,s1);

		        return;
	        }

	        if (DeviceIoControl(VolumeHandle,FSCTL_IS_VOLUME_MOUNTED,NULL,0,NULL,0,&w,NULL) == 0)
	        {
		        / * Show debug message: "Volume '%s' at mountpoint '%s' is not mounted." * /
        //		jkGui->ShowDebug(0,NULL,Data->DebugMsg[31],VolumeName,MountPoint);

		        CloseHandle(VolumeHandle);

		        return;
	        }

	        CloseHandle(VolumeHandle);

	        / * Defrag the disk. * /
	        Length = wcslen(MountPoint) + 2;

	        p1 = (WCHAR *)malloc(sizeof(WCHAR) * Length);

	        if (p1 != NULL)
	        {
		        swprintf_s(p1,Length,L"%s*",MountPoint);

		        DefragOnePath(Data,p1,Mode);

		        free(p1);
	        }

	        / * According to Microsoft I should check here if the disk has support for
	        reparse points:
	        if ((FileSystemFlags & FILE_SUPPORTS_REPARSE_POINTS) == 0) return;
	        However, I have found this test will frequently cause a false return
	        on Windows 2000. So I've removed it, everything seems to be working
	        nicely without it. * /

	        / * Iterate for all the mountpoints on the disk. * /
	        FindMountpointHandle = FindFirstVolumeMountPointW(VolumeNameSlash,RootPath,MAX_PATH + BUFSIZ);

	        if (FindMountpointHandle == INVALID_HANDLE_VALUE) return;

	        do
	        {
		        Length = wcslen(MountPoint) + wcslen(RootPath) + 1;
		        FullRootPath = (WCHAR *)malloc(sizeof(WCHAR) * Length);

		        if (FullRootPath != NULL)
		        {
			        swprintf_s(FullRootPath,Length,L"%s%s",MountPoint,RootPath);

			        DefragMountpoints(Data,FullRootPath,Mode);

			        free(FullRootPath);
		        }
	        } while (FindNextVolumeMountPointW(FindMountpointHandle,RootPath,MAX_PATH + BUFSIZ) != 0);

	        FindVolumeMountPointClose(FindMountpointHandle);
        }

*/

        /* Run the defragger/optimizer */
        public void RunJkDefrag(String Path, UInt16 Mode, Int16 Speed,
            UInt16 FreeSpace, List<String> Excludes, List<String> SpaceHogs)
        {
            Data = new MSDefragDataStruct();

            #region old code

            //UInt64 DrivesSize;

            //String Drives;
            //String Drive;

            //int DefaultRunning;
	        //	int DefaultRedrawScreen;

            //Boolean NtfsDisableLastAccessUpdate;

            //LONG Result;

            //HKEY Key;

            //DWORD KeyDisposition;
            //DWORD Length;

            //String s1/*[BUFSIZ]*/;

            //int i;

            #endregion

            /* Copy the input values to the data struct. */
            Data.Speed = Speed;
            Data.FreeSpace = FreeSpace;
            Data.Excludes = Excludes;
            Data.Running = RunningState.RUNNING;
            Data.RedrawScreen = 0;

            #region SpaceHogs

            /* Make a copy of the SpaceHogs array. */
            List<String> spaceHogs = new List<String>();

            Data.UseDefaultSpaceHogs = true;

	        if (SpaceHogs != null && SpaceHogs.Count > 0)
	        {
		        foreach (String spaceHog in SpaceHogs)
		        {
                    if (spaceHog.CompareTo("DisableDefaults") == 0)
			        {
                        Data.UseDefaultSpaceHogs = false;
			        }
			        else
			        {
				        spaceHogs.Add(spaceHog);
			        }
		        }
	        }

            if (Data.UseDefaultSpaceHogs == true)
	        {
		        spaceHogs.Add("?:\\$RECYCLE.BIN\\*");      /* Vista */
		        spaceHogs.Add("?:\\RECYCLED\\*");          /* FAT on 2K/XP */
		        spaceHogs.Add("?:\\RECYCLER\\*");          /* NTFS on 2K/XP */
		        spaceHogs.Add("?:\\WINDOWS\\$*");
		        spaceHogs.Add("?:\\WINDOWS\\Downloaded Installations\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Ehome\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Fonts\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Help\\*");
		        spaceHogs.Add("?:\\WINDOWS\\I386\\*");
		        spaceHogs.Add("?:\\WINDOWS\\IME\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Installer\\*");
		        spaceHogs.Add("?:\\WINDOWS\\ServicePackFiles\\*");
		        spaceHogs.Add("?:\\WINDOWS\\SoftwareDistribution\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Speech\\*");
		        spaceHogs.Add("?:\\WINDOWS\\Symbols\\*");
		        spaceHogs.Add("?:\\WINDOWS\\ie7updates\\*");
		        spaceHogs.Add("?:\\WINDOWS\\system32\\dllcache\\*");
		        spaceHogs.Add("?:\\WINNT\\$*");
		        spaceHogs.Add("?:\\WINNT\\Downloaded Installations\\*");
		        spaceHogs.Add("?:\\WINNT\\I386\\*");
		        spaceHogs.Add("?:\\WINNT\\Installer\\*");
		        spaceHogs.Add("?:\\WINNT\\ServicePackFiles\\*");
		        spaceHogs.Add("?:\\WINNT\\SoftwareDistribution\\*");
		        spaceHogs.Add("?:\\WINNT\\ie7updates\\*");
		        spaceHogs.Add("?:\\*\\Installshield Installation Information\\*");
		        spaceHogs.Add("?:\\I386\\*");
		        spaceHogs.Add("?:\\System Volume Information\\*");
		        spaceHogs.Add("?:\\windows.old\\*");

		        spaceHogs.Add("*.7z");
		        spaceHogs.Add("*.arj");
		        spaceHogs.Add("*.avi");
		        spaceHogs.Add("*.bak");
		        spaceHogs.Add("*.bup");    /* DVD */
		        spaceHogs.Add("*.bz2");
		        spaceHogs.Add("*.cab");
		        spaceHogs.Add("*.chm");    /* Help files */
		        spaceHogs.Add("*.dvr-ms");
		        spaceHogs.Add("*.gz");
		        spaceHogs.Add("*.ifo");    /* DVD */
		        spaceHogs.Add("*.log");
		        spaceHogs.Add("*.lzh");
		        spaceHogs.Add("*.mp3");
		        spaceHogs.Add("*.msi");
		        spaceHogs.Add("*.old");
		        spaceHogs.Add("*.pdf");
		        spaceHogs.Add("*.rar");
		        spaceHogs.Add("*.rpm");
		        spaceHogs.Add("*.tar");
		        spaceHogs.Add("*.wmv");
		        spaceHogs.Add("*.vob");    /* DVD */
		        spaceHogs.Add("*.z");
		        spaceHogs.Add("*.zip");
            }

            /* If the NtfsDisableLastAccessUpdate setting in the registry is 1, then disable
             * the LastAccessTime check for the spacehogs. */
            Data.UseLastAccessTime = true;

            if (Data.UseDefaultSpaceHogs == true)
	        {
                RegistryKey regKey = Registry.LocalMachine;

                regKey = regKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\FileSystem");
                String[] valueNames = regKey.GetValueNames();

                Data.UseLastAccessTime = true;

                foreach (String valueName in valueNames)
                {
                    if (valueName.Equals("NtfsDisableLastAccessUpdate"))
                    {
                        Data.UseLastAccessTime = Convert.ToBoolean(regKey.GetValue(valueName));
                    }
                }

                regKey.Close();

                if (Data.UseLastAccessTime == true)
		        {
                    ShowDebug(1, "NtfsDisableLastAccessUpdate is inactive, using LastAccessTime for SpaceHogs.");
		        }
		        else
		        {
                    ShowDebug(1, "NtfsDisableLastAccessUpdate is active, ignoring LastAccessTime for SpaceHogs.");
		        }
            }
            #endregion


            StartTimer();

            /* If a Path is specified then call DefragOnePath() for that path. Otherwise call
             * DefragMountpoints() for every disk in the system. */
            if (!String.IsNullOrEmpty(Path))
            {
                DefragOnePath(Path, Mode);
            }
/*
            else
	        {
		        DrivesSize = GetLogicalDriveStringsW(0,NULL);

		        Drives = (WCHAR *)malloc(sizeof(WCHAR) * (DrivesSize + 1));

		        if (Drives != NULL)
		        {
			        DrivesSize = GetLogicalDriveStringsW(DrivesSize,Drives);

			        if (DrivesSize == 0)
			        {
				        / * "Could not get list of volumes: %s" * /
				        SystemErrorStr(GetLastError(),s1,BUFSIZ);

        //				jkGui->ShowDebug(1,NULL,Data.DebugMsg[39],s1);
			        }
			        else
			        {
				        Drive = Drives;

				        while (*Drive != '\0')
				        {
					        DefragMountpoints(&Data,Drive,Mode);
					        while (*Drive != '\0') Drive++;
					        Drive++;
				        }
			        }

			        free(Drives);
		        }

        //		jkGui->ClearScreen(Data.DebugMsg[38]);
	        }
*/
            Data.Running = RunningState.STOPPED;
        }

        #region StopJKDefrag

        /// <summary>
        /// Stop the defragger.
        /// Wait for a maximum of TimeOut milliseconds for the 
        /// defragger to stop. If TimeOut is zero then wait indefinitely.
        /// If TimeOut is negative then immediately return without waiting.
        /// </summary>
        public void StopJkDefrag(int TimeOut)
        {
	        /* Sanity check. */
	        if (Data.Running != RunningState.RUNNING) 
                return;

	        /* All loops in the library check if the Running variable is set to
	        RUNNING. If not then the loop will exit. In effect this will stop
	        the defragger. */
	        Data.Running = RunningState.STOPPING;

	        /* Wait for a maximum of TimeOut milliseconds for the defragger to stop.
	        If TimeOut is zero then wait indefinitely. If TimeOut is negative then
	        immediately return without waiting. */
	        int TimeWaited = 0;

	        while (TimeWaited <= TimeOut)
	        {
		        if (Data.Running == RunningState.STOPPED) break;

		        Thread.Sleep(100);

		        if (TimeOut > 0) TimeWaited = TimeWaited + 100;
	        }
        }

        #endregion

        System.Timers.Timer aTimer;

        public static MSDefragLib me;
        public static Int64 testNumber2 = 0;
        public static DateTime firstTimestamp;
        public static DateTime secondTimeStamp2;

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (firstTimestamp.Ticks == 0)
            {
                firstTimestamp = e.SignalTime;
            }

            secondTimeStamp2 = e.SignalTime;

            Double diffMilli = ((TimeSpan)(secondTimeStamp2 - firstTimestamp)).Ticks / 10000;

            testNumber2 += me._dirtySquares.Count;
            me.ShowDebug(0, "Total number of squares: " + testNumber2);
            me.ShowDebug(1, String.Format("Current Performance: {0:F} squares / s", (me._dirtySquares.Count * 1000 / me.aTimer.Interval)));

            if (diffMilli != 0)
            {
                me.ShowDebug(2, String.Format("Average Performance: {0:F} squares / s", (testNumber2 * 1000 / diffMilli)));
            }

            me.ShowChangedClusters();
        }

        private void StartTimer()
        {
            aTimer = new System.Timers.Timer(300);

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            aTimer.Enabled = true;
        }

        public void StartSimulation()
        {
            StartTimer();

            Data = new MSDefragDataStruct();

            Data.Running = RunningState.RUNNING;

            Simulate();

            //Thread defragThread = new Thread(Simulate);
            //defragThread.Priority = ThreadPriority.Lowest;

            //defragThread.Start();
        }

        private void Simulate()
        {
            me = this;

            Random rnd = new Random();

            for (int testNumber = 0; testNumber < 1000000; testNumber++)
            {
                Int32 squareBegin = rnd.Next(NumSquares);
                Int32 squareEnd = rnd.Next(squareBegin, squareBegin + 10);

                if (squareEnd > NumSquares)
                {
                    squareEnd = NumSquares;
                }

                if (Data.Running != RunningState.RUNNING)
                {
                    break;
                }

                CLUSTER_COLORS col = (CLUSTER_COLORS)rnd.Next((Int32)CLUSTER_COLORS.COLORMAX);

                for (Int32 squareNum = squareBegin; (Data.Running == RunningState.RUNNING) && (squareNum < squareEnd); squareNum++)
                {
                    ClusterSquare clusterSquare = new ClusterSquare(squareNum, 0, 20000);
                    clusterSquare.m_color = col;

                    lock (_dirtySquares)
                    {
                        _dirtySquares.Add(clusterSquare);

                        //if (_dirtySquares.Count() == MAX_DIRTY_SQUARES)
                        //{
                        //    ShowChangedClusters();
                        //}
                    }
                }

                if (testNumber % 100 == 0) ShowDebug(4, "Test: " + testNumber);

                Thread.Sleep(1);
            }

            Data.Running = RunningState.STOPPED;
        }

        #region EventHandling

        public void ScanNtfsEventHandler(object sender, EventArgs e)
        {
            if (ShowDebugEvent != null)
            {
                ShowDebugEvent(this, e);
            }
        }

        public delegate void ShowChangedClustersHandler(object sender, EventArgs e);
        public delegate void ShowDebugHandler(object sender, EventArgs e);

        //public delegate void DrawClusterHandler(object sender, EventArgs e);
        //public delegate void NotifyGuiHandler(object sender, EventArgs e);

        public event ShowChangedClustersHandler ShowChangedClustersEvent;
        //public event DrawClusterHandler DrawClusterEvent;
        //public event NotifyGuiHandler NotifyGuiEvent;
        public event ShowDebugHandler ShowDebugEvent;

        protected virtual void OnShowChangedClusters(EventArgs e)
        {
            if (ShowChangedClustersEvent != null)
            {
                ShowChangedClustersEvent(this, e);
            }
        }
        protected virtual void OnShowDebug(EventArgs e)
        {
            if (ShowDebugEvent != null)
            {
                ShowDebugEvent(this, e);
            }
        }
        //protected virtual void OnDrawCluster(EventArgs e)
        //{
        //    if (DrawClusterEvent != null)
        //    {
        //        if (e is DrawClusterEventArgs)
        //        {
        //            DrawClusterEvent(this, e);
        //        }
        //        if (e is DrawClusterEventArgs2)
        //        {
        //            DrawClusterEvent(this, e);
        //        }
        //    }
        //}
        //protected virtual void OnNotifyGui(EventArgs e)
        //{
        //    if (NotifyGuiEvent != null)
        //    {
        //        if (e is NotifyGuiEventArgs)
        //        {
        //            NotifyGuiEvent(this, e);
        //        }
        //    }
        //}

        public void ShowChangedClusters()
        {
            if (_dirtySquares.Count() >= MAX_DIRTY_SQUARES)
            {
                ChangedClusterEventArgs e = new ChangedClusterEventArgs(DirtySquares);

                OnShowChangedClusters(e);
            }
        }

        public void ShowDebug(UInt32 level, String output)
        {
            MSScanNtfsEventArgs e = new MSScanNtfsEventArgs(level, output);

            if (level < 6)
                OnShowDebug(e);
        }

        private const Int32 MAX_DIRTY_SQUARES = 300;

        private IList<ClusterSquare> _dirtySquares = new List<ClusterSquare>(MAX_DIRTY_SQUARES);

        private IList<ClusterSquare> DirtySquares
        {
            get
            {
                lock (_dirtySquares)
                {
                    IList<ClusterSquare> oldlist = _dirtySquares;
                    _dirtySquares = new List<ClusterSquare>(MAX_DIRTY_SQUARES);
                    return oldlist;
                }
            }
        }

        private void DrawCluster(UInt64 clusterBegin, UInt64 clusterEnd, CLUSTER_COLORS color)
        {
            //ShowDebug(3, "Cluster: " + clusterBegin);

            if ((clusterBegin < 0) || (clusterBegin > Data.TotalClusters) ||
                (clusterEnd < 0) || (clusterEnd > Data.TotalClusters))
            {
                return;
            }

            Double clusterPerSquare = (Double)Data.TotalClusters / (Double)(m_numSquares);

            if (m_clusterSquares.Count == 0)
            {
                ParseSquares();
            }

            Int32 squareBegin = (Int32)(clusterBegin / clusterPerSquare);
            Int32 squareEnd = (Int32)(clusterEnd / clusterPerSquare);

            if (squareEnd >= m_numSquares)
            {
                squareEnd = m_numSquares - 1;
            }

            for (Int32 ii = squareBegin; ii <= squareEnd; ii++)
            {
                ClusterSquare clusterSquare = m_clusterSquares[ii];
                UInt64 clusterBeginIndex = clusterSquare.m_clusterBeginIndex;
                UInt64 clusterEndIndex = clusterSquare.m_clusterEndIndex;

                for (UInt64 jj = clusterBeginIndex; jj < clusterEndIndex; jj++)
                {
                    if ((jj < clusterBegin) || (jj > clusterEnd))
                    {
                        continue;
                    }

                    Int32 oldColor = (Int32)m_clusterData[(Int32)jj];

                    m_clusterData[(Int32)jj] = color;

                    if (clusterSquare.m_colors[oldColor] > 0)
                    {
                        clusterSquare.m_colors[oldColor]--;
                    }

                    clusterSquare.m_colors[(Int32)color]++;
                }

                clusterSquare.SetMaxColor();

                if (clusterSquare.m_isDirty)
                {
                    clusterSquare.m_isDirty = false;
//                    ShowDebug(0, "Done: " + m_data.PhaseDone + " / " + m_data.PhaseTodo);
                    lock (_dirtySquares)
                    {
                        _dirtySquares.Add(clusterSquare);

                        if (_dirtySquares.Count() == MAX_DIRTY_SQUARES)
                        {
                        //    ShowDebug(4, "Notify: " + clusterSquare.m_squareIndex);
                            ShowChangedClusters();
                        }
                    }
                }
            }
        }
        //public void NotifyGui(ClusterSquare clusterSquare)
        //{

        //    NotifyGuiEventArgs e = new NotifyGuiEventArgs(clusterSquare);

        //    OnNotifyGui(e);
        //}

        private static Boolean ggg = false;

        /// <summary>
        /// Function returns list of all "dirty" squares that need to be updated.
        /// </summary>
        /// <param name="squareBegin">First square number</param>
        /// <param name="squareEnd">Last square number</param>
        /// <returns>List of dirty squares</returns>
        public List<ClusterSquare> GetSquareList()
        {
            if (m_clusterSquares.Count == 0)
            {
                return null;
            }

            if (ggg)
            {
                return null;
            }

            ggg = true;


            var squareList =
                from a in m_clusterSquares
                where a.m_isDirty == true
                select a;

            //List<ClusterSquare> list = new List<ClusterSquare>();

            //for (Int32 ii = 0; ii < m_clusterSquares.Count; ii++)
            //{
            //    ClusterSquare clusterSquare = m_clusterSquares[ii];

            //    if (clusterSquare.m_isDirty)
            //    {
            //        list.Add(clusterSquare);

            //        clusterSquare.m_isDirty = false;
            //    }
            //}

            ggg = false;

            return squareList.ToList();
        }

        /// <summary>
        /// This function parses whole cluster list and updates square information.
        /// </summary>
        private void ParseSquares()
        {
            Double clusterPerSquare = (Double)Data.TotalClusters / (Double)(m_numSquares);

            m_clusterSquares.Clear();

            for (Int32 squareIndex = 0; squareIndex < m_numSquares; squareIndex++)
            {
                UInt64 clusterIndex = (UInt64)(squareIndex * clusterPerSquare);
                UInt64 lastClusterIndex = clusterIndex + (UInt64)clusterPerSquare - 1;

                if (lastClusterIndex > (UInt64)m_clusterData.Count - 1)
                {
                    lastClusterIndex = (UInt64)m_clusterData.Count - 1;
                }

                ClusterSquare square = new ClusterSquare(squareIndex, clusterIndex, lastClusterIndex);

                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORALLOCATED] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORBACK] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORBUSY] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLOREMPTY] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORFRAGMENTED] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORMFT] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORSPACEHOG] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORUNFRAGMENTED] = 0;
                square.m_colors[(Int32)MSDefragLib.CLUSTER_COLORS.COLORUNMOVABLE] = 0;

                for (UInt64 jj = clusterIndex; jj <= lastClusterIndex; jj++)
                {
                    Int32 clusterColor = (Int32)m_clusterData[(Int32)jj];

                    square.m_colors[clusterColor]++;
                }

                square.SetMaxColor();

                m_clusterSquares.Add(square);
            }
        }

        #endregion

        #region Variables

        public MSDefragDataStruct Data
        { get; set; }

        List<MSDefragLib.CLUSTER_COLORS> m_clusterData = null;
        List<ClusterSquare> m_clusterSquares = null;

        private Int32 m_numSquares = 0;

        public Int32 NumSquares
        {
            set
            {
                m_numSquares = value;

                m_clusterSquares = new List<ClusterSquare>(m_numSquares);
            }

            get
            {
                return m_numSquares;
            }
        }

        #endregion
    }

    #region Event classes

    public class ChangedClusterEventArgs : EventArgs
    {
        public IList<ClusterSquare> m_list;

        public ChangedClusterEventArgs(IList<ClusterSquare> list)
        {
            m_list = list;
        }
    }

    //public class DrawClusterEventArgs : EventArgs 
    //{
    //    public UInt64 m_clusterNumber;
    //    public MSDefragLib.CLUSTER_COLORS m_color;
    //    public MSDefragDataStruct m_data;

    //    public DrawClusterEventArgs(MSDefragDataStruct data, UInt64 clusterNum, MSDefragLib.CLUSTER_COLORS col)
    //    {
    //        m_data = data;
    //        m_clusterNumber = clusterNum;
    //        m_color = col;
    //    }
    //}
    //public class DrawClusterEventArgs2 : EventArgs
    //{
    //    public UInt64 m_startClusterNumber;
    //    public UInt64 m_endClusterNumber;
    //    public MSDefragDataStruct m_data;

    //    public Int32 m_squareBegin;
    //    public Int32 m_squareEnd;

    //    public DrawClusterEventArgs2(MSDefragDataStruct data, UInt64 startClusterNum, UInt64 endClusterNum)
    //    {
    //        m_data = data;
    //        m_startClusterNumber = startClusterNum;
    //        m_endClusterNumber = endClusterNum;
    //    }

    //    public DrawClusterEventArgs2(Int32 squareBegin, Int32 squareEnd)
    //    {
    //        m_squareBegin = squareBegin;
    //        m_squareEnd = squareEnd;
    //    }
    //}
    //public class NotifyGuiEventArgs : EventArgs
    //{
    //    public ClusterSquare m_clusterSquare;

    //    public NotifyGuiEventArgs(ClusterSquare clusterSquare)
    //    {
    //        m_clusterSquare = clusterSquare;
    //    }
    //}

    #endregion

    #region ClusterStructure

    /// <summary>
    /// Structure for describing cluster
    /// </summary>
    public class ClusterStructure
    {
        public ClusterStructure(UInt64 clusterIndex, MSDefragLib.CLUSTER_COLORS color)
        {
            m_clusterIndex = clusterIndex;
            m_color = color;
        }

        public UInt64 m_clusterIndex;
        public MSDefragLib.CLUSTER_COLORS m_color;
    }

    #endregion
}
