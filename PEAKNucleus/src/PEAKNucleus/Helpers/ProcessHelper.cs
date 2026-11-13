using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PEAKNucleus.Helpers
{
    internal class ProcessHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static void StartDependentProcess(string path)
        {
            var processStartInfo = new ProcessStartInfo(path);
            processStartInfo.WorkingDirectory = Directory.GetParent(path).FullName;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            var process = Process.Start(processStartInfo);

            JOBOBJECT_BASIC_LIMIT_INFORMATION info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
            info.LimitFlags = 0x2000;

            var job = new Job();
            job.AddProcess(process.Handle);
        }
    }
}
