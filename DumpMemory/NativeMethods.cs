using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DumpMemory
{
    public unsafe static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(void* hProcess, int lpAddress, uint dwSize, uint flNewProtect, out AllocationProtectEnum lpflOldProtect);

        [DllImport("kernel32.dll")]
        private static extern void* OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(void* hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SystemInformation lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(void* hProcess, int lpAddress, out MemoryInformation lpBuffer, uint dwLength);


        private const int MEM_COMMIT = 0x00001000;

        private const int PROCESS_ALL_ACCESS = (int)(HandlePrivileges.DELETE | HandlePrivileges.READ_CONTROL | HandlePrivileges.WRITE_DAC | HandlePrivileges.WRITE_OWNER | HandlePrivileges.SYNCHRONIZE | HandlePrivileges.END);
        public enum HandlePrivileges : int
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_WM_READ = 0x0010,
            END = 0xFFF
        }

        public enum AllocationProtectEnum : uint
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        public enum StateEnum : uint
        {
            MEM_COMMIT = 0x1000,
            MEM_FREE = 0x10000,
            MEM_RESERVE = 0x2000
        }

        public enum TypeEnum : uint
        {
            MEM_IMAGE = 0x1000000,
            MEM_MAPPED = 0x40000,
            MEM_PRIVATE = 0x20000
        }

        public struct MemoryInformation
        {
            public int BaseAddress;
            public int AllocationBase;
            public AllocationProtectEnum AllocationProtect;
            public int RegionSize;
            public StateEnum State;
            public AllocationProtectEnum Protect;
            public TypeEnum Type;
        }

        private struct SystemInformation
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }


        public static byte[] ReadMemory(Process proc, int startAddress, int length, out int bytesRead)
        {
            try
            {
                byte[] buffer = new byte[length];
                void* processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, proc.Id);

                int i = 0;
                if (!ReadProcessMemory(processHandle, startAddress, buffer, length, ref i))
                {
                    bytesRead = 0;
                    return new byte[0];
                }

                bytesRead = i;
                return buffer;
            }
            catch
            {
                bytesRead = 0;
                return new byte[0];
            }
        }

        public static MemoryInformation[] EnumerateMemRegions(Process process, Predicate<MemoryInformation> predicate)
        {
            List<MemoryInformation> memoryInformationList = new List<MemoryInformation>();

            SystemInformation systemInfo;
            GetSystemInfo(out systemInfo);

            void* processHandle = OpenProcess((int)(HandlePrivileges.PROCESS_QUERY_INFORMATION | HandlePrivileges.PROCESS_WM_READ), false, process.Id);

            MemoryInformation memoryRegionInfo;
            for (int address = (int)systemInfo.minimumApplicationAddress; ; address += memoryRegionInfo.RegionSize)
            {
                VirtualQueryEx(processHandle, address, out memoryRegionInfo, 28);
                if (predicate.Invoke(memoryRegionInfo))
                    memoryInformationList.Add(memoryRegionInfo);

                MemoryInformation peekMemInfo;
                VirtualQueryEx(processHandle, address + memoryRegionInfo.RegionSize, out peekMemInfo, 28);

                if (peekMemInfo.BaseAddress < memoryRegionInfo.BaseAddress)
                    break;
            }

            return memoryInformationList.ToArray();
        }
    }
}
