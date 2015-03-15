using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MSDefragLib.IO
{
    public class IOWrapper
    {
        #region CreateFile defines and Imports

        // CreateFile constants
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint FILE_SHARE_DELETE = 0x00000004;
        const uint OPEN_EXISTING = 3;

        const uint GENERIC_READ = (0x80000000);
        const uint GENERIC_WRITE = (0x40000000);

        const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        const uint FILE_READ_ATTRIBUTES = (0x0080);
        const uint FILE_WRITE_ATTRIBUTES = 0x0100;
        const uint ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            [Out] IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped);

        static public IntPtr OpenVolume(string DeviceName)
        {
            IntPtr hDevice;
            hDevice = CreateFile(
                @"\\.\" + DeviceName,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);
            if ((int)hDevice == -1)
            {
                throw new Exception(Marshal.GetLastWin32Error().ToString());
            }
            return hDevice;
        }

        static private IntPtr OpenFile(string path)
        {
            IntPtr hFile;
            hFile = CreateFile(path,
                        FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES,
                        FILE_SHARE_READ | FILE_SHARE_WRITE,
                        IntPtr.Zero,
                        OPEN_EXISTING,
                        0,
                        IntPtr.Zero);
            if ((int)hFile == -1)
            {
                throw new Exception(Marshal.GetLastWin32Error().ToString());
            }
            return hFile;
        }

        #endregion

        #region Wrapper1: GetVolumeMap

        public class BitmapData
        {
            public UInt64 StartingLcn;
            public UInt64 BitmapSize;
            public BitArray Buffer;
        } ;

        /// <summary>
        /// Get cluster usage for a device
        /// </summary>
        /// <param name="DeviceName">use "c:"</param>
        /// <returns>a bitarray for each cluster</returns>
        static public BitmapData GetVolumeMap(String DeviceName)
        {
            IntPtr hDevice = IntPtr.Zero;

            BitmapData retValue = new BitmapData();

            try
            {
                hDevice = OpenVolume(DeviceName);

                retValue = GetVolumeMap(hDevice);

            }
            finally
            {
                CloseHandle(hDevice);
                hDevice = IntPtr.Zero;
            }

            return retValue;
        }

        /// <summary>
        /// Get cluster usage for a device
        /// </summary>
        /// <param name="DeviceName">use "c:"</param>
        /// <returns>a bitarray for each cluster</returns>
        static public BitmapData GetVolumeMap(IntPtr hDevice)
        {
            IntPtr pAlloc = IntPtr.Zero;

            BitmapData retValue = new BitmapData();

            try
            {
                Int64 i64 = 0;

                GCHandle handle = GCHandle.Alloc(i64, GCHandleType.Pinned);
                IntPtr p = handle.AddrOfPinnedObject();

                uint q = 1024 * 1024 * 512 + 2 * 8;

                uint size = 0;
                pAlloc = Marshal.AllocHGlobal((int)q);
                IntPtr pDest = pAlloc;

                bool fResult = DeviceIoControl(
                    hDevice,
                    FSConstants.FSCTL_GET_VOLUME_BITMAP,
                    p,
                    (uint)Marshal.SizeOf(i64),
                    pDest,
                    q,
                    ref size,
                    IntPtr.Zero);

                if (!fResult)
                {
                    throw new Exception(Marshal.GetLastWin32Error().ToString());
                }
                handle.Free();

                retValue.StartingLcn = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.BitmapSize = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                Int32 byteSize = (int)(retValue.BitmapSize / 8);
                byteSize++; // round up - even with no remainder

                IntPtr BitmapBegin = (IntPtr)((Int64)pDest + 8);

                byte[] byteArr = new byte[byteSize];

                Marshal.Copy(BitmapBegin, byteArr, 0, (Int32)byteSize);

                retValue.Buffer = new BitArray(byteArr);
                retValue.Buffer.Length = (int)retValue.BitmapSize; // truncate to exact cluster count

                return retValue;
            }
            finally
            {
                Marshal.FreeHGlobal(pAlloc);
                pAlloc = IntPtr.Zero;
            }
        }

        public class NTFS_VOLUME_DATA_BUFFER
        {
            public UInt64 VolumeSerialNumber;
            public UInt64 NumberSectors;
            public UInt64 TotalClusters;
            public UInt64 FreeClusters;
            public UInt64 TotalReserved;
            public UInt32 BytesPerSector;
            public UInt32 BytesPerCluster;
            public UInt32 BytesPerFileRecordSegment;
            public UInt32 ClustersPerFileRecordSegment;
            public UInt64 MftValidDataLength;
            public UInt64 MftStartLcn;
            public UInt64 Mft2StartLcn;
            public UInt64 MftZoneStart;
            public UInt64 MftZoneEnd;
        };

        /// <summary>
        /// Get information about NTFS filesystem
        /// </summary>
        /// <param name="DeviceName">use "c:"</param>
        /// <returns>a bitarray for each cluster</returns>
        static public NTFS_VOLUME_DATA_BUFFER GetNtfsInfo(IntPtr hDevice)
        {
            IntPtr pAlloc = IntPtr.Zero;

            NTFS_VOLUME_DATA_BUFFER retValue = new NTFS_VOLUME_DATA_BUFFER();

            try
            {
                Int64 i64 = 0;

                GCHandle handle = GCHandle.Alloc(i64, GCHandleType.Pinned);
                IntPtr p = handle.AddrOfPinnedObject();

                uint q = 96;

                uint size = 0;
                pAlloc = Marshal.AllocHGlobal((int)q);
                IntPtr pDest = pAlloc;

                bool fResult = DeviceIoControl(
                    hDevice,
                    FSConstants.FSCTL_GET_NTFS_VOLUME_DATA,
                    p,
                    (uint)Marshal.SizeOf(i64),
                    pDest,
                    q,
                    ref size,
                    IntPtr.Zero);

                if (!fResult)
                {
                    throw new Exception(Marshal.GetLastWin32Error().ToString());
                }
                handle.Free();

                retValue.VolumeSerialNumber = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.NumberSectors = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.TotalClusters = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.FreeClusters = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.TotalReserved = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.BytesPerSector = (UInt32)Marshal.PtrToStructure(pDest, typeof(UInt32));

                pDest = (IntPtr)((Int64)pDest + 4);
                retValue.BytesPerCluster = (UInt32)Marshal.PtrToStructure(pDest, typeof(UInt32));

                pDest = (IntPtr)((Int64)pDest + 4);
                retValue.BytesPerFileRecordSegment = (UInt32)Marshal.PtrToStructure(pDest, typeof(UInt32));

                pDest = (IntPtr)((Int64)pDest + 4);
                retValue.ClustersPerFileRecordSegment = (UInt32)Marshal.PtrToStructure(pDest, typeof(UInt32));

                pDest = (IntPtr)((Int64)pDest + 4);
                retValue.MftValidDataLength = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.MftStartLcn = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.Mft2StartLcn = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.MftZoneStart = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                pDest = (IntPtr)((Int64)pDest + 8);
                retValue.MftZoneEnd = (UInt64)Marshal.PtrToStructure(pDest, typeof(UInt64));

                return retValue;
            }
            finally
            {
                Marshal.FreeHGlobal(pAlloc);
                pAlloc = IntPtr.Zero;
            }
        }

        #endregion

        #region Wrapper2: GetFileMap

        /// <summary>
        /// returns a 2*number of extents array -
        /// the vcn and the lcn as pairs
        /// </summary>
        /// <param name="path">file to get the map for ex: "c:\windows\explorer.exe" </param>
        /// <returns>An array of [virtual cluster, physical cluster]</returns>
        static public IList<String> GetFileMap(String path) // Fragment
        {
            IList<String> retVal = new List<String>();
            IntPtr hFile = IntPtr.Zero;
            IntPtr pAlloc = IntPtr.Zero;

            try
            {
                hFile = OpenFile(path);

                Int64 i64 = 0;

                GCHandle handle = GCHandle.Alloc(i64, GCHandleType.Pinned);
                IntPtr p = handle.AddrOfPinnedObject();

                uint q = 1024 * 1024 * 64; // 1024 bytes == 1k * 1024 == 1 meg * 64 == 64 megs

                uint size = 0;
                pAlloc = Marshal.AllocHGlobal((int)q);
                IntPtr pDest = pAlloc;
                bool fResult = DeviceIoControl(hFile,
                    FSConstants.FSCTL_GET_RETRIEVAL_POINTERS,
                    p,
                    (uint)Marshal.SizeOf(i64),
                    pDest,
                    q,
                    ref size,
                    IntPtr.Zero);

                if (fResult)
                {

                    handle.Free();

                    /*
                    returned back one of...
                     typedef struct RETRIEVAL_POINTERS_BUFFER { 
                        DWORD ExtentCount; 
                        LARGE_INTEGER StartingVcn; 
                        struct {
                            LARGE_INTEGER NextVcn;
                            LARGE_INTEGER Lcn;
                        } Extents[1];
                     } RETRIEVAL_POINTERS_BUFFER, *PRETRIEVAL_POINTERS_BUFFER;
                    */

                    Int32 ExtentCount = (Int32)Marshal.PtrToStructure(pDest, typeof(Int32));

                    pDest = (IntPtr)((Int64)pDest + 4);

                    Int64 StartingVcn = (Int64)Marshal.PtrToStructure(pDest, typeof(Int64));

//                    Debug.Assert(StartingVcn == 0);

                    pDest = (IntPtr)((Int64)pDest + 8);

                    // now pDest points at an array of pairs of Int64s.
                    for (int i = 0; i < ExtentCount; i++)
                    {
                        String ci = (String)Marshal.PtrToStructure(pDest, typeof(String));
                        retVal.Add(ci);
                        pDest = (IntPtr)((Int64)pDest + 16);
                    }
                }
                else
                    throw new Exception(Marshal.GetLastWin32Error().ToString());
            }
            catch (Exception)
            {
            }
            finally
            {
                CloseHandle(hFile);
                hFile = IntPtr.Zero;

                Marshal.FreeHGlobal(pAlloc);
                pAlloc = IntPtr.Zero;
            }
            return retVal;
        }

        #endregion

        #region Wrapper3: MoveFile

        /// <summary>
        /// input structure for use in MoveFile
        /// </summary>
        private struct MoveFileData
        {
            public IntPtr hFile;
            public Int64 StartingVCN;
            public Int64 StartingLCN;
            public Int32 ClusterCount;
        }

        /// <summary>
        /// move a virtual cluster for a file to a logical cluster on disk, repeat for count clusters
        /// </summary>
        /// <param name="deviceName">device to move on"c:"</param>
        /// <param name="path">file to muck with "c:\windows\explorer.exe"</param>
        /// <param name="VCN">cluster number in file to move</param>
        /// <param name="LCN">cluster on disk to move to</param>
        /// <param name="count">for how many clusters</param>
        static public void MoveFile(string deviceName, string path, Int64 VCN, Int64 LCN, Int32 count)
        {
            IntPtr hVol = IntPtr.Zero;
            IntPtr hFile = IntPtr.Zero;
            try
            {
                hVol = OpenVolume(deviceName);

                hFile = OpenFile(path);


                MoveFileData mfd = new MoveFileData();
                mfd.hFile = hFile;
                mfd.StartingVCN = VCN;
                mfd.StartingLCN = LCN;
                mfd.ClusterCount = count;

                GCHandle handle = GCHandle.Alloc(mfd, GCHandleType.Pinned);
                IntPtr p = handle.AddrOfPinnedObject();
                uint bufSize = (uint)Marshal.SizeOf(mfd);
                uint size = 0;

                bool fResult = DeviceIoControl(
                    hVol,
                    FSConstants.FSCTL_MOVE_FILE,
                    p,
                    bufSize,
                    IntPtr.Zero, // no output data from this FSCTL
                    0,
                    ref size,
                    IntPtr.Zero);

                handle.Free();

                if (!fResult)
                {
                    throw new Exception(Marshal.GetLastWin32Error().ToString());
                }
            }
            finally
            {
                CloseHandle(hVol);
                CloseHandle(hFile);
            }
        }

        #endregion

        #region Wrapper4: AcquireRights

        const int SE_PRIVILEGE_ENABLED = 0x00000002;
        const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        const int TOKEN_QUERY = 0x00000008;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int OpenProcessToken(int ProcessHandle, int DesiredAccess, ref int TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int LookupPrivilegeValue(string SystemName, string PrivilegeName, [MarshalAs(UnmanagedType.Struct)] ref LUID Luid);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int AdjustTokenPrivileges(int TokenHandle, int DisablepPrivs, [MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES Newstate, int BufferLength, int PreviousState, int Returnlength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CloseHandle(int TokenHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public LUID Luid;
            public int Attributes;
            public int PrivilegeCount;
        }

        /// <summary>
        /// Gets temporary rights to a privilege
        /// </summary>
        /// <param name="PrivilegeName">Privilege name</param>
        /// <returns>0 if succeeds, 1 if fails</returns>
        private static int AquireRights(String PrivilegeName)
        {
            int ReturnValue = 0;//value to hold result of function
            int ProcessHandle = 0; //The handle to the current process
            int TokenHandle = 0;//Token Handle
            LUID Value = new LUID();//LUID value
            TOKEN_PRIVILEGES NewState; //Token information
            //Get process handle
            ProcessHandle = System.Diagnostics.Process.GetCurrentProcess().Handle.ToInt32();
            //Open token for process
            if (0 == OpenProcessToken(ProcessHandle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref TokenHandle))
                return 1;
            //Look up LUID value of privilege
            if (0 == LookupPrivilegeValue(null, PrivilegeName, ref Value))
                ReturnValue = 1;
            //Fill token data
            NewState = new TOKEN_PRIVILEGES();
            NewState.PrivilegeCount = 1;
            NewState.Luid = Value;
            NewState.Attributes = SE_PRIVILEGE_ENABLED;
            //Set privilege
            if (0 == AdjustTokenPrivileges(TokenHandle, 0, ref NewState, 1024, 0, 0))
                ReturnValue = 1;
            //Close the token handle
            int result = CloseHandle(TokenHandle);
            return ReturnValue;
        }

        //    Try to change our permissions so we can access special files and directories
        //    such as "C:\System Volume Information". If this does not succeed then quietly
        //    continue, we'll just have to do with whatever permissions we have.
        //    SE_BACKUP_NAME = Backup and Restore Privileges.
        public static void ElevatePermissions()
        {
            AquireRights("SeBackupPrivilege"/*"Backup and Restore Privileges"*/);
        }

        #endregion

        #region ErrorMessage

        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        public const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        public const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        public const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibraryEx(
            [MarshalAs(UnmanagedType.LPTStr)] string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport( "kernel32.dll", CharSet=CharSet.Auto )]
        private static extern int FormatMessageW(
           uint dwFormatFlags, IntPtr lpSource, int dwMessageId,
           int dwLanguageId, out IntPtr MsgBuffer, int nSize, IntPtr Arguments );

        public static string GetMessage( int id/*, string dllFile */)
        {
            IntPtr hModule = IntPtr.Zero;
            IntPtr pMessageBuffer;
            int dwBufferLength;
            string sMsg = String.Empty;
            uint  dwFormatFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM
                                   | FORMAT_MESSAGE_IGNORE_INSERTS;

            //hModule = LoadLibraryEx( dllFile, // dll or exe file
            //                       IntPtr.Zero, // null for future use
            //                       LOAD_LIBRARY_AS_DATAFILE); // only to extract messages

            //if(IntPtr.Zero != hModule)
            //{
            //    dwFormatFlags |= FORMAT_MESSAGE_FROM_HMODULE;
            //    Console.WriteLine("\n > Found hmodule for: " + dllFile );
            //}

            dwBufferLength = FormatMessageW(
                                dwFormatFlags, // formatting options
                                IntPtr.Zero,//hModule,    // dll file message
                                id,         // Message identifier
                                0,          // Language identifier
                                out pMessageBuffer, // Pointer to a buffer
                                0,          // Minimum number of chars to write in pMessageBuffer
                                IntPtr.Zero ); //Pointer to an array of insertion strings

            if (0 != dwBufferLength)
            {
                sMsg = Marshal.PtrToStringUni(pMessageBuffer);
                Marshal.FreeHGlobal(pMessageBuffer);
            }  
            return sMsg;
        }

        #endregion

        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        public static extern unsafe bool ReadFile
        (
            System.IntPtr hFile,      // handle to file
            void* pBuffer,            // data buffer
            int NumberOfBytesToRead,  // number of bytes to read
            int* pNumberOfBytesRead,  // number of bytes read
            NativeOverlapped* Overlapped            // overlapped buffer
        );

        public static unsafe int Read(IntPtr handle, byte[] buffer, int index, int count, Overlapped lpOverlapped)
        {
            int n = 0;

            fixed (byte* p = buffer)
            {
                if (!ReadFile(handle, p + index, count, &n, lpOverlapped.Pack(null, null)))
                {
                    return 0;
                }
            }
            return n;
        }
    }


    /// <summary>
    /// constants lifted from winioctl.h from platform sdk
    /// </summary>
    internal class FSConstants
    {
        const uint FILE_DEVICE_FILE_SYSTEM = 0x00000009;

        const uint METHOD_NEITHER = 3;
        const uint METHOD_BUFFERED = 0;

        const uint FILE_ANY_ACCESS = 0;
        const uint FILE_SPECIAL_ACCESS = FILE_ANY_ACCESS;

        public static uint FSCTL_GET_NTFS_VOLUME_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 25, METHOD_BUFFERED, FILE_ANY_ACCESS); 

        //FSCTL_GET_VOLUME_BITMAP
        //
        // Operation
        //  This is generally the first FSCTL that is used by a defragmenter, because
        //  it is required to get a map of the clusters that are free and in use on a
        //  volume. Each bit in the bitmap returned in the output buffer represents one
        //  custer, where a 1 signifies an allocated cluster and a 0 signifies a free
        //  cluster.
        //
        // FileHandle
        //  The file handle passed to this call must represent a volume opened with the
        //  GENERIC_READ flag. A volume can be opened with a name of the form, "\\.\X:",
        //  where 'X' is the volume's drive letter.
        //
        // InputBuffer
        //  The input buffer must point to a ULONGLONG value that specifies the 8-cluster
        //  aligned starting cluster that the first bit in the returned bitmap buffer will
        //  correspond to. If the value is not 8-cluster aligned, the bitmap data returned
        //  will begin at the 8-cluster aligned value just below the one passed.
        //  InputBufferLength must be the size of a ULONGLONG in bytes, which is 8.
        //
        // OutputBuffer
        //  Points at a BITMAP_DESCRIPTOR data structure, which is defined as follows:
        //      typedef struct {
        //          ULONGLONG StartLcn;
        //          ULONGLONG ClustersToEndOfVol;
        //          BYTE Map[1];
        //      } BITMAP_DESCRIPTOR, *PBITMAP_DESCRIPTOR;
        //  StartLcn is filled in with the cluster number corresponding to the first bit
        //  in the bitmap data. ClustersToEndOfVol represents the number of clusters on the
        //  volume minus the StartLcn cluster. Map is the cluster bitmap data. The length
        //  of the bitmap data is the difference between OutputBufferLength and
        //  2 * sizeof(ULONGLONG).
        //
        // Return
        //  If there are errors related to the volume's support for this FSCTL or FileHandle
        //  representing a valid volume handle, an appropriate native NT error code is
        //  returned (as defined in the NT DDK file NTSTATUS.H). If the cluster specified
        //  in InputBuffer is out of range for the volume, the call will return
        //  STATUS_INVALID_PARAMETER. If there are no errors and there are no clusters beyond
        //  the last one described in the Map array, the FSCTL returns STATUS_SUCCESS.
        //  Otherwise STATUS_BUFFER_OVERFLOW is returned to notify the caller that further
        //  calls should be made to retrieve subsequent mappings.
        public static uint FSCTL_GET_VOLUME_BITMAP = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 27, METHOD_NEITHER, FILE_ANY_ACCESS);

        //FSCTL_GET_RETRIEVAL_POINTERS
        //
        // Operation
        //  This function returns the cluster map for a specified file. The cluster map
        //  indicates where particular clusters belonging to the file reside on a volume.
        //
        // FileHandle
        //  This is a file handle returned by a CreateFile call that opened the target file.
        //
        // InputBuffer
        //  The ULONGLONG starting cluster within the file at which the mapping information
        //  returned will commence. InputBufferLength must be 8.
        //
        // OutputBuffer
        //  Points at a GET_RETRIEVAL_DESCRIPTOR data structure:
        //      typedef struct {
        //          ULONG NumberOfPairs;
        //          ULONGLONG StartVcn;
        //          MAPPING_PAIR Pair[1];
        //      } GET_RETRIEVAL_DESCRIPTOR, *PGET_RETRIEVAL_DESCRIPTOR;
        //  The number of mapping pairs returns in the Pair array is placed in NumberOfPairs.
        //  StartVcn indicates the first cluster within the file that is mapped by the Pair
        //  array. The Pair array is a list of MAPPING_PAIR entries:
        //      typedef struct {
        //          ULONGLONG Vcn;
        //          ULONGLONG Lcn;
        //      } MAPPING_PAIR, *PMAPPING_PAIR;
        //  Each entry is made up of a virtual cluster offset, and a logical cluster on the
        //  volume. The interpretation of the array is as follows: the first Lcn in the Pairs
        //  array corresponds to the StartVcn of the GET_RETRIEVAL_DESCRIPTOR data structure.
        //  The length of the file segment that starts at that cluster can be calculated by
        //  subtracting the GET_RETRIEVAL_DESCRIPTOR StartVcn from the Vcn of the first entry
        //  in the Pairs array. The second segment of the file starts at the Vcn of the second
        //  entry in the Pairs array, with a corresponding Lcn described by the Lcn of the
        //  first entry. Its length is the difference between the Vcns of the first and second
        //  entries. These relationships are shown more clearly in the figure below.
        //
        // Pairs Array
        //  On NTFS volumes, compressed files contain 0-filled clusters that have no
        //  correspondence to Lcns on the volume. These clusters are described with Lcns in the
        //  Pairs array equal to (ULONGLONG) -1.
        //
        //  The maximum number of entries that can be returned in the Pairs array is equal to
        //  (OutputBufferLength - 2 * sizeof(ULONGLONG))/ (2 * sizeof(ULONGLONG)).
        //
        // Return
        //  If there are errors related to the volume's support for this FSCTL or FileHandle
        //  representing a valid volume handle, an appropriate native NT error code is returned
        //  (as defined in the NT DDK file NTSTATUS.H). If the cluster specified in InputBuffer
        //  is out of range for the volume, the call will return STATUS_INVALID_PARAMETER. If
        //  there are no errors and there are no clusters beyond the last one described in the
        //  Map array, the FSCTL returns STATUS_SUCCESS. Otherwise STATUS_BUFFER_OVERFLOW is
        //  returned to notify the caller that further calls should be made to retrieve
        //  subsequent mappings.
        public static uint FSCTL_GET_RETRIEVAL_POINTERS = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 28, METHOD_NEITHER, FILE_ANY_ACCESS);

        //FSCTL_MOVE_FILE
        //
        // Operation
        //  This is the core of the defragmentation support. It is used to move the clusters
        //  of a particular file to a currently unallocated position on the volume.
        //
        // FileHandle
        //  The file handle passed to this call must represent a volume opened with the
        //  GENERIC_READ flag. A volume can be opened with a name of the form, "\\.\X:",
        //  where 'X' is the volume's drive letter.
        //
        // InputBuffer
        //  A pointer to a MOVEFILE_DESCRIPTOR:
        //      typedef struct {
        //          HANDLE FileHandle;
        //          ULONG Reserved;
        //          LARGE_INTEGER StartVcn;
        //          LARGE_INTEGER TargetLcn;
        //          ULONG NumVcns;
        //          ULONG Reserved1;
        //      } MOVEFILE_DESCRIPTOR, *PMOVEFILE_DESCRIPTOR;
        //  FileHandle is a handle to a file previously opened by CreateFile, and represents
        //  the file that is the subject of the move operation. StartVcn is the start of the
        //  segment within the file that will be moved. TargetVcn is the Lcn on the volume
        //  to which the files clusters will be moved, and NumVcns are the number of clusters
        //  making up the segment that will be moved.
        //
        // NTFS Caveats: The NTFS implementation of this command uses some of the logic present
        // in NTFS that supports the reallocation of data within compressed files. The NTFS
        // compression algorithm divides compressed files into 16-cluster segments, so the
        // MoveFile functionality on NTFS suffers a similar restriction. When the clusters of
        // an uncompressed file are moved, StartVcn must be 16-cluster aligned, or MoveFile will
        // adjust it to the next lowest 16-cluster boundary. Similarly, NumVcns must be a multiple
        // of 16 or MoveFile will round it up to the next multiple of 16. If the clusters of a
        // compressed file are being moved, the rules are little more complex: StartVcn can
        // specify the beginning of any non 0-filled segment of the file that immediately
        // follows a 0-filled segment, and NumVcns must specify a run of clusters within the file
        // that precisely encompasses non 0-filled segments of the file. The same type of rounding
        // described for movement of non-compressed files will be performed if the rules are not
        // followed.
        //
        // OutputBuffer
        //  This function does not return any data, so this parameter should be NULL.
        //
        // Return
        //  If either the volume file handle or the file's handle are invalid an appropriate
        //  error code is returned. If StartVcn is out of range for the file or TargetLcn is out
        //  of range for the volume, STATUS_INVALID_PARAMETER is returned.
        //
        // If the move is directed at a FAT volume, the only way to tell if the move was successful
        // is to re-examine the file's mapping using FSCTL_GET_RETRIEVAL_POINTERS, since the call
        // will always return STATUS_SUCCESS.
        //
        // If the move is directed at an NTFS volume, the function will return STATUS_SUCCESS if
        // it was successful. Otherwise it will return STATUS_ALREADY_COMMITTED to indicate that
        // some range of the target clusters are already in use. In some cases, attempting to
        // move clusters to an area that is marked as unallocated in an NTFS volume bitmap as
        // free will result an unexpected STATUS_INVALID_PARAMETER. This is due to the fact that
        // NTFS reserves ranges of clusters for the expansion of its own metadata files, which the
        // cluster reallocation code will not attempt to use.
        //
        // Prior to Windows XP, FSCTL_MOVE_FILE does not work on volumes with cluster sizes larger
        // than 4KB. The error returned when moves are attempted on such volumes is
        // STATUS_INVALID_DEVICE_REQUEST. This limitation, which is tied to its implementor's
        // mistaken belief that FSCTL_MOVE_FILE must suffer the same limitations as NTFS
        // compression, is relatively serious because FORMAT uses cluster sizes larger than 4KB
        // on volumes larger than 4GB.
        //
        // Another thing to be aware of when moving clusters on a NTFS volume is that clusters
        // that have been freed by a MoveFile operation will not be available for reallocation
        // themselves until the volume has been checkpointed and if the volume has not been
        // checkpointed, the function will return STATUS_ALREADY_COMMITTED. NTFS checkpoints
        // volumes every few seconds so the only remedy is to wait and try again.
        //
        // Note that because a file handle for the file to be moved must be passed to
        // FSCTL_MOVE_FILE, it is not possible to move the clusters of files that have been
        // opened by another process for exclusive access. Since the system opens paging files,
        // the Registry, and NTFS metadata files for exclusive access, they cannot be
        // defragmented. Also, because of the way the FSCTL_MOVE_FILE routine is written, it
        // is only possible to reallocate file data clusters, and not directories or other
        // file metadata.
        public static uint FSCTL_MOVE_FILE = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 29, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);

        static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method);
        }
    }
}
