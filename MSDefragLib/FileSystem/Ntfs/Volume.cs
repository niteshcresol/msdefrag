using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// In NTFS, everything on disk is a file. Even the metadata is stored as a set of files.
    /// The Master File Table (MFT) is an index of every file on the volume. For each file,
    /// the MFT keeps a set of records called attributes and each attribute stores a
    /// different type of information. 
    /// 
    /// Everything is a file in NTFS. The index to these files is the Master File Table (MFT). 
    /// The MFT lists the Boot Sector file ($Boot), located at the beginning of the disk.
    /// $Boot also lists where to find the MFT. The MFT also lists itself.
    /// 
    /// Located in the centre of the disk, we find some more Metadata files. The interesting
    /// ones are: $MFTMirr and $LogFile. The MFT Mirror is an exact copy of the first 4 records
    /// of the MFT. If the MFT is damaged, then the volume could be recovered by finding the
    /// mirror. The LogFile is journal of all the events waiting to be written to disk. If the
    /// machine crashes, then the LogFile is used to return the disk to a sensible state.
    /// 
    /// Hidden at the end of the volume, is a copy of the boot sector (cluster 0). The only
    /// Metadata file that makes reference to it is $Bitmap, and that only says that the
    /// cluster is in use.
    /// 
    ///  Below is a table of files found on a volume
    ///  
    ///  Inode 	Filename 	OS 	Description
    ///      0 	$MFT 	  	    Master File Table - An index of every file
    ///      1 	$MFTMirr 	  	A backup copy of the first 4 records of the MFT
    ///      2 	$LogFile 	  	Transactional logging file
    ///      3 	$Volume 	  	Serial number, creation time, dirty flag
    ///      4 	$AttrDef 	  	Attribute definitions
    ///      5 	. (dot) 	  	Root directory of the disk
    ///      6 	$Bitmap 	  	Contains volume's cluster map (in-use vs. free)
    ///      7 	$Boot 	  	    Boot record of the volume
    ///      8 	$BadClus 	  	Lists bad clusters on the volume
    ///      9 	$Quota 	    NT 	Quota information
    ///      9 	$Secure 	2K 	Security descriptors used by the volume
    ///     10 	$UpCase 	  	Table of uppercase characters used for collating
    ///     11 	$Extend 	2K 	A directory: $ObjId, $Quota, $Reparse, $UsnJrnl
    ///        	  	  	 
    ///  12-15 	<Unused> 	  	Marked as in use but empty
    ///  16-23 	<Unused> 	  	Marked as unused
    ///        	  	  	 
    ///    Any 	$ObjId  	2K 	Unique Ids given to every file
    ///    Any 	$Quota 	    2K 	Quota information
    ///    Any 	$Reparse 	2K 	Reparse point information
    ///    Any 	$UsnJrnl 	2K 	Journalling of Encryption
    ///        	  	  	 
    ///   > 24 	A_File 	  	    An ordinary file
    ///   > 24 	A_Dir 	  	    An ordinary directory
    ///   ... 	... 	  	    ...
    ///   
    /// For some reason $ObjId, $Quota, $Reparse and $UsnJrnl don't have inode 
    /// numbers below 24, like the rest of the Metadata files.
    ///  
    /// Also, the sequence number for each of the system files is always equal 
    /// to their mft record number and it is never modified. 
    /// </summary>
    public class Volume
    {
        public DiskInformation DiskInformation
        { get; set; }

        public MasterFileTable Mft
        { get; set; }

        public MasterFileTable MftMirror
        { get; set; }
    }
}
