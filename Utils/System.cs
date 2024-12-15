using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SystemInfo
{
    public class SystemInf
    {
        public string GetSystemInfo()
        {
            StringBuilder systemInfo = new StringBuilder();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                systemInfo.AppendLine("Currently on Windows and Server didnt support Windows!");
                systemInfo.AppendLine(GetWinSystemInfo());
            }
            else
            {
                // Linux specific code
                systemInfo.AppendLine("CPU Information (Linux):");
                systemInfo.AppendLine(GetLinuxCpuInfo());
                systemInfo.AppendLine($"CPU Usage: {GetLinuxCpuUsage()}%");
                systemInfo.AppendLine(GetLinuxMemoryInfo());
            }

            return systemInfo.ToString();
        }
        public static string GetWinSystemInfo()
        {
            StringBuilder sb = new StringBuilder();

            // Accessing registry for Windows OS version
            string osVersion = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "Unknown");
            sb.AppendLine($"Windows Version: {osVersion}");

            // Accessing registry for OS architecture (32-bit or 64-bit)
            string osArchitecture = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "Architecture", "Unknown");
            sb.AppendLine($"OS Architecture: {osArchitecture}");

            // Return the accumulated string
            return sb.ToString();
        }

        // Linux-specific methods
        private static string GetLinuxCpuInfo()
        {
            string cpuInfo = "/proc/cpuinfo";
            if (File.Exists(cpuInfo))
            {
                var lines = File.ReadLines(cpuInfo);
                foreach (var line in lines)
                {
                    if (line.StartsWith("model name"))
                    {
                        return line;
                    }
                }
            }
            return "CPU Info not available";
        }

        private static float GetLinuxCpuUsage()
        {
            string statFile = "/proc/stat";
            if (File.Exists(statFile))
            {
                var lines = File.ReadLines(statFile);
                foreach (var line in lines)
                {
                    if (line.StartsWith("cpu"))
                    {
                        var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (columns.Length > 4)
                        {
                            long user = long.Parse(columns[1]);
                            long nice = long.Parse(columns[2]);
                            long system = long.Parse(columns[3]);
                            long idle = long.Parse(columns[4]);
                            long total = user + nice + system + idle;
                            long used = user + nice + system;

                            return (float)(used * 100.0 / total);
                        }
                    }
                }
            }
            return 0;
        }

        private static string GetLinuxMemoryInfo()
        {
            string memInfo = "/proc/meminfo";
            if (File.Exists(memInfo))
            {
                var lines = File.ReadLines(memInfo);
                long totalMemory = 0;
                long freeMemory = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal"))
                    {
                        totalMemory = long.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                    }
                    if (line.StartsWith("MemFree"))
                    {
                        freeMemory = long.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                    }
                }
                double totalMemoryMB = totalMemory / 1024.0; // Convert kB to MB
                double freeMemoryMB = freeMemory / 1024.0; // Convert kB to MB
                double usedMemoryMB = totalMemoryMB - freeMemoryMB;

                return $"Total Memory: {totalMemoryMB} MB\nUsed Memory: {usedMemoryMB} MB\nFree Memory: {freeMemoryMB} MB";
            }
            return "Memory Info not available";
        }

    }
}
