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
using MSDefragLib.Defragmenter;


namespace MSDefragLib
{
    internal class MSDefragLib
    {
        //public class STARTING_LCN_INPUT_BUFFER
        //{
        //    public UInt64 StartingLcn;
        //};

        private Scan m_scanNtfs;

        private BaseDefragmenter defragmenter;

        public MSDefragLib(BaseDefragmenter parent)
        {
            defragmenter = parent;

            m_scanNtfs = new Scan(this);

            diskMap = new DiskMap();
            //diskMap = new DiskMap((Int32)Data.TotalClusters);
        }

        #region messages

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

        #endregion

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
        public void ShowHex(DefragmenterState Data, Array Buffer, UInt64 Count)
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

        ///* Subfunction of GetShortPath(). */
        //void AppendToShortPath(ItemStruct Item, ref String Path)
        //{
        //    if (Item.ParentDirectory != null) AppendToShortPath(Item.ParentDirectory, ref Path);

        //    Path += "\\";

        //    if (Item.ShortFilename != null)
        //    {
        //        Path += Item.ShortFilename;
        //    }
        //    else if (Item.LongFilename != null)
        //    {
        //        Path += Item.LongFilename;
        //    }
        //}

        ///*

        //Return a string with the full path of an item, constructed from the short names.
        //Return NULL if error. The caller must free() the new string.

        //*/
        //String GetShortPath(DefragmenterState Data, ItemStruct Item)
        //{
        //    Debug.Assert(Item != null);

        //    String Path = Data.Disk.MountPoint;

        //    /* Append all the strings. */
        //    AppendToShortPath(Item, ref Path);

        //    return(Path);
        //}

        ///* Subfunction of GetLongPath(). */
        //void AppendToLongPath(ItemStruct Item, ref String Path)
        //{
        //    if (Item.ParentDirectory != null)
        //        AppendToLongPath(Item.ParentDirectory, ref Path);

        //    Path += "\\";

        //    if (Item.LongFilename != null)
        //    {
        //        Path += Item.LongFilename;
        //    }
        //    else if (Item.ShortFilename != null)
        //    {
        //        Path += Item.ShortFilename;
        //    }
        //}

        ///*

        //Return a string with the full path of an item, constructed from the long names.
        //Return NULL if error. The caller must free() the new string.

        //*/
        //String GetLongPath(DefragmenterState Data, ItemStruct Item)
        //{
        //    Debug.Assert(Item != null);

        //    String Path = Data.Disk.MountPoint;

        //    /* Append all the strings. */
        //    AppendToLongPath(Item, ref Path);

        //    return(Path);
        //}

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
        //SortedList<UInt64, ItemStruct> itemList;
        public List<ItemStruct> itemList;

        /* Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number). */
        public void AddItemToList(ItemStruct newItem)
        {
            if (itemList == null)
            {
                itemList = new List<ItemStruct>();
            }

            lock (itemList)
            {
                itemList.Add(newItem);
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

	        eClusterState ClusterState;

            if (Item == null)
                return;

            // Determine if the item is fragmented.
            Boolean Fragmented = Item.IsFragmented(0, Item.NumClusters);

	        // Walk through all the fragments of the file.
            UInt64 RealVcn = 0;

	        foreach (Fragment fragment in Item.FragmentList)
	        {
		        // Ignore virtual fragments. They do not occupy space on disk and do not require colorization.
		        if (fragment.IsVirtual)
			        continue;

                UInt64 Vcn = fragment.Vcn;

		        SegmentBegin = 0;
                SegmentEnd = Math.Min(fragment.Length, Data.TotalClusters - RealVcn);

                // Walk through all the segments of the file. A segment is usually the same as a fragment,
                // but if a fragment spans across a boundary then we must determine the color of the left 
                // and right parts individually. So we pretend the fragment is divided into segments
                // at the various possible boundaries.

                while (SegmentBegin < SegmentEnd)
		        {
			        // Determine the color with which to draw this segment.

			        if (UnDraw == false)
			        {
                        ClusterState = eClusterState.Unfragmented;

                        if (Item.SpaceHog) ClusterState = eClusterState.SpaceHog;
                        if (Fragmented) ClusterState = eClusterState.Fragmented;
                        if (Item.Unmovable) ClusterState = eClusterState.Unmovable;
                        if (Item.Exclude) ClusterState = eClusterState.Unmovable;

				        if ((Vcn + SegmentBegin < BusyOffset) &&
					        (Vcn + SegmentEnd > BusyOffset))
				        {
					        SegmentEnd = BusyOffset - Vcn;
				        }

				        if ((Vcn + SegmentBegin >= BusyOffset) &&
					        (Vcn + SegmentBegin < BusyOffset + BusySize))
				        {
					        if (SegmentEnd > BusyOffset + BusySize - Vcn)
					        {
						        SegmentEnd = BusyOffset + BusySize - Vcn;
					        }

                            ClusterState = eClusterState.Busy;
				        }
			        }
			        else
			        {
                        ClusterState = eClusterState.Free;

				        for (int i = 0; i < 3; i++)
				        {
					        if ((fragment.Lcn + SegmentBegin < Data.MftExcludes[i].Start) &&
						        (fragment.Lcn + SegmentEnd > Data.MftExcludes[i].Start))
					        {
                                SegmentEnd = Data.MftExcludes[i].Start - fragment.Lcn;
					        }

                            if ((fragment.Lcn + SegmentBegin >= Data.MftExcludes[i].Start) &&
                                (fragment.Lcn + SegmentBegin < Data.MftExcludes[i].End))
					        {
                                if (fragment.Lcn + SegmentEnd > Data.MftExcludes[i].End)
						        {
                                    SegmentEnd = Data.MftExcludes[i].End - fragment.Lcn;
						        }

                                ClusterState = eClusterState.Mft;
					        }
				        }
			        }

			        // Colorize the segment.
                    //defragmenter.SetClusterState((Int32)(fragment.Lcn + SegmentBegin), (Int32)(fragment.Lcn + SegmentEnd), ClusterState);

                    defragmenter.SetClusterState(Item, ClusterState);

			        // Next segment
			        SegmentBegin = SegmentEnd;
		        }

		        // Next fragment
		        RealVcn += fragment.Length;
	        }
        }

        public void ParseDiskBitmap3()
        {
            int Index;
            int IndexMax;
            Boolean InUse = false;
            Boolean PrevInUse = false;

            IO.IOWrapper.BitmapData bitmapData = null;

            if ((Data == null) || (Data.Disk == null) || !Data.Disk.IsOpen)
            {
                return;
            }

            Data.Reparse = true;

            // Show the map of all the clusters in use

            UInt64 Lcn = 0;
            UInt64 ClusterStart = 0;

            PrevInUse = true;

            diskMap.totalClusters = (Int32)Data.TotalClusters;

            int ii = 0;

            do
            {
                if (!Data.Reparse)
                    break;

                if (Data.Running != RunningState.Running)
                    break;

                // Fetch a block of cluster data.

                bitmapData = Data.Disk.VolumeBitmap;

                // Sanity check

                if (Lcn >= bitmapData.StartingLcn + bitmapData.BitmapSize)
                {
                    throw new Exception("Sanity check failed!");
                    // break;
                }

                // Analyze the clusterdata. We resume where the previous block left off.

                Lcn = bitmapData.StartingLcn;
                Index = 0;

                IndexMax = bitmapData.Buffer.Length;

                while ((Index < IndexMax) && (Data.Running == RunningState.Running))
                {
                    InUse = bitmapData.Buffer[Index];

                    // If at the beginning of the disk then copy the InUse value as our starting value.
                    if (Lcn == 0)
                        PrevInUse = InUse;

                    // At the beginning and end of an Exclude draw the cluster
                    if ((Lcn == Data.MftExcludes[0].Start) || (Lcn == Data.MftExcludes[0].End) ||
                        (Lcn == Data.MftExcludes[1].Start) || (Lcn == Data.MftExcludes[1].End) ||
                        (Lcn == Data.MftExcludes[2].Start) || (Lcn == Data.MftExcludes[2].End))
                    {
                        eClusterState State = eClusterState.Allocated;

                        if ((Lcn == Data.MftExcludes[0].End) ||
                            (Lcn == Data.MftExcludes[1].End) ||
                            (Lcn == Data.MftExcludes[2].End))
                        {
                            State = eClusterState.Unmovable;
                        }
                        else if (PrevInUse == false)
                        {
                            State = eClusterState.Free;
                        }

                        defragmenter.SetClusterState((Int32)ClusterStart, (Int32)Lcn, State);

                        InUse = true;
                        PrevInUse = true;
                        ClusterStart = Lcn;
                    }

                    if ((PrevInUse == false) && (InUse != false))
                    {
                        // Free cluster

                        defragmenter.SetClusterState((Int32)ClusterStart, (Int32)Lcn, eClusterState.Free);
                        ClusterStart = Lcn;
                    }

                    if ((PrevInUse != false) && (InUse == false))
                    {
                        // Cluster in use

                        defragmenter.SetClusterState((Int32)ClusterStart, (Int32)Lcn, eClusterState.Allocated);
                        ClusterStart = Lcn;
                    }

                    PrevInUse = InUse;

                    Index++;
                    Lcn++;

                    if (ii % 100000 == 0)
                    {
                        ShowLogMessage(1, "Reparse bitmap: " + Index + " / " + IndexMax);
                        Thread.Sleep(50);
                    }

                    ii++;
                }

            } while (Lcn < bitmapData.StartingLcn + bitmapData.BitmapSize);

            if (Lcn > 0)
            {
                eClusterState clusterState = eClusterState.Free;

                if (PrevInUse == true)
                {
                    clusterState = eClusterState.Allocated;
                }

                defragmenter.SetClusterState((Int32)ClusterStart, (Int32)Lcn, clusterState);
            }

            // Show the MFT zones

            for (int i = 0; i < 3; i++)
            {
                if (Data.MftExcludes[i].Start <= 0)
                    continue;

                defragmenter.SetClusterState((Int32)Data.MftExcludes[i].Start, (Int32)Data.MftExcludes[i].End, eClusterState.Mft);
            }

            DisplayAllItems();

            Data.Reparse = false;
        }

        public void ParseDiskBitmap()
        {
            if ((Data == null) || (Data.Disk == null) || !Data.Disk.IsOpen)
            {
                return;
            }

            defragmenter.ResetClusterStates();

            Data.Reparse = true;

            diskMap.totalClusters = (Int32)Data.TotalClusters;

            Int32 numFilteredClusters = defragmenter.diskMap.NumFilteredClusters;
            Int32 totalClusters = (Int32)Data.TotalClusters;

            Double clusterPerFilter = (Double)totalClusters / (Double)numFilteredClusters;

            // Fetch a block of cluster data.

            IO.IOWrapper.BitmapData bitmapData = Data.Disk.VolumeBitmap;

            Double Index = 0;
            Double IndexMax = bitmapData.Buffer.Length;

            while ((Index < IndexMax) && (Data.Running == RunningState.Running))
            {
                Int32 currentCluster = (Int32)Index;
                Int32 nextCluster = Math.Min((Int32)(currentCluster + clusterPerFilter), (Int32)totalClusters - 1);

                eClusterState currentState = eClusterState.Free;

                Boolean Allocated = bitmapData.Buffer[currentCluster];

                if (Allocated == false)
                {
                    for (Int32 clusterNumber = currentCluster; clusterNumber <= nextCluster; clusterNumber++)
                    {
                        Allocated = bitmapData.Buffer[clusterNumber];

                        if (Allocated == true) break;
                    }
                }

                if (Allocated)
                {
                    currentState = eClusterState.Allocated;
                }

                defragmenter.SetClusterState(currentCluster, nextCluster - 1, currentState);

                Index += clusterPerFilter;
            }

            // Show the MFT zones

            for (int i = 0; i < 3; i++)
            {
                if (Data.MftExcludes[i].Start <= 0)
                    continue;

                defragmenter.SetClusterState((Int32)Data.MftExcludes[i].Start, (Int32)Data.MftExcludes[i].End, eClusterState.Mft);
            }

            DisplayAllItems();

            defragmenter.ResendAllClusters();

            Data.Reparse = false;
        }

        public void StartReparsingClusters()
        {
            if (Data == null) return;

            Data.Reparse = true;
        }

        public void StopReparsingClusters()
        {
            Data.Reparse = false;
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
        public void DisplayAllItems()
        {
            if (itemList == null)
                return;

            lock (itemList)
            {
                // Colorize all the files on the screen.
                // Note: the "$BadClus" file on NTFS disks maps the entire disk, so we have to ignore it.
                Int32 ii = 0;

                foreach (ItemStruct Item in itemList)
                {
                    if (Data.Running != RunningState.Running)
                        break;

                    //if (Data.RedrawScreen != 2) break;

                    if (Item == null)
                        continue;

                    if ((Item.LongFilename != null) &&
                        ((Item.LongFilename.CompareTo("$BadClus") == 0) ||
                        (Item.LongFilename.CompareTo("$BadClus:$Bad:$DATA") == 0)))
                    {
                        continue;
                    }

                    ColorizeItem(Item, 0, 0, false);

                    if (ii % 1000 == 0)
                    {
                        ShowLogMessage(1, "Display all items: " + ii + " / " + itemList.Count);
                    }

                    ii++;
                }

                ShowLogMessage(1, String.Empty);
            }
        }

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
            //ItemStruct Item;

            //UInt64[] SizeOfMovableFiles/*[3]*/ = new UInt64[3];
            //UInt64[] SizeOfUnmovableFragments/*[3]*/ = new UInt64[3];
            //UInt64[] ZoneEnd/*[3]*/ = new UInt64[3];
            //UInt64[] OldZoneEnd/*[3]*/ = new UInt64[3];
            //UInt64 RealVcn;

            //int Iterate;
            //int i;

            ///* Calculate the number of clusters in movable items for every zone. */
            //for (int Zone = 0; Zone <= 2; Zone++)
            //{
            //    SizeOfMovableFiles[Zone] = new UInt64();
            //    SizeOfMovableFiles[Zone] = 0;
            //}

            //for (Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
            //{
            //    if (Item.Unmovable == true) continue;
            //    if (Item.Exclude == true) continue;
            //    if ((Item.IsDirectory == true) && (Data.CannotMoveDirs > 20)) continue;

            //    int Zone = 1;
            //    if (Item.SpaceHog == true) Zone = 2;
            //    if (Item.IsDirectory == true) Zone = 0;

            //    SizeOfMovableFiles[Zone] = SizeOfMovableFiles[Zone] + Item.NumClusters;
            //}

            ///* Iterate until the calculation does not change anymore, max 10 times. */
            //for (int Zone = 0; Zone <= 2; Zone++)
            //    SizeOfUnmovableFragments[Zone] = 0;

            //for (int Zone = 0; Zone <= 2; Zone++) 
            //    OldZoneEnd[Zone] = 0;

            //for (Iterate = 1; Iterate <= 10; Iterate++)
            //{
            //    /* Calculate the end of the zones. */
            //    ZoneEnd[0] = SizeOfMovableFiles[0] + SizeOfUnmovableFragments[0] +
            //        (UInt64)(Data.TotalClusters * Data.FreeSpace / 100.0);

            //    ZoneEnd[1] = ZoneEnd[0] + SizeOfMovableFiles[1] + SizeOfUnmovableFragments[1] +
            //        (UInt64)(Data.TotalClusters * Data.FreeSpace / 100.0);

            //    ZoneEnd[2] = ZoneEnd[1] + SizeOfMovableFiles[2] + SizeOfUnmovableFragments[2];

            //    /* Exit if there was no change. */
            //    if ((OldZoneEnd[0] == ZoneEnd[0]) &&
            //        (OldZoneEnd[1] == ZoneEnd[1]) &&
            //        (OldZoneEnd[2] == ZoneEnd[2])) break;

            //    for (int Zone = 0; Zone <= 2; Zone++)
            //        OldZoneEnd[Zone] = ZoneEnd[Zone];

            //    /* Show debug info. */
            //    ShowLogMessage(4, String.Format("Zone calculation, iteration {0:G}: 0 - {0:G} - {0:G} - {0:G}",
            //        Iterate, ZoneEnd[0], ZoneEnd[1], ZoneEnd[2]));

            //    /* Reset the SizeOfUnmovableFragments array. We are going to (re)calculate these numbers
            //    based on the just calculates ZoneEnd's. */
            //    for (int Zone = 0; Zone <= 2; Zone++) 
            //        SizeOfUnmovableFragments[Zone] = 0;

            //    /* The MFT reserved areas are counted as unmovable data. */
            //    for (i = 0; i < 3; i++)
            //    {
            //        if (Data.MftExcludes[i].Start < ZoneEnd[0])
            //        {
            //            SizeOfUnmovableFragments[0] += Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
            //        }
            //        else if (Data.MftExcludes[i].Start < ZoneEnd[1])
            //        {
            //            SizeOfUnmovableFragments[1] += Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
            //        }
            //        else if (Data.MftExcludes[i].Start < ZoneEnd[2])
            //        {
            //            SizeOfUnmovableFragments[2] += Data.MftExcludes[i].End - Data.MftExcludes[i].Start;
            //        }
            //    }

            //    // Walk through all items and count the unmovable fragments. Ignore unmovable fragments
            //    // in the MFT zones, we have already counted the zones.

            //    for (Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
            //    {
            //        if ((Item.Unmovable == false) &&
            //            (Item.Exclude == false) &&
            //            ((Item.IsDirectory == false) || (Data.CannotMoveDirs <= 20))) continue;

            //        RealVcn = 0;

            //        foreach (Fragment fragment in Item.FragmentList)
            //        {
            //            if (fragment.IsLogical)
            //            {
            //                if (((fragment.Lcn < Data.MftExcludes[0].Start) || (fragment.Lcn >= Data.MftExcludes[0].End)) &&
            //                    ((fragment.Lcn < Data.MftExcludes[1].Start) || (fragment.Lcn >= Data.MftExcludes[1].End)) &&
            //                    ((fragment.Lcn < Data.MftExcludes[2].Start) || (fragment.Lcn >= Data.MftExcludes[2].End)))
            //                {
            //                    if (fragment.Lcn < ZoneEnd[0])
            //                    {
            //                        SizeOfUnmovableFragments[0] += fragment.Length;
            //                    }
            //                    else if (fragment.Lcn < ZoneEnd[1])
            //                    {
            //                        SizeOfUnmovableFragments[1] += fragment.Length;
            //                    }
            //                    else if (fragment.Lcn < ZoneEnd[2])
            //                    {
            //                        SizeOfUnmovableFragments[2] += fragment.Length;
            //                    }
            //                }

            //                RealVcn += fragment.Length;
            //            }
            //        }
            //    }
            //}

            //// Calculated the begin of the zones.
            //Data.Zones[0] = 0;

            //for (i = 1; i <= 3; i++) Data.Zones[i] = ZoneEnd[i - 1];
        }

        /* Update some numbers in the DefragData. */
        void CallShowStatus(int Phase, int Zone)
        {
        //    STARTING_LCN_INPUT_BUFFER BitmapParam = new STARTING_LCN_INPUT_BUFFER();

        //    int Index;
        //    int IndexMax;

        //    Int64 Count;
        //    Int64 Factor;
        //    Int64 Sum;

        //    /* Count the number of free gaps on the disk. */
        //    Data.CountGaps = 0;
        //    Data.CountFreeClusters = 0;
        //    Data.BiggestGap = 0;
        //    Data.CountGapsLess16 = 0;
        //    Data.CountClustersLess16 = 0;

        //    UInt64 Lcn = 0;
        //    UInt64 ClusterStart = 0;
        //    Boolean PrevInUse = true;

        //    IO.IOWrapper.BitmapData bitmapData = null;

        //    do
        //    {
        //        /* Fetch a block of cluster data. */
        //        BitmapParam.StartingLcn = Lcn;

        //        bitmapData = Data.Disk.VolumeBitmap;

        //        if (bitmapData.Buffer == null)
        //        {
        //            break;
        //        }

        //        Lcn = bitmapData.StartingLcn;

        //        Index = 0;

        //        IndexMax = bitmapData.Buffer.Count;

        //        Boolean InUse = false;
        //        while (Index < IndexMax)
        //        {
        //            InUse = bitmapData.Buffer[Index];

        //            if (((Lcn >= Data.MftExcludes[0].Start) && (Lcn < Data.MftExcludes[0].End)) ||
        //                ((Lcn >= Data.MftExcludes[1].Start) && (Lcn < Data.MftExcludes[1].End)) ||
        //                ((Lcn >= Data.MftExcludes[2].Start) && (Lcn < Data.MftExcludes[2].End)))
        //            {
        //                InUse = true;
        //            }

        //            if ((PrevInUse == false) && (InUse != false))
        //            {
        //                Data.CountGaps ++;
        //                Data.CountFreeClusters += Lcn - ClusterStart;

        //                if (Data.BiggestGap < Lcn - ClusterStart) Data.BiggestGap = Lcn - ClusterStart;

        //                if (Lcn - ClusterStart < 16)
        //                {
        //                    Data.CountGapsLess16++;
        //                    Data.CountClustersLess16 += Lcn - ClusterStart;
        //                }
        //            }

        //            if ((PrevInUse != false) && (InUse == false)) ClusterStart = Lcn;

        //            PrevInUse = InUse;

        //            Index++;
        //            Lcn++;
        //        }

        //    } while ((bitmapData.Buffer != null) && (Lcn < bitmapData.StartingLcn + bitmapData.BitmapSize));

        //    if (PrevInUse == false)
        //    {
        //        Data.CountGaps ++;
        //        Data.CountFreeClusters += Lcn - ClusterStart;

        //        if (Data.BiggestGap < Lcn - ClusterStart) Data.BiggestGap = Lcn - ClusterStart;

        //        if (Lcn - ClusterStart < 16)
        //        {
        //            Data.CountGapsLess16 ++;
        //            Data.CountClustersLess16 += Lcn - ClusterStart;
        //        }
        //    }

        //    /* Walk through all files and update the counters. */
        //    Data.CountDirectories = 0;
        //    Data.CountAllFiles = 0;
        //    Data.CountFragmentedItems = 0;
        //    Data.CountAllBytes = 0;
        //    Data.CountFragmentedBytes = 0;
        //    Data.CountAllClusters = 0;
        //    Data.CountFragmentedClusters = 0;

        //    //for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
        //    //{
        //    //    if ((Item.LongFilename != null) &&
        //    //        (Item.LongFilename.Equals("$BadClus") ||
        //    //         Item.LongFilename.Equals("$BadClus:$Bad:$DATA")))
        //    //    {
        //    //        continue;
        //    //    }

        //    //    Data.CountAllBytes += Item.Bytes;
        //    //    Data.CountAllClusters += Item.NumClusters;

        //    //    if (Item.IsDirectory == true)
        //    //    {
        //    //        Data.CountDirectories++;
        //    //    }
        //    //    else
        //    //    {
        //    //        Data.CountAllFiles++;
        //    //    }

        //    //    if (Item.FragmentCount > 1)
        //    //    {
        //    //        Data.CountFragmentedItems++;
        //    //        Data.CountFragmentedBytes += Item.Bytes;
        //    //        Data.CountFragmentedClusters += Item.NumClusters;
        //    //    }
        //    //}

        //    /* Calculate the average distance between the end of any file to the begin of
        //    any other file. After reading a file the harddisk heads will have to move to
        //    the beginning of another file. The number is a measure of how fast files can
        //    be accessed.

        //    For example these 3 files:
        //    File 1 begin = 107
        //    File 1 end = 312
        //    File 2 begin = 595
        //    File 2 end = 645
        //    File 3 begin = 917
        //    File 3 end = 923

        //    File 1 end - File 2 begin = 283
        //    File 1 end - File 3 begin = 605
        //    File 2 end - File 1 begin = 538
        //    File 2 end - File 3 begin = 272
        //    File 3 end - File 1 begin = 816
        //    File 3 end - File 2 begin = 328
        //    --> Average distance from end to begin = 473.6666

        //    The formula used is:
        //    N = number of files
        //    Bn = Begin of file n
        //    En = End of file n
        //    Average = ( (1-N)*(B1+E1) + (3-N)*(B2+E2) + (5-N)*(B3+E3) + .... + (2*N-1-N)*(BN+EN) ) / ( N * (N-1) )

        //    For the above example:
        //    Average = ( (1-3)*(107+312) + (3-3)*(595+645) + 5-3)*(917+923) ) / ( 3 * (3-1) ) = 473.6666

        //    */
        //    Count = 0;

        //    for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
        //    {
        //        if ((Item.LongFilename == "$BadClus") ||
        //            (Item.LongFilename == "$BadClus:$Bad:$DATA"))
        //        {
        //            continue;
        //        }

        //        if (Item.NumClusters == 0) continue;

        //        Count++;
        //    }

        //    if (Count > 1)
        //    {
        //        Factor = 1 - Count;
        //        Sum = 0;

        //        for (ItemStruct Item = ItemTree.TreeSmallest(Data.ItemTree); Item != null; Item = ItemTree.TreeNext(Item))
        //        {
        //            if ((Item.LongFilename == "$BadClus") ||
        //                (Item.LongFilename == "$BadClus:$Bad:$DATA"))
        //            {
        //                continue;
        //            }

        //            if (Item.NumClusters == 0) continue;

        //            Sum += Factor * (Int64)((Item.Lcn * 2 + Item.NumClusters));

        //            Factor = Factor + 2;
        //        }

        //        Data.AverageDistance = Sum / (double)(Count * (Count - 1));
        //    }
        //    else
        //    {
        //        Data.AverageDistance = 0;
        //    }

        //    Data.Phase = (UInt16)Phase;
        //    Data.Zone = (UInt16)Zone;
        //    Data.PhaseDone = 0;
        //    Data.PhaseTodo = 0;

        ////	jkGui->ShowStatus(Data);
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

	        // Fetch the current time

            DateTime Time1 = DateTime.Now;
            Int64 Time2 = Time1.ToFileTime();
            Int64 Time3 = Time2;
            Int64 SystemTime = Time3;

	        // Scan NTFS disks
            Result = m_scanNtfs.AnalyzeNtfsVolume();

	        /* Scan FAT disks. */
            //if ((Result == false) && (*Data->Running == RUNNING)) Result = jkScanFat->AnalyzeFatVolume(Data);


	        /* Scan all other filesystems. */
            //if ((Result == FALSE) && (*Data->Running == RUNNING))
            //{
            //    jkGui->ShowDebug(0,NULL,L"This is not a FAT or NTFS disk, using the slow scanner.");

            //    /* Setup the width of the progress bar. */
            //    Data->PhaseTodo = Data->TotalClusters - Data->CountFreeClusters;

            //    for (i = 0; i < 3; i++)
            //    {
            //        Data->PhaseTodo = Data->PhaseTodo - (Data->MftExcludes[i].End - Data->MftExcludes[i].Start);
            //    }

            //    /* Scan all the files. */
            //    ScanDir(Data,Data->IncludeMask,NULL);
            //}

	        // Update the diskmap with the CLUSTER_COLORS
            Data.PhaseDone = Data.PhaseTodo;
            //DisplayAllItems();

            //defragmenter.SetClusterState(0, (Int32)Data.TotalClusters, eClusterState.Free);
            if (Data.Running != RunningState.Running || itemList == null)
                return;

	        // Setup the progress counter and the file/dir counters
            Data.PhaseDone = 0;
            Data.PhaseTodo = (UInt64)itemList.Count;

	        // Walk through all the items one by one

            foreach (ItemStruct Item in itemList)
            {
                if (Data.Running != RunningState.Running) break;

                ColorizeItem(Item, 0, 0, false);

                // Construct the full path's of the item. The MFT contains only the filename, plus
                // a pointer to the directory. We have to construct the full paths's by joining
                // all the names of the directories, and the name of the file.

                //if (Item.LongPath == null) Item.LongPath = GetLongPath(Data, Item);
                //if (Item.ShortPath == null) Item.ShortPath = GetShortPath(Data, Item);

                if (Item.LongPath == null)
                    Item.LongPath = Item.GetCompletePath(Data.Disk.MountPoint, false);
                if (Item.ShortPath == null)
                    Item.ShortPath = Item.GetCompletePath(Data.Disk.MountPoint, true);

                // Save some memory if the short and long paths are the same

                if (String.Equals(Item.LongPath, Item.ShortPath))
                {
                    Item.ShortPath = Item.LongPath;
                }

                if ((Item.LongPath == null) && (Item.ShortPath != null)) Item.LongPath = Item.ShortPath;
                if ((Item.LongPath != null) && (Item.ShortPath == null)) Item.ShortPath = Item.LongPath;

                // For debugging only: compare the data with the output from the
                // FSCTL_GET_RETRIEVAL_POINTERS function call.
                //
                // CompareItems(Data,Item);
                //

                // Apply the Mask and set the Exclude flag of all items that do not match.

                if ((Wildcard.MatchMask(Item.LongPath, Data.IncludeMask) == false) &&
                    (Wildcard.MatchMask(Item.ShortPath, Data.IncludeMask) == false))
                {
                    Item.Exclude = true;

                    ColorizeItem(Item,0,0,false);
                }

                // Determine if the item is to be excluded by comparing it's name with the Exclude masks.

                if ((Item.Exclude == false) && (Data.Excludes != null))
                {
                    for (i = 0; Data.Excludes[i] != null; i++)
                    {
                        if ((Wildcard.MatchMask(Item.LongPath, Data.Excludes[i]) == true) ||
                            (Wildcard.MatchMask(Item.ShortPath, Data.Excludes[i]) == true))
                        {
                            Item.Exclude = true;

                            ColorizeItem(Item,0,0,false);

                            break;
                        }
                    }
                }

                // Exclude my own logfile

                if ((Item.Exclude == false) &&
                    (Item.LongFilename != null) &&
                    ((Item.LongFilename == "jkdefrag.log") ||
                    (Item.LongFilename == "jkdefragcmd.log") ||
                    (Item.LongFilename == "jkdefragscreensaver.log")))
                {
                    Item.Exclude = true;
                    ColorizeItem(Item,0,0,false);
                }

                // The item is a SpaceHog if it's larger than 50 megabytes, or last access time is more 
                // than 30 days ago, or if it's filename matches a SpaceHog mask

                if ((Item.Exclude == false) && (Item.IsDirectory == false))
                {
                    if ((Data.UseDefaultSpaceHogs == true) && (Item.Bytes > 50 * 1024 * 1024))
                    {
                        Item.SpaceHog = true;
                    }
                    else if ((Data.UseDefaultSpaceHogs == true) && (Data.UseLastAccessTime == true) &&
                            ((Int64)(Item.LastAccessTime + (Int64)(30 * 24 * 60 * 60) * 10000000) < SystemTime))
                    {
                        Item.SpaceHog = true;
                    }
                    else if (Data.SpaceHogs != null)
                    {
                        for (i = 0; i < Data.SpaceHogs.Count; i++)
                        {
                            if ((Wildcard.MatchMask(Item.LongPath, Data.SpaceHogs[i]) == true) ||
                                (Wildcard.MatchMask(Item.ShortPath, Data.SpaceHogs[i]) == true))
                            {
                                Item.SpaceHog = true;

                                break;
                            }
                        }
                    }

                    if (Item.SpaceHog == true) ColorizeItem(Item,0,0,false);
                }

                // Special exception for "http://www.safeboot.com/". 
                if (Wildcard.MatchMask(Item.LongPath, "*\\safeboot.fs") == true)
                    Item.Unmovable = true;

                // Special exception for Acronis OS Selector.
                if (Wildcard.MatchMask(Item.LongPath, "?:\\bootwiz.sys") == true)
                    Item.Unmovable = true;

                if (Wildcard.MatchMask(Item.LongPath, "*\\BOOTWIZ\\*") == true)
                    Item.Unmovable = true;

                // Special exception for DriveCrypt by "http://www.securstar.com/".
                if (Wildcard.MatchMask(Item.LongPath, "?:\\BootAuth?.sys") == true)
                    Item.Unmovable = true;

                // Special exception for Symantec GoBack.
                if (Wildcard.MatchMask(Item.LongPath, "*\\Gobackio.bin") == true)
                    Item.Unmovable = true;

                // The $BadClus file maps the entire disk and is always unmovable.
                if ((Item.LongFilename == "$BadClus") ||
                    (Item.LongFilename == "$BadClus:$Bad:$DATA"))
                {
                    Item.Unmovable = true;
                }

                if (Item.Unmovable == true)
                    ColorizeItem(Item, 0, 0, false);

                // Update the progress percentage.
                Data.PhaseDone++;

                if (Data.PhaseDone % 100 == 0)
                {
                    ShowProgress((Double)(Data.PhaseDone), (Double)Data.PhaseTodo);
                    // ShowLogMessage(1, "Phase: " + Data.PhaseDone + " / " + Data.PhaseTodo);
                }
            }

	        // Force the percentage to 100%.
            Data.PhaseDone = Data.PhaseTodo;

            defragmenter.SetClusterState(0, 0, 0);
            //ShowDiskmap();

	        // Calculate the begin of the zone's.
	        CalculateZones();

	        // Call the ShowAnalyze() callback one last time.
            // jkGui->ShowAnalyze(Data,NULL);
        }

        /// <summary>
        /// Run the defragmenter. Input is the name of a disk, mountpoint, directory, or file,
        /// and may contain wildcards '*' and '?'.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Mode"></param>
        void DefragOnePath(String Path, UInt16 Mode)
        {
            #region Excludes

            // Compare the item with the Exclude masks.
            // If a mask matches then return, ignore the item.

            if (Data.Excludes != null)
	        {
                String matchedExclude = null;

                foreach (String exclude in Data.Excludes)
                {
                    if (Wildcard.MatchMask(Path, exclude) == true) return;

                    if ((exclude.Equals("*")) && (exclude.Length <= 3) &&
                        (String.Compare(exclude[0].ToString(), Path[0].ToString(), true) == 0))
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

	        // Try to change our permissions so we can access special files and directories
            // such as "C:\System Volume Information". If this does not succeed then quietly
            // continue, we'll just have to do with whatever permissions we have.
            // SE_BACKUP_NAME = Backup and Restore Privileges.

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

            // Fixup the input mask.
            //  - If the length is 2 or 3 characters then rewrite into "c:\*".
            //  - If it does not contain a wildcard then append '*'.

            Data.IncludeMask = Path;

            if ((Path.Length == 2) || (Path.Length == 3))
            {
                Data.IncludeMask = Path.ToLower()[0] + ":\\*";
            }
            else if (Path.IndexOf('*') < 0)
            {
                Data.IncludeMask = Path + "*";
            }

            ShowLogMessage(0, "Input mask: " + Data.IncludeMask);

            /* Defragment and optimize. */
            ParseDiskBitmap();

            if (Data.Running == RunningState.Running)
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
        public void RunJkDefrag(String Path, UInt16 Mode, UInt16 FreeSpace, List<String> Excludes, List<String> SpaceHogs)
        {
            Data = new DefragmenterState(FreeSpace, Excludes, SpaceHogs);

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

            #region SpaceHogs

            // Make a copy of the SpaceHogs array.

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
		        spaceHogs.Add("?:\\$RECYCLE.BIN\\*");                           // Vista
		        spaceHogs.Add("?:\\RECYCLED\\*");                               // FAT on 2K/XP
		        spaceHogs.Add("?:\\RECYCLER\\*");                               // NTFS on 2K/XP
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
		        spaceHogs.Add("*.bup");                                         // DVD
		        spaceHogs.Add("*.bz2");
		        spaceHogs.Add("*.cab");
		        spaceHogs.Add("*.chm");                                         // Help files
		        spaceHogs.Add("*.dvr-ms");
		        spaceHogs.Add("*.gz");
		        spaceHogs.Add("*.ifo");                                         // DVD
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
		        spaceHogs.Add("*.vob");                                         // DVD
		        spaceHogs.Add("*.z");
		        spaceHogs.Add("*.zip");
            }

            // If the NtfsDisableLastAccessUpdate setting in the registry is 1, then disable
            // the LastAccessTime check for the spacehogs.

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
                    ShowLogMessage(1, "NtfsDisableLastAccessUpdate is inactive, using LastAccessTime for SpaceHogs.");
		        }
		        else
		        {
                    ShowLogMessage(1, "NtfsDisableLastAccessUpdate is active, ignoring LastAccessTime for SpaceHogs.");
		        }
            }

            Data.SpaceHogs = spaceHogs;

            #endregion

            // If a Path is specified then call DefragOnePath() for that path.
            // Otherwise call DefragMountpoints() for every disk in the system.

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
	        }
*/
            Data.Running = RunningState.Stopped;
        }

        #region StopJKDefrag

        /// <summary>
        /// This function stops the defragger.
        /// 
        /// Wait for a maximum of TimeOut milliseconds for the defragger to stop.
        /// If TimeOut is zero then wait indefinitely.
        /// If TimeOut is negative then immediately return without waiting.
        /// </summary>
        public void StopJkDefrag(int TimeOut)
        {
	        // Sanity check.

            if (Data.Running != RunningState.Running) 
                return;

	        // All loops in the library check if the Running variable is set to RUNNING.
            // If not then the loop will exit. In effect this will stop the defragger.

            Data.Running = RunningState.Stopping;

	        // Wait for a maximum of TimeOut milliseconds for the defragger to stop.
	        // If TimeOut is zero then wait indefinitely.
            // If TimeOut is negative then immediately return without waiting.

	        int TimeWaited = 0;

            while (TimeWaited <= TimeOut && (Data.Running != RunningState.Stopped))
	        {
		        Thread.Sleep(100);

		        if (TimeOut > 0) TimeWaited += 100;
	        }
        }

        #endregion

        #region EventHandling

        public void ShowLogMessage(Int16 level, String message)
        {
            defragmenter.ShowLogMessage(level, message);
        }

        public void ShowProgress(Double progress, Double all)
        {
            defragmenter.ShowProgress(progress, all);
        }

        #endregion

        #region Variables

        public DefragmenterState Data
        { get; set; }

        public DiskMap diskMap;

        #endregion
    }
}
