using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DumpMemory
{
	class Program
	{
		static unsafe void Main(string[] args)
		{
			Process selectedProc = Process.GetProcessById(SelectProcess());

			NativeMethods.MemoryInformation[] memRegions = NativeMethods.EnumerateMemRegions(selectedProc, i => true).ToArray();
			void* ProcessHandle = NativeMethods.OpenAllAccessProcess(selectedProc);

			Log.Info($"Found {memRegions.Length} Memory Region(s)");

			FileStream fileStream = new FileStream($"{selectedProc.ProcessName}.bin", FileMode.Create);
			for(int x = 0; x < memRegions.Length; x++)
				WriteDump(memRegions[x], ProcessHandle, fileStream);
			fileStream.Close();

			FileInfo fileInfo = new FileInfo($"{selectedProc.ProcessName}.bin");
			Log.Info($"Wrote {memRegions.Length} Memory Region(s) to Disk, {fileInfo.Length} Bytes to Disk.");
		}

		private static unsafe void WriteDump(NativeMethods.MemoryInformation memoryInformation, void *processHandle, FileStream fileStream)
		{
			int bytesRead = 0;
			byte[] memoryRead = NativeMethods.ReadMemory(processHandle, memoryInformation.BaseAddress, memoryInformation.RegionSize, out bytesRead);
			fileStream.Write(memoryRead, 0, memoryRead.Length);
		}

		private static int SelectProcess()
		{
			Console.Clear();
			Console.WriteLine(string.Join("\n", Process.GetProcesses().Where(i => i.MainWindowTitle != "").OrderBy(i => i.ProcessName).Select(i => $"[{i.Id}]	{i.ProcessName}").ToArray()));

			int selectedPID = 0;
			Console.Write("Process ID: ");
			if (!int.TryParse(Console.ReadLine(), out selectedPID))
				return SelectProcess();

			return selectedPID;
		}
	}
}
