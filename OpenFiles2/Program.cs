using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace OpenFiles2
{
    class Program
    {

        public enum NERR
        {
            /// <summary>
            /// Operation was a success.
            /// </summary>
            NERR_Success = 0,
            /// <summary>
            /// More data available to read. dderror getting all data.
            /// </summary>
            ERROR_MORE_DATA = 234,
            /// <summary>
            /// Network browsers not available.
            /// </summary>
            ERROR_NO_BROWSER_SERVERS_FOUND = 6118,
            /// <summary>
            /// LEVEL specified is not valid for this call.
            /// </summary>
            ERROR_INVALID_LEVEL = 124,
            /// <summary>
            /// Security context does not have permission to make this call.
            /// </summary>
            ERROR_ACCESS_DENIED = 5,
            /// <summary>
            /// Parameter was incorrect.
            /// </summary>
            ERROR_INVALID_PARAMETER = 87,
            /// <summary>
            /// Out of memory.
            /// </summary>
            ERROR_NOT_ENOUGH_MEMORY = 8,
            /// <summary>
            /// Unable to contact resource. Connection timed out.
            /// </summary>
            ERROR_NETWORK_BUSY = 54,
            /// <summary>
            /// Network Path not found.
            /// </summary>
            ERROR_BAD_NETPATH = 53,
            /// <summary>
            /// No available network connection to make call.
            /// </summary>
            ERROR_NO_NETWORK = 1222,
            /// <summary>
            /// Pointer is not valid.
            /// </summary>
            ERROR_INVALID_HANDLE_STATE = 1609,
            /// <summary>
            /// Extended Error.
            /// </summary>
            ERROR_EXTENDED_ERROR = 1208,
            /// <summary>
            /// Base.
            /// </summary>
            NERR_BASE = 2100,
            /// <summary>
            /// Unknown Directory.
            /// </summary>
            NERR_UnknownDevDir = (NERR_BASE + 16),
            /// <summary>
            /// Duplicate Share already exists on server.
            /// </summary>
            NERR_DuplicateShare = (NERR_BASE + 18),
            /// <summary>
            /// Memory allocation was to small.
            /// </summary>
            NERR_BufTooSmall = (NERR_BASE + 23)
        }

        const int MAX_PREFERRED_LENGTH = -1;   //originally 0

        const int PERM_FILE_NONE = 0;
        const int PERM_FILE_READ = 1;
        const int PERM_FILE_WRITE = 2;
        const int PERM_FILE_CREATE = 4;
        const int PERM_FILE_EXECUTE = 8;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct FILE_INFO_3
        {
            public uint fi3_id;
            public PermFile fi3_permissions;
            public int fi3_num_locks;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string fi3_pathname;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string fi3_username;
        }

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern int NetSessionEnum(
            [In, MarshalAs(UnmanagedType.LPWStr)] string ServerName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string UncClientName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string UserName,
            Int32 Level,
            out IntPtr bufptr,
            int prefmaxlen,
            ref Int32 entriesread,
            ref Int32 totalentries,
            ref Int32 resume_handle);

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern int NetFileEnum(
            IntPtr ptr_servername,
            string basepath,
            string username,
            int level,
            ref IntPtr bufptr,
            int prefmaxlen,
            ref long entriesread,
            ref long totalentries,
            out IntPtr resume_handle);
        

        public enum PermFile
        {
            // file network permissions
            Read = 0x1,     //user has read access
            Write = 0x2,    // user has write access
            Create = 0x4,   //user has create access
            Perm08 = 0x8,   //?
            Perm10 = 0x10,  //?
            Perm20 = 0x20   //?
        }

        [DllImport("netapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int NetApiBufferFree(IntPtr pBuffer);

        [DllImport("netapi32.dll", EntryPoint = "NetServerEnum")]
        private static extern int NetServerEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string servername,
            int level,
            out IntPtr bufptr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            int servertype,
            [MarshalAs(UnmanagedType.LPWStr)] string domain,
            IntPtr resume_handle);


        static bool boolTabular = false;



        static List<FILE_INFO_3> FileEnum(string sServer = "")
        {
            long EntriesRead = 0;
            long TotalRead = 0;
            IntPtr ResumeHandle = IntPtr.Zero;
            IntPtr bufptr = IntPtr.Zero;
            int ret;
            List<FILE_INFO_3> Result = new List<FILE_INFO_3>();
            


            if (string.IsNullOrEmpty(sServer))
            {
                sServer = Environment.MachineName;
            }

            GCHandle handle = GCHandle.Alloc(sServer, GCHandleType.Pinned);
            IntPtr strPtr = handle.AddrOfPinnedObject();

            long totalEntries = 0;
            //int idx = totalEntries;

            
            do
            {
                ret = NetFileEnum(strPtr, null, null, 3, ref bufptr, MAX_PREFERRED_LENGTH, ref EntriesRead, ref TotalRead, out ResumeHandle);
                if (ret != (int)NERR.NERR_Success)
                {
                    break;
                }

                //List<FILE_INFO_3> listTemp = new List<FILE_INFO_3>();
                for (int i = 0; i < EntriesRead; i++)
                {
                    //FILE_INFO_3 xxx = Marshal.PtrToStructure()
                    int size = Marshal.SizeOf((new FILE_INFO_3())) * i;
                    IntPtr ptr = new IntPtr(bufptr.ToInt32() + size);
                    
                    Result.Add((FILE_INFO_3)Marshal.PtrToStructure(ptr, typeof(FILE_INFO_3)));
                }
                totalEntries += EntriesRead;
               

            } while ( ret == (int)NERR.ERROR_MORE_DATA );

            handle.Free();

            return Result;
        }

        static void Main(string[] args)
        {

            string strComputer = "";

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string CurrentArg = args[i].ToString();

                    if (CurrentArg.ToUpper() == "/T")
                    {
                        boolTabular = true;
                    }
                    else
                    {
                        strComputer = CurrentArg;
                    }
                }
            }

            if (strComputer == "/?")
            {
                WriteUsage();
                return;
            }


            
            List<FILE_INFO_3> fileInfo = FileEnum(strComputer);

            if (boolTabular)
            {
                Console.WriteLine("ID\t#Locks\tUsername\tPermissions\tPath Name");
            }
            else
            {
                Console.WriteLine("\"ID\",\"#Locks\",\"Username\",\"Permissions\",\"Path Name\"");
            }
            


            foreach (FILE_INFO_3 file in fileInfo)
            {
                if (boolTabular)
                {
                    Console.WriteLine(String.Join("\t", new string[] { file.fi3_id.ToString(), file.fi3_num_locks.ToString(), file.fi3_username, GetPermissions((int)file.fi3_permissions), file.fi3_pathname}));
                }
                else
                {
                    Console.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\"", file.fi3_id, file.fi3_num_locks, file.fi3_username, GetPermissions((int)file.fi3_permissions), file.fi3_pathname);
                }
            }
        }

        private static string GetPermissions(int perm)
        {
            switch (perm)
            {
                case PERM_FILE_WRITE:
                    return "Write";
                case PERM_FILE_EXECUTE:
                    return "Execute";
                case PERM_FILE_READ:
                    return "Read";
                case PERM_FILE_READ + PERM_FILE_WRITE:
                    return "Read+Write";
                default:
                    return perm.ToString();
            }
        }

        private static void WriteUsage()
        {
            Console.WriteLine("\nTitle:\t\t" + Application.ProductName + "\n" + 
                  "Author:\t\tJoe Ostrander\n" + 
                  "Created:\t2018.02.02\n" + 
                  "Version:\t" + Application.ProductVersion + "\n\n" + 
                  "What:\tList open network files on a host\n" + 
                  "Why:\tWhen Microsoft's *Openfiles.exe* is run as a scheduled task,\n" + 
                  "\tIt leaves a session open on the remote PC.\n\n" +
                  "USAGE:\t\t" + Application.ProductName + ".exe\n" + 
                  "\t\t" + Application.ProductName + ".exe <computername>\n" + 
                  "\t\t" + Application.ProductName + ".exe <computername> /T\n" + 
                  "\t\t(tabular output)");
        }
    }
}
