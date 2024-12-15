using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Net;
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
                systemInfo.AppendLine("Currently on Windows (Development API):");
                systemInfo.AppendLine(GetWinSystemInfo());
            }
            else
            {
                // Linux specific code
                systemInfo.AppendLine("Currently on Ubuntu 22.04.5 LTS (Public Released):");
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
        public  string PingWithoutLib(string address)
        {
            try
            {
                // Create a raw socket
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
                {
                    // Set up the ICMP request packet
                    byte[] icmpPacket = CreateIcmpPacket();

                    // Resolve the address
                    IPAddress ipAddress = IPAddress.Parse(address);
                    EndPoint remoteEndPoint = new IPEndPoint(ipAddress, 0);

                    // Send the ICMP packet
                    socket.SendTo(icmpPacket, remoteEndPoint);

                    // Buffer for the response
                    byte[] buffer = new byte[1024];

                    // Receive the response
                    DateTime startTime = DateTime.Now;
                    socket.ReceiveTimeout = 3000; // 3 seconds timeout
                    int receivedBytes = socket.Receive(buffer);
                    DateTime endTime = DateTime.Now;

                    // Calculate roundtrip time
                    double roundTripTime = (endTime - startTime).TotalMilliseconds;

                    // Validate response
                    if (receivedBytes > 0)
                    {
                        return $"Ping to {address} successful:\nRoundtrip Time: {roundTripTime} ms";
                    }
                    else
                    {
                        return $"Ping to {address} failed: No response received.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        static byte[] CreateIcmpPacket()
        {
            byte[] packet = new byte[8]; // ICMP header is 8 bytes
            packet[0] = 8;  // Type (8 = Echo Request)
            packet[1] = 0;  // Code
            packet[2] = 0;  // Checksum (will calculate below)
            packet[3] = 0;  // Checksum (part 2)
            packet[4] = 0;  // Identifier (arbitrary)
            packet[5] = 1;  // Identifier (part 2)
            packet[6] = 0;  // Sequence Number
            packet[7] = 1;  // Sequence Number (part 2)

            // Calculate checksum
            ushort checksum = CalculateChecksum(packet);
            packet[2] = (byte)(checksum >> 8); // High byte
            packet[3] = (byte)(checksum & 0xFF); // Low byte

            return packet;
        }

        static ushort CalculateChecksum(byte[] data)
        {
            int sum = 0;

            for (int i = 0; i < data.Length; i += 2)
            {
                ushort word = (ushort)((data[i] << 8) | (i + 1 < data.Length ? data[i + 1] : 0));
                sum += word;
            }

            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            return (ushort)~sum;
        }
    }

}
