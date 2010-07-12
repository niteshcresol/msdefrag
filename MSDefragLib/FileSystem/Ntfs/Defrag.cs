using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    class Defrag
    {
        // Defragment all the fragmented files
        void Defragment(DefragmenterState Data)
        {
        //    struct ItemStruct *Item;
        //    struct ItemStruct *NextItem;

        //    ULONG64 GapBegin;
        //    ULONG64 GapEnd;
        //    ULONG64 ClustersDone;
        //    ULONG64 Clusters;

        //    struct FragmentListStruct *Fragment;

        //    ULONG64 Vcn;
        //    ULONG64 RealVcn;

        //    HANDLE FileHandle;

        //    int FileZone;
        //    int Result;

        ////	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //    CallShowStatus(Data,2,-1);               / * "Phase 2: Defragment" * /

        //    / * Setup the width of the progress bar: the number of clusters in all
        //    fragmented files. * /
        //    for (Item = TreeSmallest(Data->ItemTree); Item != NULL; Item = TreeNext(Item))
        //    {
        //        if (Item->Unmovable == YES) continue;
        //        if (Item->Exclude == YES) continue;
        //        if (Item->Clusters == 0) continue;

        //        if (IsFragmented(Item,0,Item->Clusters) == NO) continue;

        //        Data->PhaseTodo = Data->PhaseTodo + Item->Clusters;
        //    }

        //    / * Exit if nothing to do. * /
        //    if (Data->PhaseTodo == 0) return;

        //    / * Walk through all files and defrag. * /
        //    NextItem = TreeSmallest(Data->ItemTree);

        //    while ((NextItem != NULL) && (*Data->Running == RUNNING))
        //    {
        //        / * The loop may change the position of the item in the tree, so we have
        //        to determine and remember the next item now. * /
        //        Item = NextItem;

        //        NextItem = TreeNext(Item);

        //        / * Ignore if the Item cannot be moved, or is Excluded, or is not fragmented. * /
        //        if (Item->Unmovable == YES) continue;
        //        if (Item->Exclude == YES) continue;
        //        if (Item->Clusters == 0) continue;

        //        if (IsFragmented(Item,0,Item->Clusters) == NO) continue;

        //        / * Find a gap that is large enough to hold the item, or the largest gap
        //        on the volume. If the disk is full then show a message and exit. * /
        //        FileZone = 1;

        //        if (Item->SpaceHog == YES) FileZone = 2;
        //        if (Item->Directory == YES) FileZone = 0;

        //        Result = FindGap(Data,Data->Zones[FileZone],0,Item->Clusters,NO,NO,&GapBegin,&GapEnd,FALSE);

        //        if (Result == NO)
        //        {
        //            / * Try finding a gap again, this time including the free area. * /
        //            Result = FindGap(Data,0,0,Item->Clusters,NO,NO,&GapBegin,&GapEnd,FALSE);

        //            if (Result == NO)
        //            {
        //                / * Show debug message: "Disk is full, cannot defragment." * /
        //                return;
        //            }
        //        }

        //        / * If the gap is big enough to hold the entire item then move the file
        //        in a single go, and loop. * /
        //        if (GapEnd - GapBegin >= Item->Clusters)
        //        {
        //            MoveItem(Data,Item,GapBegin,0,Item->Clusters,0);

        //            continue;
        //        }

        //        / * Open a filehandle for the item. If error then set the Unmovable flag,
        //        colorize the item on the screen, and loop. * /
        //        FileHandle = OpenItemHandle(Data,Item);

        //        if (FileHandle == NULL)
        //        {
        //            Item->Unmovable = YES;

        //            ColorizeItem(Data,Item,0,0,NO);

        //            continue;
        //        }

        //        / * Move the file in parts, each time selecting the biggest gap
        //        available. * /
        //        ClustersDone = 0;

        //        do
        //        {
        //            Clusters = GapEnd - GapBegin;

        //            if (Clusters > Item->Clusters - ClustersDone)
        //            {
        //                Clusters = Item->Clusters - ClustersDone;
        //            }

        //            / * Make sure that the gap is bigger than the first fragment of the
        //            block that we're about to move. If not then the result would be
        //            more fragments, not less. * /
        //            Vcn = 0;
        //            RealVcn = 0;

        //            for (Fragment = Item->Fragments; Fragment != NULL; Fragment = Fragment->Next)
        //            {
        //                if (Fragment->Lcn != VIRTUALFRAGMENT)
        //                {
        //                    if (RealVcn >= ClustersDone)
        //                    {
        //                        if (Clusters > Fragment->NextVcn - Vcn) break;

        //                        ClustersDone = RealVcn + Fragment->NextVcn - Vcn;

        //                        Data->PhaseDone = Data->PhaseDone + Fragment->NextVcn - Vcn;
        //                    }

        //                    RealVcn = RealVcn + Fragment->NextVcn - Vcn;
        //                }

        //                Vcn = Fragment->NextVcn;
        //            }

        //            if (ClustersDone >= Item->Clusters) break;

        //            / * Move the segment. * /
        //            Result = MoveItem4(Data,Item,FileHandle,GapBegin,ClustersDone,Clusters,0);

        //            / * Next segment. * /
        //            ClustersDone = ClustersDone + Clusters;

        //            / * Find a gap large enough to hold the remainder, or the largest gap
        //            on the volume. * /
        //            if (ClustersDone < Item->Clusters)
        //            {
        //                Result = FindGap(Data,Data->Zones[FileZone],0,Item->Clusters - ClustersDone,
        //                    NO,NO,&GapBegin,&GapEnd,FALSE);

        //                if (Result == NO) break;
        //            }

        //        } while ((ClustersDone < Item->Clusters) && (*Data->Running == RUNNING));

        //        / * Close the item. * /
        //        FlushFileBuffers(FileHandle);            / * Is this useful? Can't hurt. * /
        //        CloseHandle(FileHandle);
        //    }
        }

        //

        //Look for a gap, a block of empty clusters on the volume.
        //MinimumLcn: Start scanning for gaps at this location. If there is a gap
        //at this location then return it. Zero is the begin of the disk.
        //MaximumLcn: Stop scanning for gaps at this location. Zero is the end of
        //the disk.
        //MinimumSize: The gap must have at least this many contiguous free clusters.
        //Zero will match any gap, so will return the first gap at or above
        //MinimumLcn.
        //MustFit: if YES then only return a gap that is bigger/equal than the
        //MinimumSize. If NO then return a gap bigger/equal than MinimumSize,
        //or if no such gap is found return the largest gap on the volume (above
        //MinimumLcn).
        //FindHighestGap: if NO then return the lowest gap that is bigger/equal
        //than the MinimumSize. If YES then return the highest gap.
        //Return YES if succes, NO if no gap was found or an error occurred.
        //The routine asks Windows for the cluster bitmap every time. It would be
        //faster to cache the bitmap in memory, but that would cause more fails
        //because of stale information.
        Boolean FindGap(DefragmenterState Data,
						         UInt64 MinimumLcn,          /* Gap must be at or above this LCN. */
                                 UInt64 MaximumLcn,          /* Gap must be below this LCN. */
                                 UInt64 MinimumSize,         /* Gap must be at least this big. */
						         UInt32 MustFit,                 /* YES: gap must be at least MinimumSize. */
                                 UInt32 FindHighestGap,          /* YES: return the last gap that fits. */
                                 ref UInt64 BeginLcn,           /* Result, LCN of begin of cluster. */
                                 ref UInt64 EndLcn,             /* Result, LCN of end of cluster. */
						         Boolean IgnoreMftExcludes)
        {
            return true;
        //    STARTING_LCN_INPUT_BUFFER BitmapParam;

        //    struct
        //    {
        //        ULONG64 StartingLcn;
        //        ULONG64 BitmapSize;

        //        BYTE Buffer[65536];               / * Most efficient if binary multiple. * /
        //    } BitmapData;

        //    ULONG64 Lcn;
        //    ULONG64 ClusterStart;
        //    ULONG64 HighestBeginLcn;
        //    ULONG64 HighestEndLcn;
        //    ULONG64 LargestBeginLcn;
        //    ULONG64 LargestEndLcn;

        //    int Index;
        //    int IndexMax;

        //    BYTE Mask;

        //    int InUse;
        //    int PrevInUse;

        //    DWORD ErrorCode;

        //    WCHAR s1[BUFSIZ];

        //    DWORD w;

        ////	JKDefragGui *jkGui = JKDefragGui::getInstance();

        //    / * Sanity check. * /
        //    if (MinimumLcn >= Data->TotalClusters) return(NO);

        //    / * Main loop to walk through the entire clustermap. * /
        //    Lcn = MinimumLcn;
        //    ClusterStart = 0;
        //    PrevInUse = 1;
        //    HighestBeginLcn = 0;
        //    HighestEndLcn = 0;
        //    LargestBeginLcn = 0;
        //    LargestEndLcn = 0;

        //    do
        //    {
        //        / * Fetch a block of cluster data. If error then return NO. * /
        //        BitmapParam.StartingLcn.QuadPart = Lcn;
        //        ErrorCode = DeviceIoControl(Data->Disk.VolumeHandle,FSCTL_GET_VOLUME_BITMAP,
        //            &BitmapParam,sizeof(BitmapParam),&BitmapData,sizeof(BitmapData),&w,NULL);

        //        if (ErrorCode != 0)
        //        {
        //            ErrorCode = NO_ERROR;
        //        }
        //        else
        //        {
        //            ErrorCode = GetLastError();
        //        }

        //        if ((ErrorCode != NO_ERROR) && (ErrorCode != ERROR_MORE_DATA))
        //        {
        //            / * Show debug message: "ERROR: could not get volume bitmap: %s" * /
        //            SystemErrorStr(GetLastError(),s1,BUFSIZ);

        ////			jkGui->ShowDebug(1,NULL,Data->DebugMsg[12],s1);

        //            return(NO);
        //        }

        //        / * Sanity check. * /
        //        if (Lcn >= BitmapData.StartingLcn + BitmapData.BitmapSize) return(NO);
        //        if (MaximumLcn == 0) MaximumLcn = BitmapData.StartingLcn + BitmapData.BitmapSize;

        //        / * Analyze the clusterdata. We resume where the previous block left
        //        off. If a cluster is found that matches the criteria then return
        //        it's LCN (Logical Cluster Number). * /
        //        Lcn = BitmapData.StartingLcn;
        //        Index = 0;
        //        Mask = 1;

        //        IndexMax = sizeof(BitmapData.Buffer);

        //        if (BitmapData.BitmapSize / 8 < IndexMax) IndexMax = (int)(BitmapData.BitmapSize / 8);

        //        while ((Index < IndexMax) && (Lcn < MaximumLcn))
        //        {
        //            if (Lcn >= MinimumLcn)
        //            {
        //                InUse = (BitmapData.Buffer[Index] & Mask);

        //                if (((Lcn >= Data->MftExcludes[0].Start) && (Lcn < Data->MftExcludes[0].End)) ||
        //                    ((Lcn >= Data->MftExcludes[1].Start) && (Lcn < Data->MftExcludes[1].End)) ||
        //                    ((Lcn >= Data->MftExcludes[2].Start) && (Lcn < Data->MftExcludes[2].End)))
        //                {
        //                    if (IgnoreMftExcludes == FALSE) InUse = 1;
        //                }

        //                if ((PrevInUse == 0) && (InUse != 0))
        //                {
        //                    / * Show debug message: "Gap found: LCN=%I64d, Size=%I64d" * /
        ////					jkGui->ShowDebug(6,NULL,Data->DebugMsg[13],ClusterStart,Lcn - ClusterStart);

        //                    / * If the gap is bigger/equal than the mimimum size then return it,
        //                    or remember it, depending on the FindHighestGap parameter. * /
        //                    if ((ClusterStart >= MinimumLcn) &&
        //                        (Lcn - ClusterStart >= MinimumSize))
        //                    {
        //                        if (FindHighestGap == NO)
        //                        {
        //                            if (BeginLcn != NULL) *BeginLcn = ClusterStart;

        //                            if (EndLcn != NULL) *EndLcn = Lcn;

        //                            return(YES);
        //                        }

        //                        HighestBeginLcn = ClusterStart;
        //                        HighestEndLcn = Lcn;
        //                    }

        //                    / * Remember the largest gap on the volume. * /
        //                    if ((LargestBeginLcn == 0) ||
        //                        (LargestEndLcn - LargestBeginLcn < Lcn - ClusterStart))
        //                    {
        //                        LargestBeginLcn = ClusterStart;
        //                        LargestEndLcn = Lcn;
        //                    }
        //                }

        //                if ((PrevInUse != 0) && (InUse == 0)) ClusterStart = Lcn;

        //                PrevInUse = InUse;
        //            }

        //            if (Mask == 128)
        //            {
        //                Mask = 1;
        //                Index = Index + 1;
        //            }
        //            else
        //            {
        //                Mask = Mask << 1;
        //            }

        //            Lcn = Lcn + 1;
        //        }
        //    } while ((ErrorCode == ERROR_MORE_DATA) &&
        //        (Lcn < BitmapData.StartingLcn + BitmapData.BitmapSize) &&
        //        (Lcn < MaximumLcn));

        //    / * Process the last gap. * /
        //    if (PrevInUse == 0)
        //    {
        //        / * Show debug message: "Gap found: LCN=%I64d, Size=%I64d" * /
        ////		jkGui->ShowDebug(6,NULL,Data->DebugMsg[13],ClusterStart,Lcn - ClusterStart);

        //        if ((ClusterStart >= MinimumLcn) && (Lcn - ClusterStart >= MinimumSize))
        //        {
        //            if (FindHighestGap == NO)
        //            {
        //                if (BeginLcn != NULL) *BeginLcn = ClusterStart;
        //                if (EndLcn != NULL) *EndLcn = Lcn;

        //                return(YES);
        //            }

        //            HighestBeginLcn = ClusterStart;
        //            HighestEndLcn = Lcn;
        //        }

        //        / * Remember the largest gap on the volume. * /
        //        if ((LargestBeginLcn == 0) ||
        //            (LargestEndLcn - LargestBeginLcn < Lcn - ClusterStart))
        //        {
        //            LargestBeginLcn = ClusterStart;
        //            LargestEndLcn = Lcn;
        //        }
        //    }

        //    / * If the FindHighestGap flag is YES then return the highest gap we have found. * /
        //    if ((FindHighestGap == YES) && (HighestBeginLcn != 0))
        //    {
        //        if (BeginLcn != NULL) *BeginLcn = HighestBeginLcn;
        //        if (EndLcn != NULL) *EndLcn = HighestEndLcn;

        //        return(YES);
        //    }

        //    / * If the MustFit flag is NO then return the largest gap we have found. * /
        //    if ((MustFit == NO) && (LargestBeginLcn != 0))
        //    {
        //        if (BeginLcn != NULL) *BeginLcn = LargestBeginLcn;
        //        if (EndLcn != NULL) *EndLcn = LargestEndLcn;

        //        return(YES);
        //    }

        //    / * No gap found, return NO. * /
        //    return(NO);
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

                //	jkGui->DrawCluster(Data,NewLcn,NewLcn + Size,JKDefragStruct::Busy);

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
                //	jkGui->DrawCluster(Data,NewLcn,NewLcn + Size,JKDefragStruct::Free);

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
                //					MoveParams.StartingLcn.QuadPart + MoveParams.ClusterCount,JKDefragStruct::Busy);

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
                //					MoveParams.StartingLcn.QuadPart + MoveParams.ClusterCount,JKDefragStruct::Free);

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

    }
}
